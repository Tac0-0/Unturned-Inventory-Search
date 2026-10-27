# Show the LIVE installed Inventory Search version, not the static extracted ZIP.
# Windows PowerShell 5.1, read-only. No updates, deletes or network requests.
$ErrorActionPreference = 'Stop'
$stateDir = Join-Path $env:LOCALAPPDATA 'InventorySearch'
$configPath = Join-Path $stateDir 'config.json'
$manifestPath = Join-Path $stateDir 'installed-manifest.json'
if (-not (Test-Path -LiteralPath $configPath -PathType Leaf)) {
    throw 'No installed Inventory Search was found. Run INSTALL.cmd from a verified release ZIP.'
}
$config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
$installedVersion = [string]$config.installedVersion
$plugin = Join-Path ([string]$config.gamePath) 'BepInEx\plugins\InventorySearchPrototype\InventorySearchPrototype.dll'
Write-Host ('Installed Inventory Search: ' + $installedVersion) -ForegroundColor Cyan
Write-Host ('Active game plugin: ' + $plugin)
$readme = Join-Path $stateDir 'README.md'
Write-Host ('Current installed README: ' + $readme)
if (-not (Test-Path -LiteralPath $readme -PathType Leaf)) {
    Write-Warning 'Installed README is missing; run UPDATE-NOW.cmd to repair the complete package.'
}
if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
    Write-Warning 'Installed manifest missing; run UPDATE-NOW.cmd for a complete-package repair.'
} else {
    $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    if ([string]$manifest.version -ne $installedVersion) {
        Write-Warning 'Stored version and installed file manifest disagree; run UPDATE-NOW.cmd.'
    } else {
        $core = @($manifest.files | Where-Object {
            $_.target -ceq 'plugin/InventorySearchPrototype.dll'
        })
        if ($core.Count -ne 1 -or -not (Test-Path -LiteralPath $plugin -PathType Leaf)) {
            Write-Warning 'Main plugin file missing from the installed manifest or game. Run UPDATE-NOW.cmd.'
        } elseif ((Get-FileHash -LiteralPath $plugin -Algorithm SHA256).Hash -ne
                ([string]$core[0].sha256).ToUpperInvariant()) {
            Write-Warning 'Installed plugin checksum differs from the release manifest. Run UPDATE-NOW.cmd.'
        } else {
            Write-Host 'Main plugin verified against installed release manifest.' -ForegroundColor Green
        }
    }
}
Write-Host ''
Write-Host 'The old extracted InventorySearch-OneClick-vX.Y.Z folder is only a download snapshot.'
Write-Host 'It is not the live installation and is deliberately NOT renamed or modified.'
Write-Host 'You can delete an old extracted installer folder after installation.'
