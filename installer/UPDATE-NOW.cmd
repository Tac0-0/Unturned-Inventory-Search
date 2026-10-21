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

rem Manual update checks can also lead to the optional addon manager.
rem Unattended scheduled updates run Update.ps1 directly and NEVER prompt.
if "%EXITCODE%"=="0" (
  rem The installed v1.2 manager asks once; a saved choice is never overwritten.
  if exist "%LOCALAPPDATA%\InventorySearch\Manage-CraftingAddon.ps1" (
    powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%LOCALAPPDATA%\InventorySearch\Manage-CraftingAddon.ps1" -Action PromptOptIn
  )
  echo.
  echo Optional SmartCraft crafting add-on management:
  echo   "%LOCALAPPDATA%\InventorySearch\CRAFTING-ADDON.cmd"
  echo To remove ONLY the add-on:
  echo   "%LOCALAPPDATA%\InventorySearch\UNINSTALL-CRAFTING.cmd"
)

:done
echo.
pause
exit /b %EXITCODE%
