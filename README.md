# Unturned Inventory Search

A lightweight, **client-side** inventory search mod for [Unturned](https://store.steampowered.com/app/304930/Unturned/).

> **v1.0.0 (updater-validation release).** Search functionality was verified in singleplayer and private multiplayer without BattlEye, including More Storage containers, using v0.1.6. v1.0.0 contains the same functionality with updated version metadata. Automatic updates and in-game v1.0.0 behavior are being tested.

## ⬇️ Easy installation (Windows)

### **[👉 DOWNLOAD THE INSTALLER ZIP (v1.0.0)](https://github.com/Tac0-0/Unturned-Inventory-Search/releases/download/v1.0.0/InventorySearch-OneClick-v1.0.0.zip)**

**No coding, compiling, or manual BepInEx setup needed.**

1. Click the download link above, then **extract the ZIP** (right-click → **Extract All**). Don't run it from inside the ZIP.
2. **Close Unturned**, open the extracted folder, and double-click **`INSTALL.cmd`**. Follow any prompts.
3. Launch Unturned **without BattlEye**, enter a world, open your inventory, and press **Ctrl+F** to search.

**Want to remove it?** Run **`UNINSTALL.cmd`** from the extracted folder. The installer and uninstaller were tested on one Windows laptop.

**Install says “extract ZIP first” even though you already did?** Use the [installer ZIP linked above](https://github.com/Tac0-0/Unturned-Inventory-Search/releases/download/v1.0.0/InventorySearch-OneClick-v1.0.0.zip), **not** GitHub's green `Code → Download ZIP`. The patched installer can recover a missing DLL by downloading the official release asset and checking its SHA-256. If that still fails, check Windows Security → Protection history for a quarantined `InventorySearchPrototype.dll` and share the exact error output. Don't disable antivirus to install this mod.

> This is a BepInEx client mod, not an automatic Steam Workshop download. The updater is included; **v0.1.6-beta → v1.0.0 is the first real upgrade test** and has not yet been verified on Windows.

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

**Testing an upgrade:** If you already installed v0.1.6-beta, **close Unturned** and run `UPDATE-NOW.cmd` from its extracted installer folder. It should say `Inventory Search updated from official release: v1.0.0`; a second run should say `Inventory Search is already up to date.` Then launch the game and verify the search works. This is our first end-to-end updater test. See [v1.0.0 release notes](RELEASE_NOTES_v1.0.0.md).

**If `UPDATE-NOW.cmd` reports that `Update.ps1` is missing:** Download and extract the latest installer ZIP, then rerun `INSTALL.cmd` with Unturned closed. The updated `UPDATE-NOW.cmd` will restore the updater script from the extracted folder if installation configuration exists. If configuration is missing too, the original install did not complete and must be run again. After reinstalling v1.0.0, the update check should say you're up to date. To verify the actual upgrade, run an updater originally installed with v0.1.6 against the v1.0.0 release.

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

v0.1.6 was built and verified on Windows in singleplayer and private multiplayer without BattlEye; its installer, uninstaller and fresh BepInEx setup were tested. v1.0.0 is a version-metadata-only binary update from that working build, issued to validate the updater. The actual v0.1.6 → v1.0.0 upgrade and v1.0.0 in-game loading still need testing. See [v1.0.0 release notes](RELEASE_NOTES_v1.0.0.md) and [beta release notes](RELEASE_NOTES.md). Report issues with Unturned version, BepInEx version, and the relevant `BepInEx/LogOutput.log` entries.

## License

MIT. See [`LICENSE`](LICENSE). This is a community-created mod and is not affiliated with Smartly Dressed Games or BepInEx.