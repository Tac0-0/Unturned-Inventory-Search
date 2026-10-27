# Inventory Search v1.3.1

## Automatic upgrade from v1.2.1

v1.3.1 is a **stable update channel release** that the existing `UPDATE-NOW.cmd` can discover (v1.3.0 was a pre-release and was therefore intentionally skipped).

- The updater downloads a complete, SHA-256-verified release ZIP, installing all new files and folders, replacing changed package files, and retiring old package-owned files absent from the new manifest. This supports major future redesigns, not merely DLL swaps.
- Per-user configuration and SmartCraft opt-in preference are preserved; unrelated BepInEx plugins and user-owned files remain untouched.
- The main Inventory Search functionality uses the working v1.2.1 implementation with a version metadata update.
- Optional SmartCraft (experimental, Scrap-only) client is included; installs only for users who previously opted in or explicitly enable it with the bundled manager.
- A matching `SmartCraftServerPlugin.dll` is available as a separate Rocket server download and is never installed into a client BepInEx folder.

## SmartCraft current scope

Only the More Farming Metal Scrap family is supported (loose ID 67, x10 ID 49101, x100 ID 49102). The original Y recipes are reused, with server-side authoritative bundle consumption and returned change. One live transaction used one x100 Scrap bundle to craft a can for four Scrap, returning nine x10 bundles and six loose Scrap, including one loose item dropped on a full inventory. Bulk crafting, concurrent players, and other materials are **not yet validated**.

## How to upgrade

Close Unturned, then run the existing `UPDATE-NOW.cmd` in your Inventory Search installation. It will automatically download the complete v1.3.1 package, reconcile the owned directories, and keep your options. Or download `InventorySearch-OneClick-v1.3.1.zip`, extract, and run `INSTALL.cmd`. Do not use either DLL by itself as a substitute for the complete package when upgrading your updater.
