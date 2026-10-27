# Inventory Search v1.4.0: SmartCraft recipe cleanup (experimental)

## Upgrade channel

**v1.4.0** is the first numbered 1.4 release. Later bug fixes are numbered **v1.4.1, v1.4.2, ...** and are still delivered as **complete replacement ZIP packages** by UPDATE-NOW.cmd. Minor patch numbering does not mean DLL-only file swapping. The stable v1.3.2 auto-updater can discover this release once it is published.

## Changes

- Existing Inventory Search behavior is preserved. Main Inventory Search DLL has an updated version marker (v1.4.0).
- Optional SmartCraft client/server receives an experimental recipe-cleanup policy, applied in the client's native crafting menu and enforced by the Rocket server, without changing original game or Workshop asset files.
- Hides/rejects 1 Can -> 3 Scrap salvage while retaining the separate original 4 Scrap -> Can recipe.
- Hides/rejects exactly 20 inferior bundle-creation outputs, across 11 material families. Example: 2x2 More Stacking x10 Scrap loses to 1x1 More Farming x10 Scrap; 3x3 x100 loses to 2x2 x100. **Unstacking remains available**, so inferior old bundles are still usable.
- Ignores complex/stateful blueprints, recipes with other ingredients or byproducts, and recipes not proven to be equivalent. Server and client use the same conservative filter.
- The v1.4 material catalog currently identifies 451 bundle variants across 207 material families from the installed Unturned/Workshop content. **That mapping is only research data**: generalized cross-mod crafting, other families, multi-ingredient crafting and full bulk handling are not implemented in v1.4.0. Scrap x10/x100 remains the only bundle-consumption family that has passed an in-game transaction test.
- Users without SmartCraft opted in continue using the original Inventory Search plugin and normal recipes. Server owners must install the matching v1.4.0 SmartCraftServerPlugin.dll for recipe cleanup enforcement.

## Files and installation

Close Unturned and run the installed `%LOCALAPPDATA%\InventorySearch\UPDATE-NOW.cmd` to upgrade the complete managed package. The version checker remains `%LOCALAPPDATA%\InventorySearch\SHOW-VERSION.cmd` and the up-to-date installed README remains `%LOCALAPPDATA%\InventorySearch\README.md` (not an old v1.2.1 Downloads snapshot).

Release assets: complete `InventorySearch-OneClick-v1.4.0.zip`, separately available main `InventorySearchPrototype.dll`, separately available experimental Rocket `SmartCraftServerPlugin.dll`, plus `SHA256SUMS.txt`. The optional client SmartCraftCraftingAddon.dll is included with the full ZIP and respects opt-in.

**Test status:** Both v1.4.0 DLLs compiled successfully against the current game DLLs and 22 recipe-cleanup regression cases passed. Real in-game recipe suppression requires confirmation on SmartCraftTest; no universal crafting compatibility is claimed. Workshop source files and production WorkServer have not been modified.
