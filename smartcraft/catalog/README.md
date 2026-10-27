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
