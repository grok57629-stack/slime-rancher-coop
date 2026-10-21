[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string]$GamePath,
    [switch]$CheckOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSHOME 'Modules\Microsoft.PowerShell.Utility\Microsoft.PowerShell.Utility.psd1') -ErrorAction Stop

$serverDir = $PSScriptRoot
$repoRoot = Split-Path -Parent $serverDir

function Test-SlimeRancherInstall {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) { return $false }
    return (Test-Path -LiteralPath (Join-Path $Path 'SlimeRancher.exe') -PathType Leaf) -and
           (Test-Path -LiteralPath (Join-Path $Path 'SlimeRancher_Data\Managed') -PathType Container)
}

function Add-GameCandidate {
    param(
        [System.Collections.Generic.List[string]]$Candidates,
        [string]$Path
    )

    if ([string]::IsNullOrWhiteSpace($Path)) { return }
    try { $fullPath = [IO.Path]::GetFullPath($Path) } catch { return }
    if (-not (Test-SlimeRancherInstall $fullPath)) { return }

    foreach ($candidate in $Candidates) {
        if ([string]::Equals($candidate, $fullPath, [StringComparison]::OrdinalIgnoreCase)) { return }
    }
    $Candidates.Add($fullPath)
}

function Find-NearbyGameInstalls {
    $candidates = New-Object 'System.Collections.Generic.List[string]'
    $probe = Get-Item -LiteralPath $serverDir
    $depth = 0
    while ($probe -and $depth -le 4) {
        Add-GameCandidate $candidates $probe.FullName
        try {
            foreach ($child in (Get-ChildItem -LiteralPath $probe.FullName -Directory -Force -ErrorAction Stop)) {
                Add-GameCandidate $candidates $child.FullName
            }
        } catch { }

        $probe = $probe.Parent
        $depth++
    }

    $workingDirectory = (Get-Location).Path
    Add-GameCandidate $candidates $workingDirectory
    try {
        foreach ($child in (Get-ChildItem -LiteralPath $workingDirectory -Directory -Force -ErrorAction Stop)) {
            Add-GameCandidate $candidates $child.FullName
        }
    } catch { }

    return $candidates
}

$requestedGamePath = $GamePath
if ([string]::IsNullOrWhiteSpace($requestedGamePath)) { $requestedGamePath = $env:SRMP_GAME_DIR }
if (-not [string]::IsNullOrWhiteSpace($requestedGamePath)) {
    if (-not (Test-SlimeRancherInstall $requestedGamePath)) {
        throw "The supplied game path is not a Slime Rancher 1 installation: $requestedGamePath"
    }
    $GamePath = $requestedGamePath
} else {
    $nearbyCandidates = @(Find-NearbyGameInstalls)
    if ($nearbyCandidates.Count -eq 1) {
        $GamePath = $nearbyCandidates[0]
        Write-Host "Found portable Slime Rancher beside the server package: $GamePath"
    } elseif ($nearbyCandidates.Count -gt 1) {
        $withSrml = @($nearbyCandidates | Where-Object {
            Test-Path -LiteralPath (Join-Path $_ 'SRML\Mods') -PathType Container
        })
        if ($withSrml.Count -eq 1) {
            $GamePath = $withSrml[0]
            Write-Host "Found portable Slime Rancher with SRML beside the server package: $GamePath"
        } else {
            throw "More than one nearby Slime Rancher install was found. Run run.cmd with the game folder as its argument: $($nearbyCandidates -join '; ')"
        }
    } else {
        $standardCandidates = @()
        if (${env:ProgramFiles}) {
            $standardCandidates += (Join-Path ${env:ProgramFiles} 'Epic Games\SlimeRancher')
            $standardCandidates += (Join-Path ${env:ProgramFiles} 'Steam\steamapps\common\Slime Rancher')
        }
        if (${env:ProgramFiles(x86)}) {
            $standardCandidates += (Join-Path ${env:ProgramFiles(x86)} 'Steam\steamapps\common\Slime Rancher')
        }
        $standardCandidates = @($standardCandidates | Where-Object { Test-SlimeRancherInstall $_ } | Select-Object -Unique)
        if ($standardCandidates.Count -eq 1) {
            $GamePath = $standardCandidates[0]
        } elseif ($standardCandidates.Count -gt 1) {
            throw "More than one standard Slime Rancher install was found. Run run.cmd with the game folder as its argument: $($standardCandidates -join '; ')"
        }
    }
}
if (-not (Test-SlimeRancherInstall $GamePath)) {
    throw 'Slime Rancher 1 was not found near this server package. Put the server package beside the game, run run.cmd from the game folder, pass the game folder as an argument, or set SRMP_GAME_DIR.'
}

