# SmartCraft v1.4 cross-mod stack catalog (development)

This is **source data**, not a tested v1.4 client/server release. The current validated release is v1.3.2 (Scrap-only). No production or test-server files have been changed.

## Discovery

The snapshot combines stack items actually present in SmartCraftTest Workshop content:

- **416 valid bundles** from More Stacking Workshop 3818995821, or 208 complete x10/x100 chains.
- **19 bundles** from More Farming Workshop 3729650953, many of which overlap materials in More Stacking.
- **16 vanilla stack barricades**. Some represent **12** logs/planks/sticks or **5** metal sheets, not the assumed 10.
- **451 unique bundle item IDs** for **207 base-item families** in total.

Catalog rows are `[bundleItemID, baseItemID, numberOfBaseItems]`, grouped by originating provider. This is a point-in-time snapshot. Future crafting handlers must resolve item assets on the actual server/client and verify recipes still match their intended definitions.

## Bad upstream definitions

More Stacking's `DragonfruitSoup10` ID 14732 has a reverse recipe referencing **14731** instead of itself. `DragonfruitSoup100` ID 14733 depends on that invalid chain. Both are excluded rather than risking conversion exploits.

## Target behavior for the next version

- Cross-provider equivalence: loose Scrap ID 67, vanilla stack 1909, More Stacking stacks 10162/10163, and More Farming stacks 49101/49102 share the same Scrap unit balance.
- Recipes may contain **multiple unrelated materials**, for example 4 Cloth plus 2 Scrap. Independent accounting must balance both ingredient families and the combined transaction.
- Recipes whose original inputs are stack items should accept equivalent denominations from other mods or loose materials, without replacing original recipes in the Y menu.
- A single family can contain several denomination sizes (1, 5, 10, 12, 100). Change must be exact and preserve the canonical source items where possible.
- Server is authoritative. If recipes require tools, workstations, skills, quality or state transfers, Unturned's validation must remain intact; unsupported unsafe cases must fail closed.
- Do not change production WorkServer, duplicate recipes, add fake resources, or overwrite unrelated mods.

## Still requiring live validation

This catalog alone does not make every recipe work. Bulk craft-many, multiple input families, food/medical/ammunition state, inventory overflow, direct-bundle recipes and multiplayer concurrency all require client/server development and real gameplay testing. Do not publish a stable universal-compatibility claim until they pass.

Regenerate the reference dataset with `python3 smartcraft/catalog/build_stack_catalog.py /path/to/U3DS output.json` when Workshop content changes.


## Recipe cleanup policy (v1.4 development)

The audited source snapshot in `recipe-preferences.v1.json` identifies **20 inferior bundle output item IDs** across 11 material families when a matching bundle with the *same number of underlying units* uses less inventory area. For example:

- More Stacking x10 Scrap (10162, 2×2) and vanilla x10 Scrap (1909, 2×2) lose to More Farming x10 Scrap (49101, 1×1).
- More Stacking x100 Scrap (10163, 3×3) loses to More Farming x100 Scrap (49102, 2×2).
- Existing inferior bundles **can still be unpacked**. Never hide any recipe that decomposes a larger-denomination bundle into smaller bundles or loose items.

The explicit Can preference suppresses only the vanilla **1 Can → 3 Scrap** salvage blueprint. **4 Scrap → 1 Can** is the separate original crafting blueprint and must remain. There is no second vanilla 1 Can → 4 Scrap salvage blueprint in the installed asset files.

`shared/RecipeCleanupPolicy.cs` is intended to be compiled into both the BepInEx client (for recipe-list hiding via `IsBlueprintPermanentlyDisabled`) and the Rocket server (for authoritative rejection of suppressed crafting requests). It only filters **ordinary, all-consumed, material-balanced, one-output** inferior-stack creation recipes, plus the explicit Can salvage rule. Complex recipes, repair, fill, state transfer, extra outputs, different materials, and native unstack operations are unaffected.

Verified so far: both development assemblies compile against current Unturned managed assemblies; 22 standalone suppression regression tests and Python catalog/preference tests pass. **No in-game recipe cleanup test has run. No recipe deletion or rewrite was performed on any workshop files or live server.** This remains experimental v1.4 work, not part of the published v1.3.2 package.
