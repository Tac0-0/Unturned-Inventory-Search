# Offline Windows integration test of the ACTUAL packaged Update.ps1.
# Mocks only GitHub HTTP I/O, not the updater's manifest or transaction logic.
$ErrorActionPreference = 'Stop'
$zipPath = (Resolve-Path 'release-assets/InventorySearch-OneClick-v1.9.9.zip').Path
$env:MOCK_RELEASE_ZIP = $zipPath
$env:LOCALAPPDATA = Join-Path $env:RUNNER_TEMP 'CompletePackageTest'
$state = Join-Path $env:LOCALAPPDATA 'InventorySearch'
$game = Join-Path $env:RUNNER_TEMP 'UnturnedCompletePackageTest'
$pluginDir = Join-Path $game 'BepInEx\plugins\InventorySearchPrototype'
New-Item -ItemType Directory -Path $state -Force | Out-Null
New-Item -ItemType Directory -Path $pluginDir -Force | Out-Null
Set-Content -LiteralPath (Join-Path $game 'Unturned.exe') -Value 'dummy'
$pluginDll = Join-Path $pluginDir 'InventorySearchPrototype.dll'
Copy-Item -LiteralPath 'dist/v1.9.9/InventorySearchPrototype.dll' -Destination $pluginDll
Copy-Item -LiteralPath 'installer/Update.ps1' -Destination (Join-Path $state 'Update.ps1')
@{
    gamePath = $game
    repository = 'Tac0-0/Unturned-Inventory-Search'
    assetName = 'InventorySearchPrototype.dll'
    installedVersion = 'v1.9.9'
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $state 'config.json')
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [System.IO.Compression.ZipFile]::OpenRead($zipPath)
try {
    $root = 'InventorySearch-OneClick-v1.9.9/'
    $manifest = $null
    $entry = @($archive.Entries | Where-Object { $_.FullName -ceq ($root+'update-manifest.json') })[0]
    $reader = [IO.StreamReader]::new($entry.Open(), [Text.Encoding]::UTF8)
    try { $manifestText = $reader.ReadToEnd() } finally { $reader.Dispose() }
    $manifest = $manifestText | ConvertFrom-Json
} finally { $archive.Dispose() }
$manifestText | Set-Content -LiteralPath (Join-Path $state 'installed-manifest.json')
$zipDigest = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
$dllDigest = (Get-FileHash -LiteralPath $pluginDll -Algorithm SHA256).Hash.ToLowerInvariant()
$mockRunner = Join-Path $env:RUNNER_TEMP 'MockFullPackageGitHub.ps1'
$mockCode = @'
$ErrorActionPreference = "Stop"
function global:Invoke-RestMethod {
    param([string]$Uri, [object]$Headers)
    $zipDigest = (Get-FileHash -LiteralPath $env:MOCK_RELEASE_ZIP -Algorithm SHA256).Hash.ToLowerInvariant()
    $dllDigest = (Get-FileHash -LiteralPath $env:MOCK_GAME_DLL -Algorithm SHA256).Hash.ToLowerInvariant()
    [pscustomobject]@{
        draft = $false
        prerelease = $false
        tag_name = "v1.9.9"
        assets = @(
            [pscustomobject]@{
                name = "InventorySearch-OneClick-v1.9.9.zip"
                digest = "sha256:$zipDigest"
                browser_download_url = "https://github.com/Tac0-0/Unturned-Inventory-Search/releases/download/v1.9.9/InventorySearch-OneClick-v1.9.9.zip"
            }
            [pscustomobject]@{ name = "InventorySearchPrototype.dll"; digest = "sha256:$dllDigest" }
        )
    }
}
function global:Invoke-WebRequest {
    param([string]$Uri, [string]$OutFile, [switch]$UseBasicParsing)
    Copy-Item -LiteralPath $env:MOCK_RELEASE_ZIP -Destination $OutFile -Force
}
& (Join-Path $env:LOCALAPPDATA 'InventorySearch/Update.ps1')
'@
$mockCode | Set-Content -LiteralPath $mockRunner
$env:MOCK_GAME_DLL = $pluginDll
function Run-Updater {
    $output = & powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File $mockRunner 2>&1 | Out-String
    Write-Host $output
    if ($LASTEXITCODE -ne 0) { throw ('Updater failed: ' + $output) }
    return $output
}
# Broken legacy state: DLL and version already match, but every new CMD is
# missing. This must NOT print "already up to date".
$output = Run-Updater
if ($output -match 'already up to date') { throw 'Updater incorrectly trusted DLL only' }
foreach ($item in $manifest.files) {
    $target = [string]$item.target
    $pieces = $target.Split('/')
    $relative = [string]::Join([IO.Path]::DirectorySeparatorChar.ToString(), $pieces[1..($pieces.Length-1)])
    $base = if ($pieces[0] -eq 'app') { $state } else { $pluginDir }
    $file = Join-Path $base $relative
    if (-not (Test-Path -LiteralPath $file -PathType Leaf)) { throw ('Missing newly installed file: ' + $file) }
    if ((Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash -ne ([string]$item.sha256).ToUpperInvariant()) {
        throw ('Managed file checksum mismatch: ' + $target)
    }
}
if (-not (Test-Path -LiteralPath (Join-Path $state 'INSTALL.cmd'))) { throw 'INSTALL.cmd was not installed' }
if (-not (Test-Path -LiteralPath (Join-Path $state 'UNINSTALL-CRAFTING.cmd'))) { throw 'Uninstaller was not installed' }
# An owned file from an older package must disappear; unrelated files must stay.
$owned = Join-Path $state 'RETIRED-OLD-COMMAND.cmd'
$foreign = Join-Path $state 'USER-OTHER-MOD.cmd'
Set-Content -LiteralPath $owned -Value 'delete this old command'
Set-Content -LiteralPath $foreign -Value 'preserve this unrelated file'
$previous = Get-Content -LiteralPath (Join-Path $state 'installed-manifest.json') -Raw | ConvertFrom-Json
$previous.files = @($previous.files) + @([pscustomobject]@{
    source = 'RETIRED-OLD-COMMAND.cmd'
    target = 'app/RETIRED-OLD-COMMAND.cmd'
    sha256 = ('0' * 64)
})
$previous | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $state 'installed-manifest.json')
[void](Run-Updater)
if (Test-Path $owned) { throw 'Updater failed to retire package-owned old command' }
if (-not (Test-Path $foreign)) { throw 'Updater removed an unrelated file' }
# Simulate a MAJOR REWRITE that retires entire nested plugin/app layouts,
# while the new ZIP already installs completely different owned files.
$legacyApp = Join-Path $state 'legacy\engine\OldSearchModule.dll'
$legacyPlugin = Join-Path $pluginDir 'obsolete\ui\OldSearchUI.dll'
foreach ($file in @($legacyApp, $legacyPlugin)) {
    New-Item -ItemType Directory -Path (Split-Path -Parent $file) -Force | Out-Null
    Set-Content -LiteralPath $file -Value 'Old package implementation'
}
$custom = Join-Path $pluginDir 'obsolete\CUSTOM-DO-NOT-DELETE.txt'
Set-Content -LiteralPath $custom -Value 'not ours'
$previous = Get-Content -LiteralPath (Join-Path $state 'installed-manifest.json') -Raw | ConvertFrom-Json
$previous.files = @($previous.files) + @(
    [pscustomobject]@{ source='unused/old-a'; target='app/legacy/engine/OldSearchModule.dll'; sha256=('0' * 64) },
    [pscustomobject]@{ source='unused/old-b'; target='plugin/obsolete/ui/OldSearchUI.dll'; sha256=('0' * 64) }
)
$previous | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $state 'installed-manifest.json')
[void](Run-Updater)
if (Test-Path -LiteralPath $legacyApp) { throw 'Revamp failed to retire old app implementation' }
if (Test-Path -LiteralPath $legacyPlugin) { throw 'Revamp failed to retire old plugin implementation' }
if (Test-Path -LiteralPath (Join-Path $state 'legacy')) { throw 'Empty obsolete app subtree was retained' }
if (Test-Path -LiteralPath (Join-Path $pluginDir 'obsolete\ui')) { throw 'Empty obsolete plugin subtree was retained' }
if (-not (Test-Path -LiteralPath $custom)) { throw 'Revamp deleted untracked user content' }
if (-not (Test-Path -LiteralPath (Join-Path $state 'tools\Prepare-Release.ps1'))) {
    throw 'Revamp failed to install new nested app directory'
}
# Same-version, genuinely complete package is not copied over again.
$output = Run-Updater
if ($output -notmatch 'already up to date') { throw 'Fully matching package was not recognized' }
# Tampering/deleting one current command triggers a same-version repair.
$managed = Join-Path $state 'CRAFTING-ADDON.cmd'
Set-Content -LiteralPath $managed -Value 'tampered'
$output = Run-Updater
if ($output -match 'already up to date') { throw 'Updater did not repair damaged managed command' }
$entry = @($manifest.files | Where-Object { $_.target -eq 'app/CRAFTING-ADDON.cmd' })[0]
if ((Get-FileHash -LiteralPath $managed -Algorithm SHA256).Hash -ne $entry.sha256.ToUpperInvariant()) {
    throw 'Current command not repaired'
}
Write-Host 'PASS: complete package installs new CMDs, retires owned files, protects unrelated files, handles entire nested-folder revamps, repairs same-version damage, and skips verified packages.'
