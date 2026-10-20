@echo off
setlocal
cd /d "%~dp0"
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install.ps1"
set EXITCODE=%ERRORLEVEL%
if not "%EXITCODE%"=="0" echo Installer exited with code %EXITCODE%.
echo.
pause
exit /b %EXITCODE%