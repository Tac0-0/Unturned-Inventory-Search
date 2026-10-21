@echo off
setlocal
cd /d "%~dp0"
set "STATE_DIR=%LOCALAPPDATA%\InventorySearch"
set "UPDATER=%STATE_DIR%\Update.ps1"

echo Inventory Search updater
echo ========================
echo.

if not exist "%STATE_DIR%\config.json" (
  echo [ERROR] Inventory Search update configuration is missing.
  echo Run INSTALL.cmd from the extracted installer ZIP first.
  echo The game plugin may still be installed, but it cannot auto-update without configuration.
  set "EXITCODE=1"
  goto :done
)

if not exist "%UPDATER%" (
  echo [INFO] The installed Update.ps1 script is missing.
  if exist "%~dp0Update.ps1" (
    echo [INFO] Restoring Update.ps1 from this installer folder...
    if not exist "%STATE_DIR%" mkdir "%STATE_DIR%"
    copy /Y "%~dp0Update.ps1" "%UPDATER%" >nul
    if errorlevel 1 (
      echo [ERROR] Failed to restore Update.ps1. Try running INSTALL.cmd again.
      set "EXITCODE=1"
      goto :done
    )
    echo [OK] Updater script restored.
  ) else (
    echo [ERROR] No updater script was found in this folder either.
    echo Download the full installer ZIP from GitHub Releases, extract all files,
    echo then run INSTALL.cmd to repair the installation.
    set "EXITCODE=1"
    goto :done
  )
)

powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%UPDATER%"
set "EXITCODE=%ERRORLEVEL%"
if not "%EXITCODE%"=="0" echo [ERROR] Updater exited with code %EXITCODE%.
:done
echo.
pause
exit /b %EXITCODE%
