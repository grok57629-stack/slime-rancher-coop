[CmdletBinding()]
param(
    [string]$GamePath = "D:\steam\steamapps\common\Slime Rancher"
)

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "   Slime Rancher 1 - Co-op Mod Automated Installer        " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

if (-not (Test-Path "$GamePath\SlimeRancher.exe")) {
    Write-Host "ERROR: SlimeRancher.exe not found at '$GamePath'!" -ForegroundColor Red
    Write-Host "Please pass your game path, for example: .\Install-Coop.ps1 -GamePath 'C:\Games\Slime Rancher'" -ForegroundColor Yellow
    exit 1
}

# 1. Check if game is running
$proc = Get-Process -Name "SlimeRancher" -ErrorAction SilentlyContinue
if ($proc) {
    Write-Host "ERROR: Slime Rancher is currently running! Please close it before installing." -ForegroundColor Red
    exit 1
}

# 2. Backup original Assembly-CSharp.dll if not backed up yet
$managedDir = Join-Path $GamePath "SlimeRancher_Data\Managed"
$asmPath = Join-Path $managedDir "Assembly-CSharp.dll"
$bakPath = Join-Path $managedDir "Assembly-CSharp.original.dll"

if (-not (Test-Path $bakPath)) {
    Write-Host "[1/3] Creating original backup: Assembly-CSharp.original.dll..." -ForegroundColor Green
    Copy-Item $asmPath $bakPath -Force
} else {
    Write-Host "[1/3] Original backup already exists: $bakPath" -ForegroundColor DarkGray
}

# 3. Patch SRML (Mod Loader)
Write-Host "[2/3] Installing SRML (Slime Rancher Mod Loader)..." -ForegroundColor Green
$installer = Join-Path $PSScriptRoot "SRMLInstaller.exe"
& $installer -v $GamePath

if (-not (Test-Path "$GamePath\SRML\Mods")) {
    Write-Host "Creating SRML\Mods folder..." -ForegroundColor DarkGray
    New-Item -ItemType Directory -Path "$GamePath\SRML\Mods" -Force | Out-Null
}

# 4. Copy SRMP.dll into SRML/Mods
Write-Host "[3/3] Installing SRMP Co-op Mod..." -ForegroundColor Green
$sourceDll = Join-Path $PSScriptRoot "SRMP.dll"
$targetDll = Join-Path $GamePath "SRML\Mods\SRMP.dll"
Copy-Item $sourceDll $targetDll -Force

Write-Host ""
Write-Host "==========================================================" -ForegroundColor Green
Write-Host " SUCCESS! Co-op mod has been installed into Slime Rancher! " -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Green
Write-Host "Game Location: $GamePath"
Write-Host "Mod Location:  $targetDll"
Write-Host ""
Write-Host "How to Play:" -ForegroundColor Yellow
Write-Host "1. Launch Slime Rancher as usual (via Steam)." -ForegroundColor White
Write-Host "2. Once in game, press [F4] to open the Multiplayer Menu." -ForegroundColor White
Write-Host "3. To Host: click 'Host Game', share your IP or the generated Server Code with your friend." -ForegroundColor White
Write-Host "4. To Join: friend enters your IP (or Server Code) and clicks 'Connect'." -ForegroundColor White
Write-Host "5. In-game Chat: press Enter / Return to open chat." -ForegroundColor White
Write-Host "==========================================================" -ForegroundColor Green
