[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$GamePath,

    [Parameter(Mandatory = $false)]
    [switch]$DoNotRestoreBackup
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

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

function Find-SlimeRancher {
    $steamRoots = New-Object 'System.Collections.Generic.List[string]'
    if (${env:ProgramFiles(x86)}) { Add-UniquePath $steamRoots (Join-Path ${env:ProgramFiles(x86)} 'Steam') }
    if ($env:ProgramFiles) { Add-UniquePath $steamRoots (Join-Path $env:ProgramFiles 'Steam') }
    foreach ($key in @('HKCU:\SOFTWARE\Valve\Steam','HKLM:\SOFTWARE\WOW6432Node\Valve\Steam','HKLM:\SOFTWARE\Valve\Steam')) {
        try {
            $value = Get-ItemProperty -Path $key -ErrorAction Stop
            if ($value.PSObject.Properties['SteamPath'] -and $value.SteamPath) { Add-UniquePath $steamRoots $value.SteamPath }
            if ($value.PSObject.Properties['InstallPath'] -and $value.InstallPath) { Add-UniquePath $steamRoots $value.InstallPath }
        } catch { }
    }

    $libraries = New-Object 'System.Collections.Generic.List[string]'
    foreach ($root in $steamRoots) {
        if (-not (Test-Path -LiteralPath $root -PathType Container)) { continue }
        Add-UniquePath $libraries $root
        $vdf = Join-Path $root 'steamapps\libraryfolders.vdf'
        if (Test-Path -LiteralPath $vdf -PathType Leaf) {
            foreach ($line in (Get-Content -LiteralPath $vdf -ErrorAction SilentlyContinue)) {
                $candidate = $null
                if ($line -match '"path"\s+"([^"]+)"') { $candidate = $Matches[1] }
                elseif ($line -match '^\s*"\d+"\s+"([^"]+)"') { $candidate = $Matches[1] }
                if ($candidate) {
                    $candidate = $candidate -replace '\\\\', '\'
                    Add-UniquePath $libraries $candidate
                }
            }
        }
    }

    foreach ($library in $libraries) {
        $candidate = Join-Path $library 'steamapps\common\Slime Rancher'
        if (Test-SlimeRancherPath $candidate) { return $candidate }
    }

    if ($env:ProgramFiles) {
        foreach ($candidate in @((Join-Path $env:ProgramFiles 'Epic Games\SlimeRancher'), (Join-Path $env:ProgramFiles 'Slime Rancher'))) {
            if (Test-SlimeRancherPath $candidate) { return $candidate }
        }
    }
    return $null
}

if ($GamePath) {
    $resolvedGame = Get-FullPathSafe $GamePath
    if (-not (Test-SlimeRancherPath $resolvedGame)) { Fail 'Invalid -GamePath.' 11 }
} else {
    $resolvedGame = Find-NearbySlimeRancher
    if (-not $resolvedGame) { $resolvedGame = Find-SlimeRancher }
    if (-not $resolvedGame) { Fail 'Could not locate Slime Rancher 1. Re-run with -GamePath.' 12 }
}

if (Test-GameProcessRunning $resolvedGame) {
    Fail "Slime Rancher is currently running from '$resolvedGame'. Close that copy before uninstalling." 10
}

$modsPath = Join-Path $resolvedGame 'SRML\Mods'
$destination = Join-Path $modsPath 'SRMP.dll'
$manifestPath = Join-Path $modsPath 'SRMP.install.json'
$backupRoot = Join-Path (Split-Path -Parent $modsPath) 'Backups\SRMP'
$legacyBackupRoot = Join-Path $modsPath 'SRMP-Backups'
$backupToRestore = $null

if (Test-Path -LiteralPath $manifestPath -PathType Leaf) {
    try {
        $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
        if ($manifest.PreviousBackup) { $backupToRestore = [string]$manifest.PreviousBackup }
    } catch {
        Write-Warning 'Install manifest exists but could not be parsed. SRMP.dll can still be removed.'
    }
}

# Migrate backups made by older installers before restoring one. Keeping any
# rollback DLL under Mods would make SRML load it as another copy of SRMP.
if (Test-Path -LiteralPath $legacyBackupRoot -PathType Container) {
    $legacyRoot = [System.IO.Path]::GetFullPath($legacyBackupRoot).TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar
    $modsRoot = [System.IO.Path]::GetFullPath($modsPath).TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar
    if (-not $legacyRoot.StartsWith($modsRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        Fail 'Legacy backup migration path is outside SRML\Mods.' 13
    }

    $legacyDlls = @(Get-ChildItem -LiteralPath $legacyBackupRoot -File -Filter '*.dll')
    if ($legacyDlls.Count -gt 0) {
        New-Item -ItemType Directory -Path $backupRoot -Force | Out-Null
        foreach ($legacyDll in $legacyDlls) {
            $legacyFile = [System.IO.Path]::GetFullPath($legacyDll.FullName)
            if (-not $legacyFile.StartsWith($legacyRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
                Fail "Legacy backup file is outside its expected directory: $legacyFile" 14
            }

            $migratedBackup = Join-Path $backupRoot $legacyDll.Name
            if (Test-Path -LiteralPath $migratedBackup -PathType Leaf) {
                $migratedBackup = Join-Path $backupRoot ("{0}-{1}.dll" -f $legacyDll.BaseName, [Guid]::NewGuid().ToString('N'))
            }
            Move-Item -LiteralPath $legacyFile -Destination $migratedBackup
            if ($backupToRestore -and [string]::Equals([System.IO.Path]::GetFullPath($backupToRestore), $legacyFile, [System.StringComparison]::OrdinalIgnoreCase)) {
                $backupToRestore = $migratedBackup
            }
        }
    }
}

if (Test-Path -LiteralPath $destination -PathType Leaf) {
    Remove-Item -LiteralPath $destination -Force
    Write-Host "[SRMP] Removed: $destination" -ForegroundColor Cyan
} else {
    Write-Host '[SRMP] SRMP.dll was not installed in the SRML Mods folder.' -ForegroundColor Yellow
}

if (-not $DoNotRestoreBackup -and $backupToRestore -and (Test-Path -LiteralPath $backupToRestore -PathType Leaf)) {
    Copy-Item -LiteralPath $backupToRestore -Destination $destination -Force
    Write-Host "[SRMP] Restored previous SRMP.dll from: $backupToRestore" -ForegroundColor Green
}

if (Test-Path -LiteralPath $manifestPath -PathType Leaf) {
    Remove-Item -LiteralPath $manifestPath -Force
}

Write-Host '[SRMP] Uninstall completed.' -ForegroundColor Green
exit 0
