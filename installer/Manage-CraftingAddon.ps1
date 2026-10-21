# Inventory Search: opt-in SmartCraft crafting add-on manager.
# Does not modify the Inventory Search DLL, BepInEx runtime, saves, or other plugins.
# Windows PowerShell 5.1 compatible.
[CmdletBinding()]
param(
    [ValidateSet('Prompt', 'PromptOptIn', 'Install', 'Uninstall', 'Status')]
    [string]$Action = 'Prompt'
)
$ErrorActionPreference = 'Stop'
$stateDir = Join-Path $env:LOCALAPPDATA 'InventorySearch'
$configPath = Join-Path $stateDir 'config.json'
$choicePath = Join-Path $stateDir 'crafting-addon.json'
if (-not (Test-Path -LiteralPath $configPath -PathType Leaf)) {
    throw 'Inventory Search is not installed. Install the main mod first.'
}
$config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
$gameDir = [string]$config.gamePath
if (-not $gameDir -or -not (Test-Path -LiteralPath (Join-Path $gameDir 'Unturned.exe') -PathType Leaf)) {
    throw 'Saved Unturned path not found. Run the Inventory Search installer to repair it.'
}
$addonDll = Join-Path $gameDir 'BepInEx\plugins\InventorySearchPrototype\SmartCraftCraftingAddon.dll'

function Get-AddonEnabled {
    if (-not (Test-Path -LiteralPath $choicePath -PathType Leaf)) { return $false }
    try {
        $choice = Get-Content -LiteralPath $choicePath -Raw | ConvertFrom-Json
        return ($choice.enabled -eq $true)
    } catch {
        Write-Warning 'Could not read add-on preference. Defaulting to disabled.'
        return $false
    }
}
function Save-AddonPreference([bool]$enabled) {
    @{ schemaVersion = 1; enabled = $enabled } |
        ConvertTo-Json | Set-Content -LiteralPath $choicePath -Encoding UTF8
}
function Assert-GameClosed {
    if (Get-Process -Name Unturned -ErrorAction SilentlyContinue) {
        throw 'Close Unturned before installing or removing the crafting add-on.'
    }
}
if ($Action -eq 'PromptOptIn') {
    if (Test-Path -LiteralPath $choicePath -PathType Leaf) {
        Write-Host 'Existing SmartCraft choice preserved. Run CRAFTING-ADDON.cmd to change it.'
        exit 0
    }
    Write-Host ''
    Write-Host 'Inventory Search v1.2: SmartCraft optional add-on preference'
    Write-Host 'Are you a member of the private Unturned server using SmartCraft?'
    Write-Host 'SmartCraft crafting is NOT implemented in v1.2; engine development begins in v1.3.'
    $answer = Read-Host 'Would you like to enable SmartCraft for future releases? [y/N]'
    if ([string]$answer -match '^[Yy]$') {
        Save-AddonPreference $true
        Write-Host 'SmartCraft selected for future versions. No crafting DLL is installed by v1.2.' -ForegroundColor Green
    } else {
        Save-AddonPreference $false
        Write-Host 'SmartCraft declined. Inventory Search continues normally.'
    }
    exit 0
}
if ($Action -eq 'Prompt') {
    Write-Host ''
    Write-Host 'SmartCraft Crafting Add-on (optional, for private server members)'
    Write-Host ('Opted in: ' + (Get-AddonEnabled))
    Write-Host ('DLL installed: ' + (Test-Path -LiteralPath $addonDll -PathType Leaf))
    Write-Host '1. Install / enable the add-on'
    Write-Host '2. Uninstall / disable the add-on'
    Write-Host '3. Show status'
    Write-Host '0. Cancel'
    $answer = (Read-Host 'Choose 0, 1, 2, or 3').Trim()
    switch ($answer) {
        '1' { $Action = 'Install' }
        '2' { $Action = 'Uninstall' }
        '3' { $Action = 'Status' }
        default { Write-Host 'No changes made.'; exit 0 }
    }
}
if ($Action -eq 'Status') {
    Write-Host ('SmartCraft enabled: ' + (Get-AddonEnabled))
    Write-Host ('SmartCraft DLL exists: ' + (Test-Path -LiteralPath $addonDll -PathType Leaf))
    exit 0
}
Assert-GameClosed
if ($Action -eq 'Uninstall') {
    # Explicit opt-out survives future unattended Inventory Search updates.
    if (Test-Path -LiteralPath $addonDll -PathType Leaf) {
        Remove-Item -LiteralPath $addonDll -Force
    }
    Save-AddonPreference $false
    Write-Host 'SmartCraft crafting add-on removed and future installation disabled.' -ForegroundColor Green
    Write-Host 'Inventory Search, BepInEx, other plugins, and game saves were not changed.'
    exit 0
}
if ($Action -eq 'Install') {
    # v1.2 is a preference-only release. Never download a placeholder DLL or
    # claim the crafting engine exists before the v1.3 implementation.
    $installedVersion = [version]'0.0.0'
    if ([string]$config.installedVersion -match '^v?(\d+)\.(\d+)\.(\d+)$') {
        $installedVersion = [version]([string]$config.installedVersion).TrimStart('v')
    }
    if ($installedVersion -lt [version]'1.3.0') {
        Save-AddonPreference $true
        Write-Host 'SmartCraft opted in for future updates. There is no crafting DLL in v1.2.' -ForegroundColor Green
        exit 0
    }
    $updater = Join-Path $stateDir 'Update.ps1'
    if (-not (Test-Path -LiteralPath $updater -PathType Leaf)) {
        throw 'Inventory Search full-package updater is missing. Reinstall the current release once.'
    }
    $hadChoice = Test-Path -LiteralPath $choicePath -PathType Leaf
    $oldChoice = if ($hadChoice) { Get-Content -LiteralPath $choicePath -Raw } else { $null }
    Save-AddonPreference $true
    try {
        # The trusted updater authenticates the entire release ZIP and the add-on's
        # per-file SHA-256 before installing it. A same-version repair is permitted.
        & $updater -ForceAddonRepair
        if (-not (Test-Path -LiteralPath $addonDll -PathType Leaf)) {
            throw 'The official release does not contain the SmartCraft add-on yet.'
        }
        Write-Host 'SmartCraft crafting add-on installed and enabled.' -ForegroundColor Green
        Write-Host 'Future official updates will keep the add-on updated until you opt out.'
    } catch {
        if ($hadChoice) {
            Set-Content -LiteralPath $choicePath -Value $oldChoice -Encoding UTF8
        } else {
            Remove-Item -LiteralPath $choicePath -Force -ErrorAction SilentlyContinue
        }
        throw ('SmartCraft add-on installation failed; previous preference restored. ' + $_.Exception.Message)
    }
}
