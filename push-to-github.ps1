param(
    [string]$RepoUrl = "https://github.com/grok57629-stack/slime-rancher-coop.git"
)

$env:PATH = "C:\ETS2LA\system\git\cmd;$env:PATH"
$repoRoot = $PSScriptRoot

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "   Slime Rancher Co-op - Push to GitHub                    " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "Target Remote: $RepoUrl" -ForegroundColor White
Write-Host ""

Set-Location $repoRoot

# Ensure git remote origin is set
$existingRemote = git remote get-url origin 2>$null
if (-not $existingRemote) {
    Write-Host "Adding remote origin..." -ForegroundColor DarkGray
    git remote add origin $RepoUrl
} else {
    Write-Host "Updating remote origin to: $RepoUrl" -ForegroundColor DarkGray
    git remote set-url origin $RepoUrl
}

Write-Host "Pushing main branch to GitHub..." -ForegroundColor Green
git push -u origin main

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "==========================================================" -ForegroundColor Green
    Write-Host " SUCCESS! Project has been pushed to GitHub!               " -ForegroundColor Green
    Write-Host " Repository: https://github.com/grok57629-stack/slime-rancher-coop" -ForegroundColor White
    Write-Host "==========================================================" -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "Note: If the repository does not exist yet on GitHub, please create it at:" -ForegroundColor Yellow
    Write-Host "https://github.com/new (Name: slime-rancher-coop, Public, without README)" -ForegroundColor Yellow
    Write-Host "Then run this script again!" -ForegroundColor Yellow
}
