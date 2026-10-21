# Inventory Search self-updater. Downloads the COMPLETE release ZIP, not just a DLL.
# Windows PowerShell 5.1. Operates only inside this mod's game plugin folder
# and %LOCALAPPDATA%\InventorySearch. No arbitrary paths from ZIPs are trusted.
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$stateDir = Join-Path $env:LOCALAPPDATA 'InventorySearch'
$configFile = Join-Path $stateDir 'config.json'
if (-not (Test-Path -LiteralPath $configFile -PathType Leaf)) {
    throw 'Inventory Search configuration missing. Run INSTALL.cmd once.'
}
$config = Get-Content -LiteralPath $configFile -Raw | ConvertFrom-Json
if ([string]$config.repository -ne 'Tac0-0/Unturned-Inventory-Search') {
    throw 'Unexpected GitHub repository in local config. Refusing unauthorised update source.'
}
$gameDir = [string]$config.gamePath
if (-not $gameDir -or -not (Test-Path -LiteralPath (Join-Path $gameDir 'Unturned.exe') -PathType Leaf)) {
    throw 'Saved Unturned directory not found. Run INSTALL.cmd to repair the path.'
}
$pluginDir = Join-Path $gameDir 'BepInEx\plugins\InventorySearchPrototype'
$pluginDll = Join-Path $pluginDir 'InventorySearchPrototype.dll'
if (-not (Test-Path -LiteralPath $pluginDll -PathType Leaf)) {
    throw 'Inventory Search DLL is missing. Reinstall the current release once.'
}
if (Get-Process -Name Unturned -ErrorAction SilentlyContinue) {
    Write-Host 'Unturned is running. Update deferred until next check.'
    exit 0
}

