@echo off
setlocal
set "SCRIPT=%LOCALAPPDATA%\InventorySearch\Show-Version.ps1"
if not exist "%SCRIPT%" (
    echo [ERROR] Installed Inventory Search version checker not found.
    echo Close Unturned and run UPDATE-NOW.cmd to repair the complete package.
    pause
    exit /b 1
)
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT%"
set "STATUS=%ERRORLEVEL%"
echo.
pause
exit /b %STATUS%
