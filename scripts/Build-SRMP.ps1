[CmdletBinding()]
param(
    [string]$GamePath,
    [string]$DotNetPath,
    [ValidateSet('SRML', 'SRML NoVer', 'Debug', 'Release', 'Standalone', 'Standalone NoVer')]
    [string]$Configuration = 'SRML',
    [switch]$Package,
    [switch]$Install,
    [string]$Version = 'dev'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectDir = Join-Path $repoRoot 'SRMP'
$projectPath = Join-Path $projectDir 'SRMP.csproj'
$solutionPath = Join-Path $repoRoot 'SRMP.sln'
$outputDll = Join-Path $repoRoot 'Builds\SRMP\SRMP.dll'

function Test-GamePath {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) { return $false }

    try {
        if (-not (Test-Path -LiteralPath $Path -PathType Container -ErrorAction SilentlyContinue)) {
            return $false
        }

        $exe = [IO.Path]::Combine($Path, 'SlimeRancher.exe')
        $managed = [IO.Path]::Combine($Path, 'SlimeRancher_Data', 'Managed')

        return (Test-Path -LiteralPath $exe -PathType Leaf -ErrorAction SilentlyContinue) -and
               (Test-Path -LiteralPath $managed -PathType Container -ErrorAction SilentlyContinue)
    }
    catch {
        return $false
    }
}

function Get-Sha256Hex {
    param([Parameter(Mandatory = $true)][string]$LiteralPath)

    $stream = [IO.File]::OpenRead($LiteralPath)
    $algorithm = [Security.Cryptography.SHA256]::Create()
    try {
        return [BitConverter]::ToString($algorithm.ComputeHash($stream)).Replace('-', '')
    }
    finally {
        $algorithm.Dispose()
        $stream.Dispose()
    }
}

function Get-SteamLibraryPaths {
    $paths = New-Object System.Collections.Generic.List[string]
    $steamRoots = New-Object System.Collections.Generic.List[string]

    foreach ($key in @(
        'HKCU:\Software\Valve\Steam',
        'HKLM:\SOFTWARE\WOW6432Node\Valve\Steam',
        'HKLM:\SOFTWARE\Valve\Steam'
    )) {
        try {
            $props = Get-ItemProperty -LiteralPath $key -ErrorAction Stop
            $value = $props.SteamPath
            if (-not $value) { $value = $props.InstallPath }
            if ($value) { $steamRoots.Add([IO.Path]::GetFullPath($value)) }
        } catch { }
    }

    if (${env:ProgramFiles(x86)}) {
        $steamRoots.Add([IO.Path]::Combine(${env:ProgramFiles(x86)}, 'Steam'))
    }

    foreach ($root in ($steamRoots | Select-Object -Unique)) {
        if ([string]::IsNullOrWhiteSpace($root)) { continue }
        if (-not (Test-Path -LiteralPath $root -PathType Container -ErrorAction SilentlyContinue)) { continue }

        $paths.Add($root)
        $vdf = [IO.Path]::Combine($root, 'steamapps', 'libraryfolders.vdf')
        if (Test-Path -LiteralPath $vdf -PathType Leaf -ErrorAction SilentlyContinue) {
            $raw = Get-Content -LiteralPath $vdf -Raw
            foreach ($match in [regex]::Matches($raw, '"path"\s+"([^"]+)"')) {
                $library = $match.Groups[1].Value -replace '\\\\', '\'
                if ([string]::IsNullOrWhiteSpace($library)) { continue }

                if (Test-Path -LiteralPath $library -PathType Container -ErrorAction SilentlyContinue) {
                    $paths.Add($library)
                }
                else {
                    Write-Host "Skipping unavailable Steam library: $library" -ForegroundColor DarkYellow
                }
            }
        }
    }

    return $paths | Select-Object -Unique
}

