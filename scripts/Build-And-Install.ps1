<#
Build Inventory Search Prototype against YOUR installed Unturned assemblies.
No Steam files are replaced, and nothing touches a dedicated server.

Examples:
  .\scripts\Build-And-Install.ps1 -UnturnedDir 'D:\SteamLibrary\steamapps\common\Unturned'
  .\scripts\Build-And-Install.ps1 -UnturnedDir 'D:\SteamLibrary\steamapps\common\Unturned' -Install
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $UnturnedDir,
    [switch] $Install
)

$ErrorActionPreference = 'Stop'
$UnturnedDir = (Resolve-Path -LiteralPath $UnturnedDir).Path
$project = Join-Path (Split-Path -Parent $PSScriptRoot) 'InventorySearchPrototype.csproj'
$assembly = Join-Path $UnturnedDir 'Unturned_Data\Managed\Assembly-CSharp.dll'
$glazier = Join-Path $UnturnedDir 'Unturned_Data\Managed\SDG.Glazier.Runtime.dll'
$bepinex = Join-Path $UnturnedDir 'BepInEx\core\BepInEx.dll'
$harmony = Join-Path $UnturnedDir 'BepInEx\core\0Harmony.dll'

if (-not (Test-Path -LiteralPath $assembly)) {
    throw "Unturned client Assembly-CSharp.dll not found at: $assembly"
}
if (-not (Test-Path -LiteralPath $glazier)) {
    throw "Unturned UI assembly missing: $glazier. Check the game installation and game version."
}
if (-not (Test-Path -LiteralPath $bepinex)) {
    throw "BepInEx 5 not found at: $bepinex. Install BepInEx 5 for Mono first (no-BattlEye only)."
}
if (-not (Test-Path -LiteralPath $harmony)) {
    throw "BepInEx Harmony DLL missing: $harmony"
}
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'dotnet SDK not found. Install the .NET SDK and .NET Framework 4.8 Developer Pack (or Visual Studio .NET desktop tooling).'
}

Write-Host 'Building read-only inventory search prototype...' -ForegroundColor Cyan
& dotnet build $project --configuration Release "-p:UnturnedDir=$UnturnedDir" --nologo
if ($LASTEXITCODE -ne 0) {
    throw 'Build failed. See compiler errors above; no game files were changed.'
}

$dll = Join-Path (Split-Path -Parent $PSScriptRoot) 'bin\Release\net48\InventorySearchPrototype.dll'
if (-not (Test-Path -LiteralPath $dll)) { throw "Built DLL missing: $dll" }
Write-Host "Compiled DLL: $dll" -ForegroundColor Green

if ($Install) {
    if (Get-Process -Name Unturned -ErrorAction SilentlyContinue) {
        throw 'Close Unturned before installing the DLL.'
    }
    $destination = Join-Path $UnturnedDir 'BepInEx\plugins\InventorySearchPrototype'
    New-Item -ItemType Directory -Path $destination -Force | Out-Null
    Copy-Item -LiteralPath $dll -Destination (Join-Path $destination 'InventorySearchPrototype.dll') -Force
    Write-Host "Installed into: $destination" -ForegroundColor Green
    Write-Host 'Launch Unturned WITHOUT BattlEye. Test in a backed-up singleplayer world.'
} else {
    Write-Host 'Build only. Pass -Install to copy the plugin into BepInEx/plugins.'
}