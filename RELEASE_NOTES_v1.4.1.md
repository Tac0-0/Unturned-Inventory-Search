# Inventory Search v1.4.1: SmartCraft cross-provider recipe compatibility (experimental)

## Patch numbering and updating

v1.4.1 is a patch release after v1.4.0. As before, `UPDATE-NOW.cmd` downloads the **entire** SHA-256-checked package, reconciles managed directories, updates the SmartCraft opt-in DLL and keeps personal settings. The stable v1.4.0 updater will discover v1.4.1 automatically. The old downloaded v1.4.0 folder is a snapshot, not the live install.

## SmartCraft improvements

- Generalizes the old More Farming-only Scrap handler to an experimental cross-provider stack catalog containing **451 bundle variants in 207 base-material families** from vanilla Unturned, More Farming and More Stacking Workshop packs installed on SmartCraftTest.
- Allows original native Y-menu recipes to consider equivalent stacks and loose materials from the same base item. Multiple separate ingredient families can be planned in the same craft request (for example, bundled Scrap and Cloth in one recipe).
- Uses a bounded, server-authoritative inventory planner to select real held jars, minimize overpayment, and create exact change from the base material. Original recipe output, crafting skill and workbench conditions remain subject to Unturned's own validation.
- Supports non-decimal denominations where the catalog defines them, including vanilla x12 logs/planks/sticks and x5 metal sheets.
- Verifies **all 451 bundle GUIDs** against the loaded item assets to reject colliding or spoofed numeric IDs.
- Preserves v1.4.0 recipe cleanup: skips inefficient bundle-creation recipes and 1 Can -> 3 Scrap salvage, retains 4 Scrap -> Can and all unstacking.

## Scope and caution

**Experimental; do not assume every recipe is safe yet.** Conversion is attempted only for ordinary, fully consumed, total-item-count recipe supplies. Repair, fill, state-transfer and other special semantics are delegated to vanilla crafting without rewriting. The engine does not modify workshop assets or create extra recipes. Complex quality/state-bearing items, bulk crafting, client/server mismatches, overflow and multi-player concurrency need in-game tests.

**Verification so far:** the two v1.4.1 DLLs compile with current Unturned assemblies; 186 planner regression cases, 22 cleanup cases, and catalog checks passed, along with the Harmony ABI check. End-to-end v1.4.1 gameplay has NOT yet been confirmed. On the test server, install the matching Rocket SmartCraftServerPlugin.dll before enabling the new client SmartCraft addon.

## Download assets

`InventorySearch-OneClick-v1.4.1.zip` includes the main client search plugin and an optional SmartCraft BepInEx DLL. `SmartCraftServerPlugin.dll` is separate and must be placed in a dedicated server Rocket Plugins folder. `SHA256SUMS.txt` verifies assets. The stable Inventory Search functionality is identical to v1.4.0 apart from its version marker.
