# Removes Inventory Search only. Never uninstalls BepInEx or touches other plugins/game saves.
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$stateDir = Join-Path $env:LOCALAPPDATA 'InventorySearch'
$configPath = Join-Path $stateDir 'config.json'
if (-not (Test-Path -LiteralPath $configPath)) {
    throw 'Inventory Search installation config not found; no files changed.'
}
if (Get-Process -Name Unturned -ErrorAction SilentlyContinue) {
    throw 'Close Unturned before uninstalling Inventory Search.'
}
$config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
$target = Join-Path ([string]$config.gamePath) 'BepInEx\plugins\InventorySearchPrototype\InventorySearchPrototype.dll'
Write-Host "Inventory Search DLL: $target"
$answer = Read-Host 'Remove Inventory Search and its updater? Type YES to confirm'
if ($answer -cne 'YES') { Write-Host 'Canceled.'; exit 0 }
if (Test-Path -LiteralPath $target -PathType Leaf) {
    Remove-Item -LiteralPath $target -Force
    Write-Host 'Inventory Search DLL removed.'
}
# An optional SmartCraft DLL lives in our owned plugin directory.
# Full Inventory Search uninstall removes it too, but add-on-only uninstall
# is independently available from UNINSTALL-CRAFTING.cmd.
$addonTarget = Join-Path ([string]$config.gamePath) 'BepInEx\plugins\InventorySearchPrototype\SmartCraftCraftingAddon.dll'
if (Test-Path -LiteralPath $addonTarget -PathType Leaf) {
    Remove-Item -LiteralPath $addonTarget -Force
    Write-Host 'Optional SmartCraft crafting DLL removed.'
}
try { Unregister-ScheduledTask -TaskName 'Inventory Search Auto Update' -Confirm:$false -ErrorAction SilentlyContinue | Out-Null }
catch { Write-Warning ('Task removal was unsuccessful: ' + $_.Exception.Message) }
Remove-Item -LiteralPath $stateDir -Force -Recurse -ErrorAction SilentlyContinue
$startMenu = Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs\Inventory Search'
Remove-Item -LiteralPath $startMenu -Recurse -Force -ErrorAction SilentlyContinue
Write-Host 'Uninstalled Inventory Search. BepInEx and all other mods were left untouched.'