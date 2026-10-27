# Inventory Search v1.3.2

This release improves **version visibility** for full-package auto-updates and preserves the experimental opt-in SmartCraft Scrap functionality of v1.3.1.

## Version display and documentation

- Adds `%LOCALAPPDATA%\InventorySearch\SHOW-VERSION.cmd`, which checks the live installed `config.json`, compares the active Unturned DLL SHA-256 against the installed release manifest, and points to the up-to-date README.
- After a complete update, `UPDATE-NOW.cmd` reports where the live README is stored. A same-version check also identifies the live installation directory and version.
- Generated release READMEs now clearly identify the package snapshot, how to find the live README and the version checker.
- **Old extracted installer folders (for example, `InventorySearch-OneClick-v1.2.1`) are deliberately not renamed or edited.** They are user-owned download snapshots, not the live app. You can delete them after installing. Only files recorded as Inventory Search-managed are updated, retired or moved.

## Full-package update model

The v1.2.1+ updater downloads and verifies the entire new release ZIP, reconciles all mod-owned files and folders, removes obsolete owned files and now cleans empty obsolete package folders. It preserves unrelated BepInEx plugins and local settings. This also supports future major revisions that change owned file layouts.

## SmartCraft (still experimental)

The opt-in Windows client DLL and separate Rocket server DLL remain byte-identical to the working v1.3.1 builds. Only bundled Scrap is supported: loose `67`, x10 `49101`, x100 `49102`. The client uses the original Unturned Y recipes, and the matching server consumes bundles and returns change. One in-game end-to-end Scrap transaction passed; multiplayer concurrency, bulk crafting and non-Scrap materials remain unverified.

## Upgrade

Close Unturned and run the **installed** `%LOCALAPPDATA%\InventorySearch\UPDATE-NOW.cmd` to move from v1.2.1 or v1.3.1 to v1.3.2. Or use the extracted one-click installer ZIP for a new installation. No manual DLL copying is needed.
