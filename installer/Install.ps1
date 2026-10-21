# Unturned Inventory Search - per-user, one-time Windows installer.
# Copies only our mod DLL into BepInEx/plugins, and optionally installs official BepInEx 5 if missing.
# Never edits the game save or dedicated server. Designed for PowerShell 5.1+.
[CmdletBinding()]
param([string] $UnturnedDir)
$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$settings = Get-Content -LiteralPath (Join-Path $here 'CONFIG.json') -Raw | ConvertFrom-Json
# Pin the SHA-256 of the already-tested v0.1.6 DLL. Never trust a missing/broken local payload.
$expectedHash = 'A45DF1EE2202B05A20CDAF98BBFDBD8AD016113E7536381E9A9CB30F072AEFC2'
$hashFile = Join-Path $here 'payload\InventorySearchPrototype.sha256'
if (Test-Path -LiteralPath $hashFile -PathType Leaf) {
    $bundledHash = (Get-Content -LiteralPath $hashFile -Raw).Trim().ToUpperInvariant()
    if ($bundledHash -ne $expectedHash) {
        throw 'Bundled DLL checksum metadata disagrees with this verified release. Download a clean copy from GitHub Releases.'
    }
}
$dllSource = Join-Path $here 'payload\InventorySearchPrototype.dll'
$recoveryPath = $null
if (-not (Test-Path -LiteralPath $dllSource -PathType Leaf)) {
    Write-Warning 'The installer DLL is missing. This may be a GitHub source-code ZIP, incomplete extraction, or antivirus quarantine.'
    Write-Host 'Downloading the tested DLL from the official v1.0.0 GitHub release...' -ForegroundColor Cyan
    $recoveryPath = Join-Path ([IO.Path]::GetTempPath()) ('InventorySearch-' + [guid]::NewGuid().ToString('N') + '.dll')
    $downloadUrl = 'https://github.com/Tac0-0/Unturned-Inventory-Search/releases/download/v1.0.0/InventorySearchPrototype.dll'
    try {
        Invoke-WebRequest -Uri $downloadUrl -OutFile $recoveryPath -UseBasicParsing
        $dllSource = $recoveryPath
    } catch {
        Remove-Item -LiteralPath $recoveryPath -Force -ErrorAction SilentlyContinue
        throw ('Could not download the missing DLL from the official GitHub release: ' + $_.Exception.Message + '. Check antivirus protection/quarantine or download the one-click release ZIP.')
    }
}
$actualDllHash = (Get-FileHash -LiteralPath $dllSource -Algorithm SHA256).Hash.ToUpperInvariant()
if ($actualDllHash -ne $expectedHash) {
    if ($recoveryPath) { Remove-Item -LiteralPath $recoveryPath -Force -ErrorAction SilentlyContinue }
    throw 'InventorySearchPrototype.dll failed the pinned SHA-256 integrity check. Installation stopped; verify the official download and antivirus quarantine.'
}

