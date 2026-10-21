@echo off
setlocal
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0Run-Server.ps1" %*
exit /b %ERRORLEVEL%
