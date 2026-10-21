# Inventory Search v1.0.0

## Why this release exists

This release is intended to validate **updating from v0.1.6-beta to v1.0.0** using the existing `UPDATE-NOW.cmd` updater.

- Same search behavior and inventory UI as the successfully tested v0.1.6 beta.
- BepInEx plugin version metadata and loading log changed to **1.0.0**.
- The v1.0.0 DLL has a different verified SHA-256 digest, so the updater must download, validate and replace the binary. This is not a cosmetic GitHub tag change.
- Full installer ZIP is also available for new users.

**Build provenance:** This updater-validation build was produced by precisely changing the two same-length version strings in the previously tested unsigned v0.1.6 .NET assembly. No functional IL changes were made. The corresponding source file now specifies v1.0.0. A clean source rebuild is still recommended for future development.

**v1.0.0 DLL SHA-256:** `a45df1ee2202b05a20cdaf98bbfdbd8ad016113e7536381e9a9cb30f072aefc2`

## Test the updater

1. Start with an installed **v0.1.6-beta** release, with `%LOCALAPPDATA%\InventorySearch\Update.ps1` present.
2. **Close Unturned.** Make sure the game process has exited.
3. From your existing extracted v0.1.6 installer folder, run `UPDATE-NOW.cmd`.
4. Expected output: `Inventory Search updated from official release: v1.0.0`.
5. Run it again. Expected: `Inventory Search is already up to date.`
6. Launch Unturned **without BattlEye**. Confirm G then Ctrl+F opens the search field and search/highlighting still work in singleplayer and/or a supported private server.

If the updater script is missing, reinstall the latest complete installer first. If the update fails, keep the full console error and check `%LOCALAPPDATA%\InventorySearch\InventorySearchPrototype.previous.dll` for an automatic rollback backup.

**Important:** This updater test has not yet been confirmed on your friends' Windows machines. Do not describe it as proven until checked in-game. This plugin does not support BattlEye-protected servers.