function Find-Unturned {
    $roots = New-Object 'System.Collections.Generic.List[string]'
    if ($env:USERPROFILE) { $roots.Add((Join-Path $env:USERPROFILE 'scoop\apps\steam\current')) }
    foreach ($reg in @('HKCU:\Software\Valve\Steam','HKLM:\SOFTWARE\WOW6432Node\Valve\Steam','HKLM:\SOFTWARE\Valve\Steam')) {
        try {
            $r = Get-ItemProperty -Path $reg -ErrorAction Stop
            if ($r.SteamPath) { $roots.Add($r.SteamPath) }
            if ($r.InstallPath) { $roots.Add($r.InstallPath) }
        } catch { }
    }
    if (${env:ProgramFiles(x86)}) { $roots.Add((Join-Path ${env:ProgramFiles(x86)} 'Steam')) }
    if ($env:ProgramFiles) { $roots.Add((Join-Path $env:ProgramFiles 'Steam')) }
    $libraryRoots = New-Object 'System.Collections.Generic.List[string]'
    foreach ($root in $roots) {
        if (-not (Test-Path -LiteralPath $root -PathType Container)) { continue }
        $libraryRoots.Add($root)
        $libraryFile = Join-Path $root 'steamapps\libraryfolders.vdf'
        if (Test-Path -LiteralPath $libraryFile -PathType Leaf) {
            foreach ($line in Get-Content -LiteralPath $libraryFile) {
                if ($line -match '"path"\s+"([^"]+)"') {
                    $libraryRoots.Add(($Matches[1] -replace '\\\\','\'))
                }
            }
        }
    }
    foreach ($root in ($libraryRoots | Select-Object -Unique)) {
        $candidate = Join-Path $root 'steamapps\common\Unturned'
        if (Test-Path -LiteralPath (Join-Path $candidate 'Unturned.exe')) {
            return $candidate
        }
    }
    return $null
}

if (-not $UnturnedDir) { $UnturnedDir = Find-Unturned }
if (-not $UnturnedDir) {
    Add-Type -AssemblyName System.Windows.Forms
    $picker = New-Object System.Windows.Forms.FolderBrowserDialog
    $picker.Description = 'Select your Unturned folder (containing Unturned.exe)'
    if ($picker.ShowDialog() -ne [System.Windows.Forms.DialogResult]::OK) {
        throw 'No Unturned installation selected.'
    }
    $UnturnedDir = $picker.SelectedPath
}
$UnturnedDir = (Resolve-Path -LiteralPath $UnturnedDir).Path
if (-not (Test-Path -LiteralPath (Join-Path $UnturnedDir 'Unturned.exe') -PathType Leaf)) {
    throw "Not an Unturned installation: $UnturnedDir"
}
if (-not (Test-Path -LiteralPath (Join-Path $UnturnedDir 'Unturned_Data\Managed\Assembly-CSharp.dll'))) {
    throw 'Unturned managed assemblies were not found. Is this the correct game installation?'
}
if (Get-Process -Name Unturned -ErrorAction SilentlyContinue) {
    throw 'Close Unturned before installing or updating Inventory Search.'
}

# If BepInEx is already installed, leave all its files and other plugins unchanged.
$bepCore = Join-Path $UnturnedDir 'BepInEx\core\BepInEx.dll'
if (-not (Test-Path -LiteralPath $bepCore -PathType Leaf)) {
    $bepDirectory = Join-Path $UnturnedDir 'BepInEx'
    if (Test-Path -LiteralPath $bepDirectory) {
        Write-Host 'Found a partial BepInEx folder. Preserving its existing contents while installing missing files.' -ForegroundColor Yellow
    }
    Write-Host 'BepInEx 5 is missing. Downloading pinned official release...' -ForegroundColor Cyan
    $githubHeaders = @{ 'User-Agent' = 'InventorySearch-Installer'; 'Accept' = 'application/vnd.github+json' }
    $tag = [uri]::EscapeDataString([string]$settings.bepInExVersion)
    $info = Invoke-RestMethod -Uri "https://api.github.com/repos/BepInEx/BepInEx/releases/tags/$tag" -Headers $githubHeaders
    $assetName = "BepInEx_win_x64_$($settings.bepInExVersion.TrimStart('v')).zip"
    $asset = @($info.assets | Where-Object { $_.name -eq $assetName }) | Select-Object -First 1
    if (-not $asset -or $asset.browser_download_url -notlike 'https://github.com/BepInEx/BepInEx/releases/download/*') {
        throw 'Expected official BepInEx asset not found.'
    }
    if ([string]$asset.digest -notmatch '^sha256:[0-9a-fA-F]{64}$') {
        throw 'Official BepInEx release asset has no verifiable SHA-256 digest.'
    }
    $tmp = Join-Path ([IO.Path]::GetTempPath()) ('InventorySearch-BepInEx-' + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $tmp | Out-Null
    try {
        $archive = Join-Path $tmp 'BepInEx.zip'
        Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $archive -UseBasicParsing
        $actual = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
        if ($actual -ne ([string]$asset.digest).Substring(7).ToLowerInvariant()) {
            throw 'BepInEx download failed SHA-256 integrity verification.'
        }
        # The signed/hash-verified upstream ZIP includes an optional changelog.txt,
        # which can be locked by Windows indexing or antivirus during Expand-Archive.
        # Extract ONLY necessary runtime files via .NET and retry transient sharing violations.
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        $zip = [System.IO.Compression.ZipFile]::OpenRead($archive)
        try {
            $runtimeEntries = @($zip.Entries | Where-Object {
                $_.FullName -cmatch '^BepInEx/core/[^/]+$' -or
                $_.FullName -cin @('winhttp.dll', 'doorstop_config.ini', '.doorstop_version')
            })
            foreach ($requiredName in @('BepInEx/core/BepInEx.dll', 'winhttp.dll', 'doorstop_config.ini')) {
                if (-not @($runtimeEntries | Where-Object { $_.FullName -ceq $requiredName }).Count) {
                    throw ("Official BepInEx archive is missing a required runtime file: " + $requiredName)
                }
            }
            foreach ($entry in $runtimeEntries) {
                # Archive paths are restricted by the whitelist above; no arbitrary ZIP paths are installed.
                $relativePath = $entry.FullName.Replace('/', [IO.Path]::DirectorySeparatorChar)
                $destination = Join-Path $UnturnedDir $relativePath
                if (Test-Path -LiteralPath $destination -PathType Leaf) { continue }
                New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force | Out-Null

                $installed = $false
                for ($attempt = 1; $attempt -le 4; $attempt++) {
                    $staged = Join-Path $tmp ('runtime-' + [guid]::NewGuid().ToString('N') + '.tmp')
                    try {
                        [System.IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $staged)
                        if ((Get-Item -LiteralPath $staged).Length -ne $entry.Length) {
                            throw ('BepInEx runtime extraction produced an unexpected file length: ' + $entry.FullName)
                        }
                        # Do not overwrite files from an existing installation.
                        [System.IO.File]::Copy($staged, $destination, $false)
                        $installed = $true
                        break
                    } catch [System.IO.IOException] {
                        if (Test-Path -LiteralPath $destination -PathType Leaf) {
                            # An existing file appeared while installing: preserve it rather than overwrite.
                            if ((Get-Item -LiteralPath $destination).Length -eq $entry.Length) {
                                $installed = $true
                                break
                            }
                            throw ('BepInEx destination file exists but has an unexpected length: ' + $destination)
                        }
                        if ($attempt -eq 4) {
                            throw ('Windows repeatedly locked a temporary BepInEx file: ' + $entry.FullName +
                                '. Close other installers and inspect Windows Security Protection history, then retry. ' +
                                $_.Exception.Message)
                        }
                        Write-Warning ("Temporary file lock while installing " + $entry.FullName +
                            ". Retrying (" + $attempt + "/4)...")
                        Start-Sleep -Milliseconds (400 * $attempt)
                    }
                }
                if (-not $installed) { throw ('BepInEx file was not installed: ' + $entry.FullName) }
            }
        } finally {
            $zip.Dispose()
        }
        if (-not (Test-Path -LiteralPath (Join-Path $UnturnedDir 'BepInEx\core\BepInEx.dll') -PathType Leaf)) {
            throw 'BepInEx core DLL was not installed successfully.'
        }
        Write-Host 'Missing BepInEx 5 files installed from the official release.' -ForegroundColor Green
    } finally {
        Remove-Item -LiteralPath $tmp -Recurse -Force -ErrorAction SilentlyContinue
    }
}

$pluginDirectory = Join-Path $UnturnedDir 'BepInEx\plugins\InventorySearchPrototype'
New-Item -ItemType Directory -Path $pluginDirectory -Force | Out-Null
$installedDll = Join-Path $pluginDirectory 'InventorySearchPrototype.dll'
Copy-Item -LiteralPath $dllSource -Destination $installedDll -Force
if ((Get-FileHash $dllSource -Algorithm SHA256).Hash -ne (Get-FileHash $installedDll -Algorithm SHA256).Hash) {
    throw 'Installed mod DLL failed integrity verification.'
}
Write-Host "Inventory Search installed in: $pluginDirectory" -ForegroundColor Green
Write-Host "Verified DLL SHA-256: $expectedHash"
if ($recoveryPath) { Remove-Item -LiteralPath $recoveryPath -Force -ErrorAction SilentlyContinue }

# Per-user updater installation. No elevated permissions and no other mod changes.
$stateDir = Join-Path $env:LOCALAPPDATA 'InventorySearch'
New-Item -ItemType Directory -Path $stateDir -Force | Out-Null
$state = @{
    gamePath = $UnturnedDir
    repository = [string]$settings.releaseRepository
    assetName = [string]$settings.releaseAsset
} | ConvertTo-Json -Depth 4
Set-Content -LiteralPath (Join-Path $stateDir 'config.json') -Value $state -Encoding UTF8
Copy-Item -LiteralPath (Join-Path $here 'Update.ps1') -Destination (Join-Path $stateDir 'Update.ps1') -Force

if ([string]::IsNullOrWhiteSpace([string]$settings.releaseRepository)) {
    Write-Warning 'Automatic updates are not configured yet: the mod needs an official GitHub Releases repository.'
    Write-Host 'You can play right away. Update scheduling will be enabled with the published release installer.'
} else {
    if ([string]$settings.releaseRepository -notmatch '^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$') {
        throw 'Invalid GitHub owner/repository name in CONFIG.json.'
    }
    try {
        $exe = Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
        $updater = Join-Path $stateDir 'Update.ps1'
        $action = New-ScheduledTaskAction -Execute $exe -Argument ('-NoLogo -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File "' + $updater + '"')
        $triggers = @(
            (New-ScheduledTaskTrigger -Daily -At 12:00),
            (New-ScheduledTaskTrigger -AtLogOn -User ([Security.Principal.WindowsIdentity]::GetCurrent().Name))
        )
        Register-ScheduledTask -TaskName 'Inventory Search Auto Update' -TaskPath '\' -Action $action -Trigger $triggers -Description 'Checks official Inventory Search release for updates, never while Unturned is running.' -Force -ErrorAction Stop | Out-Null
        Write-Host 'Automatic update checks scheduled daily and at sign-in.' -ForegroundColor Green
    } catch {
        Write-Warning ("Task Scheduler could not register the updater: " + $_.Exception.Message)
        Write-Warning 'The mod is installed; updates can still be checked manually with UPDATE-NOW.cmd.'
    }
}
Write-Host ''
Write-Host 'DONE. Launch Unturned WITHOUT BattlEye. No server installation is required.' -ForegroundColor Green