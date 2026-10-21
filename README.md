# Unturned Inventory Search

A lightweight, **client-side** inventory search mod for [Unturned](https://store.steampowered.com/app/304930/Unturned/).

> **v1.0.0** ships a one-time installer and a full-package self-updater. The v0.1.6 search behavior has been tested in singleplayer and private multiplayer without BattlEye. Installation and complete-package update behavior have Windows PowerShell 5.1 automated tests; v1.0.0 gameplay validation on friends' PCs is ongoing.

## ⬇️ Easy installation (Windows)

### **[👉 DOWNLOAD THE INSTALLER ZIP (v1.0.0)](https://github.com/Tac0-0/Unturned-Inventory-Search/releases/download/v1.0.0/InventorySearch-OneClick-v1.0.0.zip)**

**No coding, compiling, or manual BepInEx setup needed.**

1. Click the download link above, then **extract the ZIP** (right-click → **Extract All**). Don't run it from inside the ZIP.
2. **Close Unturned**, open the extracted folder, and double-click **`INSTALL.cmd`**. Follow any prompts.
3. Launch Unturned **without BattlEye**, enter a world, open your inventory, and press **Ctrl+F** to search.

**Want to remove it?** Run **`UNINSTALL.cmd`** from the extracted folder. The installer and uninstaller were tested on one Windows laptop.

**Install says “extract ZIP first” even though you already did?** Use the [installer ZIP linked above](https://github.com/Tac0-0/Unturned-Inventory-Search/releases/download/v1.0.0/InventorySearch-OneClick-v1.0.0.zip), **not** GitHub's green `Code → Download ZIP`. The patched installer can recover a missing DLL by downloading the official release asset and checking its SHA-256. If that still fails, check Windows Security → Protection history for a quarantined `InventorySearchPrototype.dll` and share the exact error output. Don't disable antivirus to install this mod.

> This is a BepInEx client mod, not an automatic Steam Workshop download. The installer includes a permanent full-package updater, automatically scheduled at login and daily. No new ZIP downloads are needed for future releases.

## Features

- Replaces the character-rotation slider with an inventory search field.
- Press **Ctrl+F** while viewing your inventory to focus the search field.
- Matching items receive a green border around their full item rectangle.
- Nonmatching items are dimmed, not rearranged, dropped, or hidden.
- Search updates as you type and supports modded item names.
- Works with personal inventory and opened containers, including oversized modded storage using normal Unturned inventory widgets.
- Clears the search when you leave the inventory or use Esc in the field.

## Additional installation details

The installer detects your Unturned folder (or prompts you to select it), downloads official BepInEx 5.4.23.5 x64 if missing and verifies its published SHA-256 digest, then copies `InventorySearchPrototype.dll` into `Unturned/BepInEx/plugins/InventorySearchPrototype/`.

The installer and updater source is in [`installer/`](installer/). Review scripts before executing them, especially on shared or managed computers.

**Manual installation:** If you prefer, install BepInEx 5 x64 yourself, then download the [DLL directly](https://github.com/Tac0-0/Unturned-Inventory-Search/releases/download/v1.0.0/InventorySearchPrototype.dll) and place it in `Unturned/BepInEx/plugins/InventorySearchPrototype/`.

**Automatic updates from v1.0.0 onward:** You install **v1.0.0 once**. The installer puts a permanent updater under `%LOCALAPPDATA%\InventorySearch` and registers checks at Windows sign-in and daily. When v1.1.0, v1.2.0, v2.0.0, or a later stable release is published, the updater downloads its **full release ZIP** directly from GitHub Releases, verifies the ZIP's SHA-256 and the per-file update manifest, and updates the mod DLL, any additional mod files, updater, installer and uninstall scripts, and packaged documentation. Files retired by newer releases are removed **only** if an earlier manifest recorded that Inventory Search owned them. Existing game saves, other plugins, BepInEx, and the user-specific game path are preserved. Updates pause while Unturned is running.

**Manual update check (without downloading anything):** Windows Start menu → **Inventory Search → Check for Updates**. Or double-click `%LOCALAPPDATA%\InventorySearch\UPDATE-NOW.cmd`. The originally downloaded ZIP can be deleted after installation.

**Recovery:** The updater keeps `%LOCALAPPDATA%\InventorySearch\InventorySearchPrototype.previous.dll` after an upgrade. File replacement failures trigger an attempt to restore all changed files and configuration. If an updater script from an **older v1.0.0 installer** predates these improvements, run the **current v1.0.0 `INSTALL.cmd` once** to repair the updater; subsequent stable releases update automatically with no new installer download.

**Previous v0.1.6-beta installs:** The stable starting point for friends is a fresh **v1.0.0 install**. The beta installer predates the permanent full-package updater design; don't use it to judge future automatic-upgrade behavior.

## Compatibility and limitations

- Client-side **BepInEx 5 + Harmony** plugin. No server-side component required for the search itself.
- Intended for singleplayer and compatible private servers running without BattlEye. Normal Workshop subscriptions **cannot automatically install BepInEx DLLs**, and this mod is **not auto-installed when joining servers**.
- Private multiplayer was tested successfully without BattlEye on the author's server. Other servers and BattlEye-protected servers are not verified.
- Searching is purely visual; it reads rendered inventory item names and does not alter save contents or item placement.
- The installer may be blocked by organizational device policies; test on a personally controlled Windows machine when possible.

## Building from source

1. Install the .NET SDK / Framework 4.8 targeting pack, Unturned and BepInEx 5.
2. Open PowerShell in the repo root. Run:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Build-And-Install.ps1 -UnturnedDir 'C:\Path\To\Unturned' -Install
```

The C# project builds against **your local game assemblies**, which cannot be redistributed in this repository. A build may need code updates after Unturned releases.

## Development status

v0.1.6 search behavior was verified on Windows in singleplayer and private multiplayer without BattlEye. v1.0.0 uses version-metadata-only changes to that binary; the new v1.0.0 release package and full self-update system have automated Windows tests. In-game v1.0.0 confirmation on player machines and future-version compatibility are still being verified. See [v1.0.0 release notes](RELEASE_NOTES_v1.0.0.md) and [beta release notes](RELEASE_NOTES.md). Report issues with Unturned version, BepInEx version, and the relevant `BepInEx/LogOutput.log` entries.

## License

MIT. See [`LICENSE`](LICENSE). This is a community-created mod and is not affiliated with Smartly Dressed Games or BepInEx.