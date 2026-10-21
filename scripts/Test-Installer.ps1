[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$artifactsRoot = [IO.Path]::GetFullPath((Join-Path $root 'artifacts'))
$testRoot = Join-Path $artifactsRoot ('installer-test-' + [Guid]::NewGuid().ToString('N'))
$installer = Join-Path $PSScriptRoot '..\installer\Install-SRMP.ps1'
$uninstaller = Join-Path $PSScriptRoot '..\installer\Uninstall-SRMP.ps1'
$source = Join-Path $testRoot 'new-SRMP.dll'
$portableTestRoot = $null
$checks = 0

function Assert([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
    $script:checks++
    Write-Host "PASS $Message"
}

foreach ($scriptPath in @($installer, $uninstaller)) {
    $tokens = $null
    $parseErrors = $null
    [System.Management.Automation.Language.Parser]::ParseFile($scriptPath, [ref]$tokens, [ref]$parseErrors) | Out-Null
    Assert ($parseErrors.Count -eq 0) "PowerShell syntax: $(Split-Path -Leaf $scriptPath)"
}

try {
    $game = Join-Path $testRoot 'fake-game'
    $managed = Join-Path $game 'SlimeRancher_Data\Managed'
    $mods = Join-Path $game 'SRML\Mods'
    $legacyDir = Join-Path $mods 'SRMP-Backups'
    New-Item -ItemType Directory -Path $managed -Force | Out-Null
    New-Item -ItemType Directory -Path $legacyDir -Force | Out-Null
    New-Item -ItemType File -Path (Join-Path $game 'SlimeRancher.exe') | Out-Null

    Set-Content -LiteralPath $source -Value 'new SRMP build' -Encoding ASCII
    $installed = Join-Path $mods 'SRMP.dll'
    $legacyBackup = Join-Path $legacyDir 'SRMP-old-rollback.dll'
    Set-Content -LiteralPath $installed -Value 'previous installed SRMP' -Encoding ASCII
    Set-Content -LiteralPath $legacyBackup -Value 'older rollback SRMP' -Encoding ASCII
    $manifestPath = Join-Path $mods 'SRMP.install.json'
    @{ PreviousBackup = $legacyBackup } | ConvertTo-Json | Set-Content -LiteralPath $manifestPath -Encoding UTF8

    & $installer -GamePath $game -SourceDll $source
    Assert ((Get-Content -LiteralPath $installed -Raw).Trim() -eq 'new SRMP build') 'installer atomically replaces the mod'
    $modDlls = @(Get-ChildItem -LiteralPath $mods -Recurse -File -Filter '*.dll')
    Assert ($modDlls.Count -eq 1 -and $modDlls[0].FullName -eq $installed) 'rollback DLLs are not visible to SRML under Mods'

    $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    $manifestBackupValid = (Test-Path -LiteralPath $manifest.PreviousBackup -PathType Leaf) -and
        $manifest.PreviousBackup.StartsWith((Join-Path $game 'SRML\Backups\SRMP'), [StringComparison]::OrdinalIgnoreCase) -and
        (Get-Content -LiteralPath $manifest.PreviousBackup -Raw).Trim() -eq 'previous installed SRMP'
    Assert $manifestBackupValid 'install manifest points outside Mods to the previous version'
    Assert ((Get-ChildItem -LiteralPath (Join-Path $game 'SRML\Backups\SRMP') -File -Filter '*.dll').Count -eq 2) 'legacy rollback DLL is migrated outside Mods'

    & $uninstaller -GamePath $game
    $previousVersionRestored = (Test-Path -LiteralPath $installed -PathType Leaf) -and
        (Get-Content -LiteralPath $installed -Raw).Trim() -eq 'previous installed SRMP'
    Assert $previousVersionRestored 'uninstall restores the previous version from the external backup'
    $modDllsAfterUninstall = @(Get-ChildItem -LiteralPath $mods -Recurse -File -Filter '*.dll')
    Assert ($modDllsAfterUninstall.Count -eq 1 -and $modDllsAfterUninstall[0].FullName -eq $installed) 'uninstall leaves no duplicate DLL under Mods'

    $portableTestRoot = Join-Path $artifactsRoot ('portable-neighbor-test-' + [Guid]::NewGuid().ToString('N'))
    $portableRoot = $portableTestRoot
    $portableGame = Join-Path $portableRoot 'Slime Rancher Portable'
    $portableRelease = Join-Path $portableRoot 'SRMP-release'
    $portableInstallerDir = Join-Path $portableRelease 'installer'
    $portablePayloadDir = Join-Path $portableRelease 'payload'
    $portableMods = Join-Path $portableGame 'SRML\Mods'
    New-Item -ItemType Directory -Path (Join-Path $portableGame 'SlimeRancher_Data\Managed') -Force | Out-Null
    New-Item -ItemType Directory -Path $portableMods -Force | Out-Null
    New-Item -ItemType Directory -Path $portableInstallerDir -Force | Out-Null
    New-Item -ItemType Directory -Path $portablePayloadDir -Force | Out-Null
    New-Item -ItemType File -Path (Join-Path $portableGame 'SlimeRancher.exe') | Out-Null
    Copy-Item -LiteralPath $installer -Destination (Join-Path $portableInstallerDir 'Install-SRMP.ps1')
    Copy-Item -LiteralPath $uninstaller -Destination (Join-Path $portableInstallerDir 'Uninstall-SRMP.ps1')
    Copy-Item -LiteralPath $source -Destination (Join-Path $portablePayloadDir 'SRMP.dll')

    Push-Location -LiteralPath $portableInstallerDir
    try { & (Join-Path $portableInstallerDir 'Install-SRMP.ps1') }
    finally { Pop-Location }
    $portableInstalled = Join-Path $portableMods 'SRMP.dll'
    $portableManifest = Get-Content -LiteralPath (Join-Path $portableMods 'SRMP.install.json') -Raw | ConvertFrom-Json
    Assert ((Test-Path -LiteralPath $portableInstalled -PathType Leaf) -and $portableManifest.GamePath -eq $portableGame) 'installer detects a nearby portable game without a fixed path'

    Push-Location -LiteralPath $portableInstallerDir
    try { & (Join-Path $portableInstallerDir 'Uninstall-SRMP.ps1') }
    finally { Pop-Location }
    Assert (-not (Test-Path -LiteralPath $portableInstalled)) 'uninstaller detects the same nearby portable game'

    Write-Host "$checks installer checks passed."
}
finally {
    if ($portableTestRoot) {
        $resolvedPortableRoot = [IO.Path]::GetFullPath($portableTestRoot)
        $artifactsPrefix = $artifactsRoot.TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
        if ($resolvedPortableRoot.StartsWith($artifactsPrefix, [StringComparison]::OrdinalIgnoreCase)) {
            if (Test-Path -LiteralPath $resolvedPortableRoot) {
                Remove-Item -LiteralPath $resolvedPortableRoot -Recurse -Force
            }
        } else {
            throw "Refusing to clean a portable test directory outside artifacts: $resolvedPortableRoot"
        }
    }

    $resolvedTestRoot = [IO.Path]::GetFullPath($testRoot)
    $artifactsPrefix = $artifactsRoot.TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
    if ($resolvedTestRoot.StartsWith($artifactsPrefix, [StringComparison]::OrdinalIgnoreCase)) {
        if (Test-Path -LiteralPath $resolvedTestRoot) { Remove-Item -LiteralPath $resolvedTestRoot -Recurse -Force }
    } else {
        throw "Refusing to clean a test directory outside artifacts: $resolvedTestRoot"
    }
}
