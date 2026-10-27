# Inventory Search v1.3.0

**Pre-release:** it must be installed manually; stable v1.2.1 auto-updaters deliberately ignore pre-releases. This version preserves tested v1.2.1 Inventory Search functionality and introduces **SmartCraft**, an **optional experimental crafting add-on** for compatible private Unturned servers.

### SmartCraft, experimental / Scrap-only

- The normal **Y crafting menu** recognizes More Farming x10 and x100 Metal Scrap bundles as ingredient equivalents to loose Scrap (IDs 49101, 49102, and 67).
- With the matching **Rocket dedicated-server DLL**, real crafting consumes bundles and returns remaining original bundles and loose Scrap as change. The standard recipes are not replaced.
- Tested end-to-end: crafting a can using one x100 stack, cost 4 Scrap, returns nine x10 bundles and six loose Scrap (one of those loose Scrap fell to the ground because inventory space was full).
- **Not supported/tested yet:** other stack families, crafting many at once, farming seeds, and advanced/multi-input edge cases. Treat as a private-server experiment.
- SmartCraft remains opt-in. Existing opt-ins persist through the complete ZIP updater, while other players continue using Inventory Search without the crafting add-on.

### Downloads

- `InventorySearch-OneClick-v1.3.0.zip`: full Windows installer, automatic updater and optional client SmartCraft DLL.
- `InventorySearchPrototype.dll`: v1.3.0 core inventory search (same working search implementation as v1.2.1).
- `SmartCraftServerPlugin.dll`: separate **Rocket server-side** plugin. Only install this in the dedicated server's Rocket Plugins folder, never in the BepInEx client folder.
- `SHA256SUMS.txt`: checksums of the archive and the separately downloadable DLLs.

Close Unturned, extract the complete ZIP, and run `INSTALL.cmd` manually to install or migrate from an earlier version. Users on v1.2.1 will **not** update to this pre-release automatically. For BepInEx testing, run without BattlEye. On a private server with SmartCraft enabled, install the Rocket DLL separately and restart the server. Test backups are recommended.