# Never have Task Scheduler and manual update replacing the same files together.
$mutex = New-Object System.Threading.Mutex($false, 'Local\InventorySearchSelfUpdate')
$locked = $false
$tmpRoot = $null
try {
    $locked = $mutex.WaitOne(0)
    if (-not $locked) {
        Write-Host 'Another Inventory Search update check is already running.'
        exit 0
    }
    $headers = @{ 'User-Agent'='InventorySearch-Updater'; 'Accept'='application/vnd.github+json' }
    $api = 'https://api.github.com/repos/Tac0-0/Unturned-Inventory-Search/releases?per_page=30'
    Write-Host 'Checking Inventory Search releases on GitHub...'
    $response = Invoke-RestMethod -Uri $api -Headers $headers
    $releases = New-Object 'System.Collections.Generic.List[object]'
    function Collect-Releases([object]$item) {
        if ($null -eq $item) { return }
        if ($item -is [array]) {
            foreach ($child in $item) { Collect-Releases $child }
        } else {
            [void]$releases.Add($item)
        }
    }
    Collect-Releases $response

    $choice = $null
    $asset = $null
    $bestVersion = $null
    foreach ($release in $releases) {
        if ($release.draft -or $release.prerelease) { continue }
        if ([string]$release.tag_name -cnotmatch '^v(\d+)\.(\d+)\.(\d+)$') { continue }
        $version = [version]$release.tag_name.Substring(1)
        $wantedAsset = 'InventorySearch-OneClick-' + $release.tag_name + '.zip'
        foreach ($candidate in @($release.assets)) {
            if ([string]$candidate.name -ceq $wantedAsset) {
                if ($null -eq $bestVersion -or $version -gt $bestVersion) {
                    $bestVersion = $version
                    $choice = $release
                    $asset = $candidate
                }
                break
            }
        }
    }
    if ($null -eq $asset) { throw 'No complete stable Inventory Search ZIP release was found.' }
    $tag = [string]$choice.tag_name
    if ([string]$asset.digest -notmatch '^sha256:[0-9A-Fa-f]{64}$') {
        throw 'The release ZIP has no GitHub SHA-256 digest. Refusing unverified update.'
    }
    $expectedZipHash = ([string]$asset.digest).Substring(7).ToUpperInvariant()
    $expectedUrl = 'https://github.com/Tac0-0/Unturned-Inventory-Search/releases/download/' + $tag + '/InventorySearch-OneClick-' + $tag + '.zip'
    if ([string]$asset.browser_download_url -cne $expectedUrl) {
        throw 'Release asset URL is unexpected. Refusing untrusted download.'
    }
    $installedVersion = $null
    if ($config.PSObject.Properties['installedVersion'] -and
        [string]$config.installedVersion -match '^v?(\d+)\.(\d+)\.(\d+)$') {
        $installedVersion = [version](([string]$config.installedVersion).TrimStart('v'))
    }
    $dllAsset = @($choice.assets | Where-Object { $_.name -eq 'InventorySearchPrototype.dll' }) | Select-Object -First 1
    $sameDll = $false
    if ($dllAsset -and [string]$dllAsset.digest -match '^sha256:[0-9A-Fa-f]{64}$') {
        $sameDll = ((Get-FileHash -LiteralPath $pluginDll -Algorithm SHA256).Hash -eq
                    ([string]$dllAsset.digest).Substring(7).ToUpperInvariant())
    }
    Write-Host ('Latest release: ' + $tag)
    if ($null -ne $installedVersion -and $installedVersion -gt $bestVersion) {
        Write-Host 'Installed version is newer. Downgrade skipped.'
        exit 0
    }
    if ($null -ne $installedVersion -and $installedVersion -eq $bestVersion -and $sameDll) {
        Write-Host 'Inventory Search is already up to date.'
        exit 0
    }

    $tmpRoot = Join-Path ([IO.Path]::GetTempPath()) ('InventorySearch-Update-' + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $tmpRoot | Out-Null
    $archive = Join-Path $tmpRoot 'release.zip'
    Write-Host ('Downloading COMPLETE ' + $tag + ' release package...')
    Invoke-WebRequest -Uri $expectedUrl -OutFile $archive -UseBasicParsing
    if ((Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash -ne $expectedZipHash) {
        throw 'Release ZIP checksum mismatch. Installed files were not changed.'
    }

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zip = [System.IO.Compression.ZipFile]::OpenRead($archive)
    $files = New-Object 'System.Collections.Generic.List[object]'
    $seen = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
    try {
        $root = 'InventorySearch-OneClick-' + $tag + '/'
        $manifestEntries = @($zip.Entries | Where-Object { $_.FullName -ceq ($root + 'update-manifest.json') })
        if ($manifestEntries.Count -ne 1) {
            throw 'Release ZIP lacks a unique update-manifest.json. No files changed.'
        }
        $reader = [IO.StreamReader]::new($manifestEntries[0].Open(), [Text.Encoding]::UTF8)
        try { $manifestText = $reader.ReadToEnd() } finally { $reader.Dispose() }
        $manifest = $manifestText | ConvertFrom-Json
        if ([string]$manifest.version -cne $tag) { throw 'Update manifest version does not match release tag.' }
        if (-not $manifest.files -or @($manifest.files).Count -gt 100) {
            throw 'Update manifest is empty or too large.'
        }

        $foundDll = $false
        $foundUpdater = $false
        foreach ($item in $manifest.files) {
            $source = [string]$item.source
            $target = [string]$item.target
            $digest = [string]$item.sha256
            # Strict relative paths: no traversal, drive letters, whitespace tricks, or backslashes.
            $safePath = '^[A-Za-z0-9_.-]+(/[A-Za-z0-9_.-]+)*$'
            if ($source -cnotmatch $safePath -or $target -cnotmatch $safePath -or
                $source -match '(^|/)\.\.?(/|$)' -or $target -match '(^|/)\.\.?(/|$)' -or
                $digest -notmatch '^[A-Fa-f0-9]{64}$') {
                throw ('Invalid update manifest file mapping: ' + $source)
            }
            $pieces = $target.Split('/')
            if ($pieces.Length -lt 2 -or $pieces[0] -cnotin @('app', 'plugin')) {
                throw ('File destination outside Inventory Search directories: ' + $target)
            }
            if (-not $seen.Add($target)) { throw ('Duplicate manifest destination: ' + $target) }
            $relative = [string]::Join([IO.Path]::DirectorySeparatorChar.ToString(), $pieces[1..($pieces.Length-1)])
            $baseDir = if ($pieces[0] -ceq 'app') { $stateDir } else { $pluginDir }
            $dest = Join-Path $baseDir $relative
            if ($target -ceq 'plugin/InventorySearchPrototype.dll') { $foundDll = $true }
            if ($target -ceq 'app/Update.ps1') { $foundUpdater = $true }
            $entryPath = $root + $source
            $entries = @($zip.Entries | Where-Object { $_.FullName -ceq $entryPath })
            if ($entries.Count -ne 1) { throw ('Missing/duplicate ZIP file: ' + $entryPath) }
            if ($entries[0].Length -gt 52428800) { throw ('Oversized release file: ' + $entryPath) }
            $staged = Join-Path $tmpRoot ('stage-' + $files.Count)
            [IO.Compression.ZipFileExtensions]::ExtractToFile($entries[0], $staged)
            if ((Get-FileHash -LiteralPath $staged -Algorithm SHA256).Hash -ne $digest.ToUpperInvariant()) {
                throw ('SHA-256 mismatch inside release package: ' + $source)
            }
            [void]$files.Add([pscustomobject]@{ Destination=$dest; Staged=$staged })
        }
        if (-not $foundDll -or -not $foundUpdater) {
            throw 'Update manifest must contain the mod DLL and its updater.'
        }
    } finally {
        $zip.Dispose()
    }

    if (Get-Process -Name Unturned -ErrorAction SilentlyContinue) {
        Write-Host 'Unturned opened during download. No files replaced. Try after closing it.'
        exit 0
    }

    # Stage and authenticate everything FIRST, then replace managed files with rollback.
    $backups = New-Object 'System.Collections.Generic.List[object]'
    try {
        foreach ($file in $files) {
            $dest = [string]$file.Destination
            $existed = Test-Path -LiteralPath $dest -PathType Leaf
            $backup = Join-Path $tmpRoot ('backup-' + $backups.Count)
            if ($existed) { Copy-Item -LiteralPath $dest -Destination $backup -Force }
            [void]$backups.Add([pscustomobject]@{ Destination=$dest; Existed=$existed; Backup=$backup })
            New-Item -ItemType Directory -Path (Split-Path -Parent $dest) -Force | Out-Null
            Copy-Item -LiteralPath $file.Staged -Destination $dest -Force
            if ((Get-FileHash -LiteralPath $dest -Algorithm SHA256).Hash -ne
                (Get-FileHash -LiteralPath $file.Staged -Algorithm SHA256).Hash) {
                throw ('Post-install checksum mismatch: ' + $dest)
            }
        }
        # Preserve install-specific gamePath/repository config, only update version marker.
        if ($config.PSObject.Properties['installedVersion']) {
            $config.installedVersion = $tag
        } else {
            Add-Member -InputObject $config -NotePropertyName installedVersion -NotePropertyValue $tag
        }
        $config | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $configFile -Encoding UTF8
    } catch {
        $originalError = $_
        for ($i=$backups.Count-1; $i -ge 0; $i--) {
            $item = $backups[$i]
            try {
                if ($item.Existed) {
                    Copy-Item -LiteralPath $item.Backup -Destination $item.Destination -Force
                } else {
                    Remove-Item -LiteralPath $item.Destination -Force -ErrorAction SilentlyContinue
                }
            } catch { Write-Warning ('Rollback failed for ' + $item.Destination + ': ' + $_.Exception.Message) }
        }
        throw ('Update failed; attempted to restore the previous installation. ' + $originalError.Exception.Message)
    }
    Write-Host ('Inventory Search updated from official release: ' + $tag)
    Write-Host 'Updated plugin DLL, updater components, and release files.'
} finally {
    if ($tmpRoot) { Remove-Item -LiteralPath $tmpRoot -Force -Recurse -ErrorAction SilentlyContinue }
    if ($locked) { $mutex.ReleaseMutex() }
    $mutex.Dispose()
}
