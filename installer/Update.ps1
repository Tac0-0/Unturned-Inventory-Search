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
# GitHub published releases API supports pre-releases; the /latest endpoint excludes them.
$headers = @{ 'User-Agent'='InventorySearch-Updater'; 'Accept'='application/vnd.github+json' }
$releases = @(Invoke-RestMethod -Uri ("https://api.github.com/repos/" + $config.repository + "/releases?per_page=15") -Headers $headers)
$asset = $null
$selectedRelease = $null
foreach ($release in $releases) {
    if ($release.draft) { continue }
    $a = @($release.assets | Where-Object { $_.name -eq $config.assetName }) | Select-Object -First 1
    if ($a) { $asset = $a; $selectedRelease = $release; break }
}
if (-not $asset) {
    Write-Host 'No published release with InventorySearchPrototype.dll was found.'
    exit 0
}
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