@echo off
setlocal
set "MANAGER=%LOCALAPPDATA%\InventorySearch\Manage-CraftingAddon.ps1"
if not exist "%MANAGER%" (
    echo [ERROR] SmartCraft add-on manager is missing.
    echo Install a current Inventory Search release first.
    set "EXITCODE=1"
    goto :done
)
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%MANAGER%" -Action Prompt
set "EXITCODE=%ERRORLEVEL%"
if not "%EXITCODE%"=="0" echo [ERROR] Add-on manager failed with code %EXITCODE%.
:done
echo.
pause
exit /b %EXITCODE%
