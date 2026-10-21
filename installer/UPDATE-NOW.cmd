@echo off
setlocal
cd /d "%~dp0"
set "STATE_DIR=%LOCALAPPDATA%\InventorySearch"
set "UPDATER=%STATE_DIR%\Update.ps1"
set "SHIPPED_UPDATER=%~dp0Update.ps1"

echo Inventory Search Updater
echo ========================
echo.

if not exist "%STATE_DIR%\config.json" (
  echo [ERROR] Inventory Search updater configuration is missing.
  echo Close Unturned and run INSTALL.cmd from this extracted release ZIP first.
  echo If v0.1.6 is already installed, this repairs updater configuration.
  set "EXITCODE=1"
  goto :done
)

rem Replacing the installed updater engine from a LOCAL, user-extracted release
rem lets older v0.1.6 installs fix script bugs BEFORE updating the plugin DLL.
rem No remote scripts are downloaded or run.
if exist "%SHIPPED_UPDATER%" (
  echo [INFO] Refreshing installed updater script from this release package...
  copy /Y "%SHIPPED_UPDATER%" "%UPDATER%" >nul
  if errorlevel 1 (
    echo [ERROR] Cannot replace installed Update.ps1.
    echo Close Unturned and other updater windows, then try again.
    set "EXITCODE=1"
    goto :done
  )
  echo [OK] Updater script ready.
) else (
  echo [WARNING] The extracted installer folder is missing Update.ps1.
  echo Re-download and extract the official release ZIP if the updater fails.
)

if not exist "%UPDATER%" (
  echo [ERROR] Update.ps1 is not installed or available.
  echo Re-run INSTALL.cmd from a fully extracted release ZIP.
  set "EXITCODE=1"
  goto :done
)

powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%UPDATER%"
set "EXITCODE=%ERRORLEVEL%"
if not "%EXITCODE%"=="0" echo [ERROR] Updater exited with code %EXITCODE%.
:done
echo.
pause
exit /b %EXITCODE%
