[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$GamePath,

    [Parameter(Mandatory = $false)]
    [string]$SourceDll
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSHOME 'Modules\Microsoft.PowerShell.Utility\Microsoft.PowerShell.Utility.psd1') -ErrorAction Stop

function Write-Step([string]$Message) {
    Write-Host "[SRMP] $Message" -ForegroundColor Cyan
}

function Fail([string]$Message, [int]$Code = 1) {
    Write-Host "[SRMP] ERROR: $Message" -ForegroundColor Red
    exit $Code
}

function Get-FullPathSafe([string]$Path) {
    if ([string]::IsNullOrWhiteSpace($Path)) { return $null }
    try { return [System.IO.Path]::GetFullPath($Path.Trim('"')) } catch { return $null }
}

function Test-SlimeRancherPath([string]$Path) {
    $full = Get-FullPathSafe $Path
    if (-not $full) { return $false }
    return (Test-Path -LiteralPath (Join-Path $full 'SlimeRancher.exe') -PathType Leaf) -and
           (Test-Path -LiteralPath (Join-Path $full 'SlimeRancher_Data') -PathType Container)
}

function Test-GameProcessRunning([string]$GamePath) {
    $expectedExe = [System.IO.Path]::GetFullPath((Join-Path $GamePath 'SlimeRancher.exe'))
    foreach ($process in (Get-Process -Name 'SlimeRancher' -ErrorAction SilentlyContinue)) {
        try {
            $runningExe = [System.IO.Path]::GetFullPath($process.Path)
            if ([string]::Equals($runningExe, $expectedExe, [System.StringComparison]::OrdinalIgnoreCase)) {
                return $true
            }
        } catch {
            # If the running executable cannot be identified, fail closed.
            return $true
        }
    }
    return $false
}

function Add-UniquePath([System.Collections.Generic.List[string]]$List, [string]$Path) {
    $full = Get-FullPathSafe $Path
    if (-not $full) { return }
    foreach ($existing in $List) {
        if ([string]::Equals($existing, $full, [System.StringComparison]::OrdinalIgnoreCase)) { return }
    }
    $List.Add($full)
}

function Find-NearbySlimeRancher {
    $candidates = New-Object 'System.Collections.Generic.List[string]'
    $startPaths = @($PSScriptRoot, (Get-Location).Path)

    foreach ($startPath in $startPaths) {
        if (-not (Test-Path -LiteralPath $startPath -PathType Container)) { continue }
        $probe = Get-Item -LiteralPath $startPath
        $depth = 0
        while ($probe -and $depth -le 4) {
            if (Test-SlimeRancherPath $probe.FullName) { Add-UniquePath $candidates $probe.FullName }
            try {
                foreach ($child in (Get-ChildItem -LiteralPath $probe.FullName -Directory -Force -ErrorAction Stop)) {
                    if (Test-SlimeRancherPath $child.FullName) { Add-UniquePath $candidates $child.FullName }
                }
            } catch { }

            $probe = $probe.Parent
            $depth++
        }
    }

    if ($candidates.Count -eq 1) { return $candidates[0] }
    if ($candidates.Count -gt 1) {
        $withSrml = @($candidates | Where-Object {
            Test-Path -LiteralPath (Join-Path $_ 'SRML\Mods') -PathType Container
        })
        if ($withSrml.Count -eq 1) { return $withSrml[0] }
        throw "More than one nearby Slime Rancher install was found. Re-run with -GamePath to choose one: $($candidates -join '; ')"
    }
    return $null
}

function Get-SteamRoots {
    $roots = New-Object 'System.Collections.Generic.List[string]'

    if (${env:ProgramFiles(x86)}) { Add-UniquePath $roots (Join-Path ${env:ProgramFiles(x86)} 'Steam') }
    if ($env:ProgramFiles) { Add-UniquePath $roots (Join-Path $env:ProgramFiles 'Steam') }

    $registryCandidates = @(
        'HKCU:\SOFTWARE\Valve\Steam',
        'HKLM:\SOFTWARE\WOW6432Node\Valve\Steam',
        'HKLM:\SOFTWARE\Valve\Steam'
    )

    foreach ($key in $registryCandidates) {
        try {
            $value = Get-ItemProperty -Path $key -ErrorAction Stop
            if ($value.PSObject.Properties['SteamPath'] -and $value.SteamPath) { Add-UniquePath $roots $value.SteamPath }
            if ($value.PSObject.Properties['InstallPath'] -and $value.InstallPath) { Add-UniquePath $roots $value.InstallPath }
        } catch { }
    }

    return $roots
}

