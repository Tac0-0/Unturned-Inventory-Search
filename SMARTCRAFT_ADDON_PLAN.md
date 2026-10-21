# Optional SmartCraft Crafting Add-on (development branch)

**Status:** Installer/updater/uninstaller scaffolding only. The actual client-side
crafting compatibility DLL and matching server-side crafting logic have **not**
been completed, published, or validated in-game. Do not label this a functional
SmartCraft release until that testing is complete.

## User-required behavior

- Keep existing More Farming stacks: loose Metal Scrap (67),
  10 Metal Scraps (49101), and 100 Metal Scraps (49102).
- Preserve their real item appearance and bundle IDs. No ammo-style replacement.
- Preserve original Unturned Y-menu crafting recipes. No per-recipe duplicates.
- Count all equivalent materials together, consume the exact recipe cost,
  and return remainder as ordinary 100x/10x bundles and loose items.
- Allow players on the private server to opt into the client-side Harmony
  crafting patch **once**, alongside Inventory Search. Do not force it on
  Inventory Search users who are not in the private server.
- Keep SmartCraftTest off between development and a new Workshop upload. Do not
  change the production world until the client/server implementation passes tests.

## Add-on management behavior (implemented in this branch)

- `INSTALL.cmd` runs `Install.ps1`, which asks once with **No as default**.
- `CRAFTING-ADDON.cmd` lets the player install, uninstall, or view status later.
- `UNINSTALL-CRAFTING.cmd` removes only the owned
  `SmartCraftCraftingAddon.dll` and persists opt-out.
- In the Start menu the player has separate **Manage SmartCraft Add-on**
  and **Uninstall SmartCraft Add-on** shortcuts.
- Preference file: `%LOCALAPPDATA%\InventorySearch\crafting-addon.json`.
  This file is outside the release's managed manifest so updates cannot silently
  reset the user's choice.
- `Update.ps1` treats the add-on as an OPTIONAL manifest entry whose
  `optionalFeature` must equal `smartcraft-crafting` and whose target is exactly
  `plugin/SmartCraftCraftingAddon.dll`. It verifies the release ZIP and the
  file's SHA-256 before copying anything.
- Unattended updates never prompt. They install/update the add-on only if
  the saved preference is enabled. Removal persists across later updates.
- `Manage-CraftingAddon.ps1 -Action Install` uses the existing authenticated
  full-package updater with `-ForceAddonRepair`; if the release does not yet
  include the optional DLL, opt-in is rolled back and the base mod stays usable.
- Full Inventory Search uninstall removes this owned add-on DLL too, while
  leaving unrelated plugins, saves, and BepInEx alone.

## Release packaging

When the actual client patch is ready, include the compiled DLL at
`dist/vX.Y.Z/mod/SmartCraftCraftingAddon.dll` before packaging.
`scripts/package-release.py` automatically adds the optional feature field
in the update manifest. **Do not publish a dummy or placeholder DLL.**

Before a public release, run the Windows PowerShell test workflow, the release
packager test workflow, and in-game tests on SmartCraftTest. Confirm the normal
Y-menu available-material count, exact consumption, output/remainder, full
inventory overflow, multiple players, and proper persistence across reconnect.

## Uninstall guarantees

The add-on-only uninstaller removes the one named SmartCraft DLL, not the
Inventory Search DLL, updater, BepInEx runtime, other plugins, or save data.
To reinstall after removal, use the standalone manager and opt in explicitly.
