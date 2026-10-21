@echo off
setlocal
set "UPDATER=%LOCALAPPDATA%\InventorySearch\Update.ps1"

echo Inventory Search Updater
echo ========================
echo.

if not exist "%UPDATER%" (
  echo [ERROR] Installed updater missing at:
  echo         "%UPDATER%"
  echo Close Unturned and run INSTALL.cmd once to repair the installation.
  set "EXITCODE=1"
  goto :done
)

rem The installed updater updates itself from trusted, hash-checked release ZIPs.
rem Never restore older scripts from the original downloaded installer folder.
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%UPDATER%"
set "EXITCODE=%ERRORLEVEL%"
if not "%EXITCODE%"=="0" echo [ERROR] Updater exited with code %EXITCODE%.

:done
echo.
pause
exit /b %EXITCODE%
