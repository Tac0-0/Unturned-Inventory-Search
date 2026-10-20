# Maintainer step: copies the verified DLL into this installer kit and configures its GitHub update source.
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string] $TestedDll,
    [Parameter(Mandatory=$true)][string] $GitHubRepository
)
$ErrorActionPreference='Stop'
if ($GitHubRepository -notmatch '^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$') { throw 'Expected owner/repository.' }
$here = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$configPath = Join-Path $here 'CONFIG.json'
$config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
$config.releaseRepository = $GitHubRepository
$config | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $configPath -Encoding UTF8
$target = Join-Path $here 'payload\InventorySearchPrototype.dll'
$resolvedDll = (Resolve-Path -LiteralPath $TestedDll).Path
if ($resolvedDll -ne $target) {
    Copy-Item -LiteralPath $resolvedDll -Destination $target -Force
}
$digest = (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash.ToUpperInvariant()
Set-Content -LiteralPath (Join-Path $here 'payload\InventorySearchPrototype.sha256') -Value $digest -Encoding ASCII
Write-Host ('Tested DLL SHA-256: ' + $digest)
Write-Host 'Release kit prepared. Review the DLL and GitHub repository before distributing.'
Write-Host 'Upload exactly this InventorySearchPrototype.dll as a GitHub release asset.'