function Get-SteamLibraries {
    $libraries = New-Object 'System.Collections.Generic.List[string]'

    foreach ($steamRoot in (Get-SteamRoots)) {
        if (-not (Test-Path -LiteralPath $steamRoot -PathType Container)) { continue }
        Add-UniquePath $libraries $steamRoot

        $vdf = Join-Path $steamRoot 'steamapps\libraryfolders.vdf'
        if (-not (Test-Path -LiteralPath $vdf -PathType Leaf)) { continue }

        try {
            foreach ($line in (Get-Content -LiteralPath $vdf -ErrorAction Stop)) {
                $candidate = $null
                if ($line -match '"path"\s+"([^"]+)"') {
                    $candidate = $Matches[1]
                } elseif ($line -match '^\s*"\d+"\s+"([^"]+)"') {
                    $candidate = $Matches[1]
                }

                if ($candidate) {
                    $candidate = $candidate -replace '\\\\', '\'
                    Add-UniquePath $libraries $candidate
                }
            }
        } catch {
            Write-Warning "Could not parse Steam library file: $vdf"
        }
    }

    return $libraries
}

function Find-SlimeRancher {
    $candidates = New-Object 'System.Collections.Generic.List[string]'

    foreach ($library in (Get-SteamLibraries)) {
        Add-UniquePath $candidates (Join-Path $library 'steamapps\common\Slime Rancher')
    }

    if ($env:ProgramFiles) {
        Add-UniquePath $candidates (Join-Path $env:ProgramFiles 'Epic Games\SlimeRancher')
        Add-UniquePath $candidates (Join-Path $env:ProgramFiles 'Slime Rancher')
    }
    if (${env:ProgramFiles(x86)}) {
        Add-UniquePath $candidates (Join-Path ${env:ProgramFiles(x86)} 'Slime Rancher')
    }

    foreach ($candidate in $candidates) {
        if (Test-SlimeRancherPath $candidate) { return $candidate }
    }

    return $null
}

Write-Host ''
Write-Host '=============================================' -ForegroundColor Magenta
Write-Host '        SRMP Revival - One Click Setup       ' -ForegroundColor Magenta
Write-Host '=============================================' -ForegroundColor Magenta
Write-Host ''

if ($GamePath) {
    $resolvedGame = Get-FullPathSafe $GamePath
    if (-not (Test-SlimeRancherPath $resolvedGame)) {
        Fail "The supplied -GamePath is not a valid Slime Rancher 1 installation: $GamePath" 11
    }
} else {
    Write-Step 'Looking for a portable Slime Rancher beside the installer...'
    $resolvedGame = Find-NearbySlimeRancher
    if (-not $resolvedGame) {
        Write-Step 'No nearby portable game found; checking common Steam/Epic locations...'
        $resolvedGame = Find-SlimeRancher
    }
    if (-not $resolvedGame) {
        Fail 'Could not locate Slime Rancher 1 automatically. Put the installer beside the game or re-run with -GamePath "C:\Path\To\Slime Rancher".' 12
    }
}

if (Test-GameProcessRunning $resolvedGame) {
    Fail "Slime Rancher is currently running from '$resolvedGame'. Close that copy before installing." 10
}

Write-Step "Game found: $resolvedGame"

$modsPath = Join-Path $resolvedGame 'SRML\Mods'
if (-not (Test-Path -LiteralPath $modsPath -PathType Container)) {
    Fail "The SRML Mods folder does not exist at '$modsPath'. Install SRML for Slime Rancher 1 first, launch the game once, then run this installer again." 13
}

$managedPath = Join-Path $resolvedGame 'SlimeRancher_Data\Managed'
if (-not (Test-Path -LiteralPath $managedPath -PathType Container)) {
    Fail 'SlimeRancher_Data\Managed is missing; refusing to modify this installation.' 14
}

$packageRoot = Split-Path -Parent $PSScriptRoot
$bundledDll = Join-Path $packageRoot 'payload\SRMP.dll'

if ($SourceDll) {
    $resolvedSource = Get-FullPathSafe $SourceDll
} else {
    $resolvedSource = $bundledDll
}

if (-not $resolvedSource -or -not (Test-Path -LiteralPath $resolvedSource -PathType Leaf)) {
    Fail "SRMP.dll was not found. Release packages must contain payload\SRMP.dll. Developers can use -SourceDll 'C:\path\to\SRMP.dll'." 15
}

if (-not [string]::Equals([System.IO.Path]::GetExtension($resolvedSource), '.dll', [System.StringComparison]::OrdinalIgnoreCase)) {
    Fail "Source file is not a DLL: $resolvedSource" 16
}

try {
    $hash = (Get-FileHash -LiteralPath $resolvedSource -Algorithm SHA256).Hash
} catch {
    Fail "Could not hash SRMP.dll: $($_.Exception.Message)" 17
}

$destination = Join-Path $modsPath 'SRMP.dll'
$srmlPath = Split-Path -Parent $modsPath
$backupDir = Join-Path $srmlPath 'Backups\SRMP'
$legacyBackupDir = Join-Path $modsPath 'SRMP-Backups'
$manifestPath = Join-Path $modsPath 'SRMP.install.json'
$previousBackup = $null

if (Test-Path -LiteralPath $manifestPath -PathType Leaf) {
    try {
        $oldManifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
        if ($oldManifest.PreviousBackup) { $previousBackup = [string]$oldManifest.PreviousBackup }
    } catch {
        Write-Warning 'The existing install manifest could not be read; its rollback reference will be replaced.'
    }
}

