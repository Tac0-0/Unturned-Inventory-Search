# SmartCraft v1.3.0 (experimental)

Optional BepInEx client add-on and matching Rocket dedicated-server plugin for **original** Unturned Y-menu crafting. Only the More Farming Scrap family is currently supported: loose 67, x10 bundle 49101, x100 bundle 49102.

## Verified

- Windows client Alpha 4 Harmony hooks survive frame-zero BepInEx host destruction and modify original recipe counts.
- Server plugin uses `PlayerCrafting.OnCraftBlueprintRequestedV2`, clones only the current blueprint and substitutes bundles with real `CachingBcAssetRef` references. The original blueprint assets and recipes remain unchanged.
- Live SmartCraftTest crafting of a can from one x100 Scrap bundle consumed four equivalent Scrap and returned nine x10 bundles plus six loose Scrap (one item dropped due to full inventory). No material duplication or loss observed in that transaction.
- Offline 12 bundle-planner checks plus blueprint-reference constructor regression test passed. The actual gameplay proof was a **single-player** transaction.

## Not yet established

Bulk craft-many, concurrent players, item overflow in unusual containers, duplicate Scrap input rows, other ingredient families, stacked seeds, and edge recipes are not yet validated. The client can appear craftable on servers not running SmartCraft and must be used only with the matching dedicated-server plugin.

## Installation

The official v1.3.0 pre-release ZIP contains `payload/mod/SmartCraftCraftingAddon.dll` for optional client installation and includes a verified local add-on cache so users can toggle SmartCraft later while stable updaters ignore pre-releases. `SmartCraftServerPlugin.dll` is a **separate release asset**; place it in the Unturned dedicated server's Rocket Plugins directory and restart the server. Do not put the Rocket plugin in a client BepInEx folder. Do not put the client plugin in a server Rocket folder.

Sources: `client/SmartCraftClientPlugin.cs` and `server/SmartCraftServerPlugin.cs`. The experimental test world is distinct from the production Unturned server.
