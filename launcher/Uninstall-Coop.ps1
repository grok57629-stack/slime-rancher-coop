[CmdletBinding()]
param(
    [string]$GamePath = "D:\steam\steamapps\common\Slime Rancher"
)

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "   Slime Rancher 1 - Co-op Mod Uninstaller                " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$managedDir = Join-Path $GamePath "SlimeRancher_Data\Managed"
$asmPath = Join-Path $managedDir "Assembly-CSharp.dll"
$bakPath = Join-Path $managedDir "Assembly-CSharp.original.dll"

# 1. Restore Assembly-CSharp.dll
if (Test-Path $bakPath) {
    Write-Host "Restoring original Assembly-CSharp.dll..." -ForegroundColor Green
    Copy-Item $bakPath $asmPath -Force
    Remove-Item $bakPath -Force
}

# 2. Remove SRMP mod dll
$srmpDll = Join-Path $GamePath "SRML\Mods\SRMP.dll"
if (Test-Path $srmpDll) {
    Write-Host "Removing SRMP.dll..." -ForegroundColor Green
    Remove-Item $srmpDll -Force
}

Write-Host "Uninstallation complete. Slime Rancher restored to vanilla." -ForegroundColor Green