# SRML scans subdirectories of Mods for DLLs. Older installer builds placed
# rollback DLLs there, which made every upgrade look like a duplicate SRMP mod.
if (Test-Path -LiteralPath $legacyBackupDir -PathType Container) {
    $legacyRoot = [System.IO.Path]::GetFullPath($legacyBackupDir).TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar
    $modsRoot = [System.IO.Path]::GetFullPath($modsPath).TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar
    if (-not $legacyRoot.StartsWith($modsRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        Fail "Legacy backup migration path is outside SRML\Mods: $legacyRoot" 20
    }

    $legacyDlls = @(Get-ChildItem -LiteralPath $legacyBackupDir -File -Filter '*.dll')
    if ($legacyDlls.Count -gt 0) {
        New-Item -ItemType Directory -Path $backupDir -Force | Out-Null
        foreach ($legacyDll in $legacyDlls) {
            $legacyFile = [System.IO.Path]::GetFullPath($legacyDll.FullName)
            if (-not $legacyFile.StartsWith($legacyRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
                Fail "Legacy backup file is outside its expected directory: $legacyFile" 21
            }

            $migratedBackup = Join-Path $backupDir $legacyDll.Name
            if (Test-Path -LiteralPath $migratedBackup -PathType Leaf) {
                $migratedBackup = Join-Path $backupDir ("{0}-{1}.dll" -f $legacyDll.BaseName, [Guid]::NewGuid().ToString('N'))
            }

            Move-Item -LiteralPath $legacyFile -Destination $migratedBackup
            if ($previousBackup -and [string]::Equals([System.IO.Path]::GetFullPath($previousBackup), $legacyFile, [System.StringComparison]::OrdinalIgnoreCase)) {
                $previousBackup = $migratedBackup
            }
            Write-Step "Moved legacy rollback DLL outside SRML\\Mods: $migratedBackup"
        }

        if ($previousBackup) {
            $oldManifest.PreviousBackup = $previousBackup
            $oldManifest | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $manifestPath -Encoding UTF8
        }
    }
}

if (Test-Path -LiteralPath $destination -PathType Leaf) {
    $existingHash = (Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash
    if (-not [string]::Equals($existingHash, $hash, [System.StringComparison]::OrdinalIgnoreCase)) {
        New-Item -ItemType Directory -Path $backupDir -Force | Out-Null
        $stamp = Get-Date -Format 'yyyyMMdd-HHmmss-fff'
        $previousBackup = Join-Path $backupDir "SRMP-$stamp-$PID.dll"
        Write-Step "Backing up existing SRMP.dll to: $previousBackup"
        Copy-Item -LiteralPath $destination -Destination $previousBackup
        $backupHash = (Get-FileHash -LiteralPath $previousBackup -Algorithm SHA256).Hash
        if (-not [string]::Equals($existingHash, $backupHash, [System.StringComparison]::OrdinalIgnoreCase)) {
            Fail 'Rollback backup checksum verification failed.' 22
        }
    } elseif ($previousBackup -and -not (Test-Path -LiteralPath $previousBackup -PathType Leaf)) {
        $previousBackup = $null
    }
}

$tempDestination = "$destination.new"
try {
    Write-Step 'Installing SRMP.dll...'
    Copy-Item -LiteralPath $resolvedSource -Destination $tempDestination -Force

    $copiedHash = (Get-FileHash -LiteralPath $tempDestination -Algorithm SHA256).Hash
    if (-not [string]::Equals($hash, $copiedHash, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw 'SHA256 validation failed after copy.'
    }

    Move-Item -LiteralPath $tempDestination -Destination $destination -Force

    $manifest = [ordered]@{
        FormatVersion   = 1
        Product         = 'SRMP Revival'
        InstalledUtc    = [DateTime]::UtcNow.ToString('o')
        GamePath        = $resolvedGame
        ModsPath        = $modsPath
        InstalledFile   = $destination
        SourceSha256    = $hash
        PreviousBackup  = $previousBackup
    }
    $manifest | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $manifestPath -Encoding UTF8
} catch {
    if (Test-Path -LiteralPath $tempDestination) {
        Remove-Item -LiteralPath $tempDestination -Force -ErrorAction SilentlyContinue
    }
    Fail "Installation failed: $($_.Exception.Message)" 18
}

$finalHash = (Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash
if (-not [string]::Equals($hash, $finalHash, [System.StringComparison]::OrdinalIgnoreCase)) {
    Fail 'Final SRMP.dll checksum verification failed.' 19
}

Write-Host ''
Write-Host '[SRMP] SUCCESS' -ForegroundColor Green
Write-Host "[SRMP] Installed to: $destination" -ForegroundColor Green
Write-Host "[SRMP] SHA256: $finalHash" -ForegroundColor DarkGray
if ($previousBackup) {
    Write-Host "[SRMP] Previous version backed up to: $previousBackup" -ForegroundColor DarkGray
}
Write-Host '[SRMP] Start Slime Rancher normally through your launcher.' -ForegroundColor Green
exit 0
