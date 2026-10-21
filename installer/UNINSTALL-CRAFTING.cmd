@echo off
setlocal
set "MANAGER=%LOCALAPPDATA%\InventorySearch\Manage-CraftingAddon.ps1"
if not exist "%MANAGER%" (
    echo [ERROR] SmartCraft add-on manager is missing.
    echo Remove only SmartCraftCraftingAddon.dll from your InventorySearchPrototype plugin folder, or install the current manager.
    set "EXITCODE=1"
    goto :done
)
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%MANAGER%" -Action Uninstall
set "EXITCODE=%ERRORLEVEL%"
if not "%EXITCODE%"=="0" echo [ERROR] Add-on uninstall failed with code %EXITCODE%.
:done
echo.
pause
exit /b %EXITCODE%
