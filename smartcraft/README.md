# SmartCraft v1.3 engine (EXPERIMENTAL)

**Never publish this as a working update until a two-player SmartCraftTest session passes.** Both DLLs compile against the current Unturned headless assembly, but only the **real client build** can validate the `Y` UI Harmony patch.

## Scope currently implemented
- Client source: `smartcraft/client/SmartCraftClientPlugin.cs`.
  - BepInEx 5 / Harmony postfix on vanilla `PlayerCrafting.UpdateBlueprintInputItemStatus`.
  - Adds **virtual** input counts for More Farming's original **ID 49101 (x10)** and **49102 (x100)** to vanilla **Metal Scrap ID 67** crafting rows.
  - Preserves native blueprint identity and its normal `Y` entry. No recipe duplication.
  - Does not install a new item, change inventory, or create ammo-style stacks.
- Server source: `smartcraft/server/SmartCraftServerPlugin.cs`.
  - Rocket plugin uses native `PlayerCrafting.OnCraftBlueprintRequestedV2` callback.
  - Replaces only the CURRENT REQUEST's blueprint with an in-memory clone; original assets remain unchanged.
  - Substitutes bundled supplies for deficient loose Scrap and adds correct change output. Ex.: one original 100 Scrap bundle -> one recipe costing 2 scrap + 9 original ten-bundles + 8 loose Scrap.
  - All vanilla crafting validations (skills, nearby workstations/tags, quests, additional required materials, output granting) remain in the game's implementation. Invalid requests are rejected by vanilla.
  - **Fail-closed** on hook exceptions; no manual item-give commands.
- `SmartCraftTest` remains offline until explicit testing. Production `WorkServer` must never be modified before validation.

## Important current limitations
- Only the first material family **Scrap 67/49101/49102** is enabled. Cloth (66/49103/49104), other More Farming stacks, and any More Stacking pairs are subsequent compatibility passes.
- State-transfer and repair/fill blueprints are deliberately left to vanilla.
- Craft-many behavior, UI count rendering, test-world dropped overflow and multiplayer races need live verification. Output leftovers use the vanilla crafting item-grant path (which may drop items when inventory is full).
- Client plugin has not been runtime-tested in Windows Unturned. It must be tested only on SmartCraftTest with a matching server plugin; on an unsupported server it can show craftable items that the server refuses.
- No compiled engine is included in stable v1.2.1. **Never substitute a placeholder DLL.**

## Build (dev/test)
Dependencies are **not** committed or redistributed: game `Assembly-CSharp.dll`, `UnturnedDat.dll`, `UnityEngine.CoreModule.dll`, Rocket API/Core for the server, BepInEx 5 `BepInEx.dll` and `0Harmony.dll` for the client.

Build the server Rocket DLL against dedicated-server assemblies, and the client BepInEx DLL against the actual client game build when available. A preliminary headless-assembly compilation was successful for both.

The stable updater already recognizes an optional future binary stored in a release as `dist/v1.3.0/mod/SmartCraftCraftingAddon.dll` under `optionalFiles`. Only opted-in players should receive it. Do not put it in mandatory `files`.

## Test matrix before release
1. Server **without** plugin + client **without** addon: vanilla untouched.
2. Client + server: original **x10** and **x100** bundle appearances unchanged; original `Y` recipe becomes craftable with sufficient equivalent units.
3. One x100, craft a two-scrap recipe => **9 x10 + 8 loose**, exact output quantity, no duplicates.
4. One x10, craft 2 scrap => **8 loose**. Loose-only crafting stays vanilla.
5. Mixed loose/x10/x100, adequate/inadequate materials, full inventory, disconnect/reconnect.
6. Workstation, skills, tool-not-consumed, modded AlicePack upgrade, and repeat-craft behavior.
7. Two concurrent players crafting, server restarts, client without addon, no item loss.
8. Uninstall only SmartCraft; Inventory Search remains intact; opt-out persists across full-package updates.
