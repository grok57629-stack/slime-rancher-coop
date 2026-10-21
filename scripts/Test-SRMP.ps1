[CmdletBinding()]
param(
    [string]$DotNetPath,
    [string]$GamePath = 'C:\Program Files\Epic Games\SlimeRancher'
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
if (-not $DotNetPath) {
    $command = Get-Command dotnet.exe -ErrorAction SilentlyContinue
    if ($command) { $DotNetPath = $command.Source }
    else { $DotNetPath = Join-Path $root 'artifacts\tools\dotnet\dotnet.exe' }
}
if (-not (Test-Path -LiteralPath $DotNetPath -PathType Leaf)) { throw 'A .NET SDK is required. Pass -DotNetPath.' }
if (-not (Test-Path -LiteralPath (Join-Path $root 'Builds\SRMP\SRMP.dll'))) { throw 'Build SRMP before running the assembly tests.' }
$managed = Join-Path $GamePath 'SlimeRancher_Data\Managed'
& $DotNetPath run --project (Join-Path $root 'tests\SRMP.RegressionTests.csproj')
if ($LASTEXITCODE -ne 0) { throw 'Transport/identity/save regression tests failed.' }
& $DotNetPath build (Join-Path $root 'tests\SRMP.AssemblyTests.csproj') "/p:GameManagedPath=$managed" --verbosity quiet
if ($LASTEXITCODE -ne 0) { throw 'Assembly regression harness failed to compile.' }
& (Join-Path $root 'tests\bin\Debug\net472\SRMP.AssemblyTests.exe') $root $managed
if ($LASTEXITCODE -ne 0) { throw 'Assembly/Harmony regression tests failed.' }
& (Join-Path $PSScriptRoot 'Test-Installer.ps1')
