# Inventory Search v1.4.2: Strawberry fruitcrates / More Farming crops

v1.4.2 is a full-package patch of the experimental SmartCraft cross-mod engine. The Inventory Search implementation is unchanged from v1.4.1 except for version metadata. The same UPDATE-NOW.cmd installs all managed new files and respects your optional SmartCraft selection.

## Fixed

- **Strawberry fruitcrate x10 (48147)** and **large Strawberry crate x100 (48177)** are now equivalent to original Strawberry item **42213** in the original Y crafting recipes.
- The v1.4.1 catalog omitted the More Farming furniture fruitcrate family. Added all **38 reversible fruitcrate variants representing 19 crops** after validating each forward/reverse recipe in More Farming's original CraftingAsset definitions (including original vanilla vegetable crops).
- The shared stack catalog contains **489 verified bundles across 219 material families**. All 489 bundle identities are checked against their correct GUID, so numeric item-ID collisions cannot be silently substituted.
- More Farming fruitcrates take priority over less compact alternatives when selecting refundable bundle denominations. Original recipes, ordinary Craft Y behavior, stack/unstack recipes and v1.4.0 inferior-recipe cleanup are retained.

## In-game test case

With one Strawberry fruitcrate x10 (48147) and eight loose Strawberries (42213) in the player's inventory, original `10 Strawberry -> Strawberry fruitcrate` should recognize **18 Strawberry-equivalent units**, rather than reporting only 8 of 10. A normal ten-Strawberry-cost recipe should consume ten equivalent units without material loss. A x100 fruitcrate contributes 100 Strawberry units.

## Safety and verification

Both v1.4.2 DLLs compile against the currently installed Unturned managed assemblies. Catalog/GUID static checks pass for all 489 variants; 192 bounded-planner tests and 22 conservative recipe-cleanup tests pass. These tests do **not** substitute for live gameplay verification of Strawberry or other production recipes. Test first on SmartCraftTest, then validate on WorkServer.

## Upgrade

Close Unturned and run `%LOCALAPPDATA%\InventorySearch\UPDATE-NOW.cmd`. The client SmartCraft DLL is optional and requires a compatible server with the separately supplied v1.4.2 `SmartCraftServerPlugin.dll` in Rocket/Plugins. The production server can be rolled back by restoring its previous DLL; back up gameplay data before deployments.
