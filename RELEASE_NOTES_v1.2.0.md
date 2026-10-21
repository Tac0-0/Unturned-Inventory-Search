# Inventory Search v1.2.0

**Scope: optional SmartCraft choice and add-on lifecycle, not the crafting engine.**

- Fresh installs ask whether the player wants to opt in to future SmartCraft crafting compatibility, with **No** as the default.
- `UPDATE-NOW.cmd` on v1.2 can show the one-time opt-in question when launched manually, without making unattended update tasks pop up.
- A saved preference is preserved across updates. Choosing Yes **does not install a crafting DLL in v1.2**.
- Adds `CRAFTING-ADDON.cmd` for managing the opt-in and `UNINSTALL-CRAFTING.cmd` to opt out/remove only the optional SmartCraft module.
- Prevents legacy updaters from auto-installing optional files by isolating them in `optionalFiles`.
- Retains existing Inventory Search features and full-package self-update/rollback behavior.

**SmartCraft crafting is not implemented in v1.2.0.** Crafting engine development is reserved for v1.3.0 and later, requiring separate client/server work and testing.

For friends using v1.1.0: update to v1.2.0, then run `%LOCALAPPDATA%\InventorySearch\CRAFTING-ADDON.cmd` to see and manage the question if your first update check did not display it. The new one-time question is also shown on a fresh v1.2.0 `INSTALL.cmd` run. Launch Unturned without BattlEye.