$GamePath = [IO.Path]::GetFullPath($GamePath)
$gameExe = Join-Path $GamePath 'SlimeRancher.exe'
$modsDir = Join-Path $GamePath 'SRML\Mods'
if (-not (Test-Path -LiteralPath $modsDir -PathType Container)) {
    throw "SRML is missing. Install SRML first so this folder exists: $modsDir"
}

$dllCandidates = @(
    (Join-Path $serverDir '..\payload\SRMP.dll'),
    (Join-Path $serverDir '..\Builds\SRMP\SRMP.dll')
)
$sourceDll = $dllCandidates | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1
if (-not $sourceDll) {
    throw 'SRMP.dll is missing. Extract the SRMP release package or build the project first.'
}

$installedDll = Join-Path $modsDir 'SRMP.dll'
$sourceHash = (Get-FileHash -LiteralPath $sourceDll -Algorithm SHA256).Hash
$installedHash = if (Test-Path -LiteralPath $installedDll -PathType Leaf) {
    (Get-FileHash -LiteralPath $installedDll -Algorithm SHA256).Hash
} else { '' }
if ($CheckOnly) {
    if ($sourceHash -ne $installedHash) {
        throw "SRMP.dll is not installed correctly in $modsDir. Run run.cmd once to install it."
    }
    Write-Host "Server setup OK. Game: $GamePath"
    Write-Host "SRML mods: $modsDir"
    Write-Host "SRMP.dll SHA256: $installedHash"
    exit 0
}

if ($sourceHash -ne $installedHash) {
    Copy-Item -LiteralPath $sourceDll -Destination $installedDll -Force
    Write-Host "Installed SRMP.dll in $modsDir"
}

$dataDir = Join-Path $GamePath 'SRMP'
$logsDir = Join-Path $dataDir 'Logs'
New-Item -ItemType Directory -Path $logsDir -Force | Out-Null
$configPath = Join-Path $dataDir 'autohost.json'
if (-not (Test-Path -LiteralPath $configPath -PathType Leaf)) {
    Copy-Item -LiteralPath (Join-Path $serverDir 'autohost.json') -Destination $configPath
    Write-Host "Created first-run server settings: $configPath"
}

$stoppedMarker = Join-Path $dataDir 'server.stopped'
if (Test-Path -LiteralPath $stoppedMarker) { Remove-Item -LiteralPath $stoppedMarker -Force }
$shutdownRequest = Join-Path $dataDir 'shutdown.request'
if (Test-Path -LiteralPath $shutdownRequest) { Remove-Item -LiteralPath $shutdownRequest -Force }

Write-Host "Starting the headless SRMP host from $GamePath"
Write-Host 'The wrapper restarts the game after a crash or failed host startup.'
Write-Host "For a clean stop, create: $(Join-Path $dataDir 'shutdown.request')"

while (-not (Test-Path -LiteralPath $stoppedMarker -PathType Leaf)) {
    $logPath = Join-Path $logsDir ("Unity-{0:yyyyMMdd-HHmmss}.log" -f [DateTime]::UtcNow)
    Write-Host "Launching Slime Rancher from its own folder; log: $logPath"

    $gameArguments = '-batchmode -nographics -srmp-autohost -logFile "' + $logPath + '"'
    $gameProcess = Start-Process -FilePath $gameExe -ArgumentList $gameArguments `
        -WorkingDirectory $GamePath -PassThru -Wait
    $exitCode = $gameProcess.ExitCode
    $gameProcess.Dispose()

    if (Test-Path -LiteralPath $stoppedMarker -PathType Leaf) { break }

    Write-Warning "Slime Rancher exited with code $exitCode. Restarting in 10 seconds."
    Start-Sleep -Seconds 10
}

Write-Host 'SRMP server stopped cleanly.'