function Find-GamePath {
    if ($GamePath) {
        $candidate = [IO.Path]::GetFullPath($GamePath)
        if (-not (Test-GamePath $candidate)) {
            throw "The supplied -GamePath is not a Slime Rancher 1 installation: $candidate"
        }
        return $candidate
    }

    foreach ($library in Get-SteamLibraryPaths) {
        if ([string]::IsNullOrWhiteSpace($library)) { continue }
        if (-not (Test-Path -LiteralPath $library -PathType Container -ErrorAction SilentlyContinue)) {
            Write-Host "Skipping unavailable Steam library: $library" -ForegroundColor DarkYellow
            continue
        }

        $candidate = [IO.Path]::Combine($library, 'steamapps', 'common', 'Slime Rancher')
        if (Test-GamePath $candidate) { return $candidate }
    }

    $fallbacks = New-Object System.Collections.Generic.List[string]
    if (${env:ProgramFiles}) {
        $fallbacks.Add([IO.Path]::Combine(${env:ProgramFiles}, 'Epic Games', 'SlimeRancher'))
        $fallbacks.Add([IO.Path]::Combine(${env:ProgramFiles}, 'Slime Rancher'))
    }
    if (${env:ProgramFiles(x86)}) {
        $fallbacks.Add([IO.Path]::Combine(${env:ProgramFiles(x86)}, 'Steam', 'steamapps', 'common', 'Slime Rancher'))
    }

    foreach ($candidate in $fallbacks) {
        if (Test-GamePath $candidate) { return $candidate }
    }

    throw 'Could not find Slime Rancher 1. Pass -GamePath "C:\path\to\Slime Rancher".'
}

function Find-BuildTool {
    $command = Get-Command msbuild.exe -ErrorAction SilentlyContinue
    if ($command) { return @{ Exe = $command.Source; Args = @() } }
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (Test-Path -LiteralPath $vswhere -PathType Leaf) {
        $found = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
        if ($found) { return @{ Exe = $found; Args = @() } }
    }
    $dotnet = $DotNetPath
    if (-not $dotnet) {
        $command = Get-Command dotnet.exe -ErrorAction SilentlyContinue
        if ($command) { $dotnet = $command.Source }
    }
    if (-not $dotnet) { $dotnet = Join-Path $repoRoot 'artifacts\tools\dotnet\dotnet.exe' }
    if (Test-Path -LiteralPath $dotnet -PathType Leaf) {
        $env:DOTNET_ROOT = Split-Path -Parent ([IO.Path]::GetFullPath($dotnet))
        return @{ Exe = $dotnet; Args = @('msbuild') }
    }
    throw 'Install Visual Studio Build Tools or a .NET SDK, or pass -DotNetPath to dotnet.exe.'
}

$resolvedGame = Find-GamePath
$managedDir = Join-Path $resolvedGame 'SlimeRancher_Data\Managed'
$srmlLibDir = Join-Path $resolvedGame 'SRML\Libs'
$modsDir = Join-Path $resolvedGame 'SRML\Mods'
$buildTool = Find-BuildTool
Write-Host "Using Slime Rancher: $resolvedGame"
Write-Host "Using build tool: $($buildTool.Exe)"

# References are resolved in place. Building never edits the project or copies
# game assemblies into the source tree, and does not require a modded game.
$buildArgs = @($buildTool.Args) + @(
    $solutionPath, '-restore', '/m', "/p:Configuration=$Configuration", '/p:Platform=Any CPU',
    "/p:GameManagedPath=$managedDir", "/p:SRMLLibPath=$srmlLibDir", '/verbosity:minimal'
)
& $buildTool.Exe @buildArgs
if ($LASTEXITCODE -ne 0) { throw "MSBuild failed with exit code $LASTEXITCODE" }
if ($Configuration -eq 'Release') { $outputDll = Join-Path $projectDir 'bin\Release\SRMP.dll' }
if (-not (Test-Path -LiteralPath $outputDll -PathType Leaf)) { throw "Missing build output: $outputDll" }
$hash = Get-Sha256Hex -LiteralPath $outputDll
Write-Host "SRMP.dll built successfully: $outputDll"
Write-Host "SHA256: $hash"

# Package before installation so a missing SRML installation does not hide a
# successful compilation or prevent producing the requested release artifact.
if ($Package) {
    & (Join-Path $PSScriptRoot 'Package-Release.ps1') -SrmpDll $outputDll -Version $Version -OutputDirectory (Join-Path $repoRoot 'dist')
}
if ($Install) {
    if ($Configuration -notlike 'SRML*') { throw 'Only an SRML build can be installed into SRML\Mods.' }
    if (-not (Test-Path -LiteralPath $modsDir -PathType Container)) {
        throw "Compilation succeeded, but installation requires SRML. Missing: $modsDir. Install SRML and rerun Build SRMP.cmd. DLL: $outputDll"
    }
    & (Join-Path $repoRoot 'installer\Install-SRMP.ps1') -GamePath $resolvedGame -SourceDll $outputDll
    $installedDll = Join-Path $modsDir 'SRMP.dll'
    if (-not (Test-Path -LiteralPath $installedDll -PathType Leaf) -or
        (Get-Sha256Hex -LiteralPath $installedDll) -ne $hash) {
        throw "Installed SRMP.dll is missing or differs from the build: $installedDll"
    }
    Write-Host "Installed successfully: $installedDll"
}
