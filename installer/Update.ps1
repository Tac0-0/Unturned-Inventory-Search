# Unturned Inventory Search - read-only GitHub release check and verified DLL replacement.
# Never downloads executable scripts. Never modifies other plugins or the dedicated server.
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$stateDir = Join-Path $env:LOCALAPPDATA 'InventorySearch'
$configPath = Join-Path $stateDir 'config.json'
if (-not (Test-Path -LiteralPath $configPath -PathType Leaf)) {
    throw 'Inventory Search is not configured; run the installer first.'
}
$config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
if (-not $config.repository -or [string]$config.repository -notmatch '^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$') {
    Write-Host 'No official GitHub release repository configured yet; update skipped.'
    exit 0
}
if ([string]$config.assetName -ne 'InventorySearchPrototype.dll') {
    throw 'Unrecognized release asset name. Refusing to install unexpected files.'
}
$gameDir = [string]$config.gamePath
if (-not (Test-Path -LiteralPath (Join-Path $gameDir 'Unturned.exe') -PathType Leaf)) {
    throw 'Saved Unturned directory no longer exists. Re-run installer to locate it.'
}
if (Get-Process -Name Unturned -ErrorAction SilentlyContinue) {
    Write-Host 'Unturned is running; skipping update until next check.'
    exit 0
}
$target = Join-Path $gameDir 'BepInEx\plugins\InventorySearchPrototype\InventorySearchPrototype.dll'
if (-not (Test-Path -LiteralPath $target -PathType Leaf)) {
    throw 'Inventory Search DLL is missing. Re-run installer to reinstall before updating.'
}
# GitHub publishes both stable releases and beta pre-releases through this endpoint.
# In Windows PowerShell 5.1, Invoke-RestMethod can return a JSON array as one
# nested [object[]] pipeline result. Never wrap it blindly in @(...).
$headers = @{ 'User-Agent'='InventorySearch-Updater'; 'Accept'='application/vnd.github+json' }
$releaseUri = "https://api.github.com/repos/$($config.repository)/releases?per_page=15"
Write-Host ("Checking official Inventory Search releases: " + $config.repository)
$releaseResponse = Invoke-RestMethod -Uri $releaseUri -Headers $headers

# Explicitly flatten array responses; avoids Windows PowerShell 5.1
# pipeline/enumeration differences and does not depend on ConvertFrom-Json depth.
$releases = New-Object 'System.Collections.Generic.List[object]'
function Add-InventorySearchRelease([object] $item) {
    if ($null -eq $item) { return }
    if ($item -is [array]) {
        foreach ($child in $item) { Add-InventorySearchRelease $child }
    } else {
        [void] $releases.Add($item)
    }
}
Add-InventorySearchRelease $releaseResponse

$asset = $null
$selectedRelease = $null
$availableTags = New-Object 'System.Collections.Generic.List[string]'
foreach ($release in $releases) {
    if ($null -eq $release -or $release.draft -or -not $release.tag_name) { continue }
    [void] $availableTags.Add([string]$release.tag_name)
    foreach ($candidate in $release.assets) {
        if ($null -ne $candidate -and [string]$candidate.name -eq [string]$config.assetName) {
            $asset = $candidate
            $selectedRelease = $release
            break
        }
    }
    if ($null -ne $asset) { break }
}
if ($null -eq $asset) {
    throw ("No matching DLL release asset found. Published tags returned: " +
           [string]::Join(', ', $availableTags.ToArray()) +
           ". Expected asset: " + [string]$config.assetName +
           ". Reinstall from the official v1.0.0 ZIP if the repository is wrong.")
}
Write-Host ("Found release: " + $selectedRelease.tag_name + " | Asset: " + $asset.name)
if (-not ([string]$asset.digest -match '^sha256:[A-Fa-f0-9]{64}$')) {
    throw 'Release asset lacks a SHA-256 digest. Refusing an unverified update.'
}
if (-not ([string]$asset.browser_download_url -match '^https://github\.com/[^/]+/[^/]+/releases/download/')) {
    throw 'Release asset URL is not a GitHub Releases download.'
}
$expected = ([string]$asset.digest).Substring(7).ToUpperInvariant()
if ((Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -eq $expected) {
    Write-Host 'Inventory Search is already up to date.'
    exit 0
}
$tmp = Join-Path ([IO.Path]::GetTempPath()) ('InventorySearch-' + [guid]::NewGuid().ToString('N') + '.dll')
try {
    Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $tmp -UseBasicParsing
    if ((Get-FileHash -LiteralPath $tmp -Algorithm SHA256).Hash -ne $expected) {
        throw 'Update hash mismatch. Current working version has NOT been replaced.'
    }
    # Check a second time, in case the user launched the game during download.
    if (Get-Process -Name Unturned -ErrorAction SilentlyContinue) {
        Write-Host 'Unturned started during download; waiting until the next update check.'
        exit 0
    }
    $backup = Join-Path $stateDir 'InventorySearchPrototype.previous.dll'
    Copy-Item -LiteralPath $target -Destination $backup -Force
    try {
        Copy-Item -LiteralPath $tmp -Destination $target -Force
        if ((Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -ne $expected) {
            throw 'Installed update did not match expected SHA-256.'
        }
        Write-Host ('Inventory Search updated from official release: ' + $selectedRelease.tag_name)
    } catch {
        Copy-Item -LiteralPath $backup -Destination $target -Force
        throw
    }
} finally {
    Remove-Item -LiteralPath $tmp -Force -ErrorAction SilentlyContinue
}