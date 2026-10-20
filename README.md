# Unturned Inventory Search

A lightweight, **client-side** inventory search mod for [Unturned](https://store.steampowered.com/app/304930/Unturned/).

> **v0.1.6 beta.** Tested in singleplayer and on a private multiplayer server without BattlEye, including oversized More Storage containers. The installer and fresh BepInEx setup were tested on one Windows laptop; other PCs and automatic updates still need testing.

## ⬇️ Easy installation (Windows)

### **[👉 DOWNLOAD THE INSTALLER ZIP (v0.1.6 Beta)](https://github.com/Tac0-0/Unturned-Inventory-Search/releases/download/v0.1.6-beta/InventorySearch-OneClick-v0.1.6-beta.zip)**

**No coding, compiling, or manual BepInEx setup needed.**

1. Click the download link above, then **extract the ZIP** (right-click → **Extract All**). Don't run it from inside the ZIP.
2. **Close Unturned**, open the extracted folder, and double-click **`INSTALL.cmd`**. Follow any prompts.
3. Launch Unturned **without BattlEye**, enter a world, open your inventory, and press **Ctrl+F** to search.

**Want to remove it?** Run **`UNINSTALL.cmd`** from the extracted folder. The installer and uninstaller were tested on one Windows laptop.

> This is a BepInEx client mod, not an automatic Steam Workshop download. The automatic updater is included but has **not been tested with a future release yet**.

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

**Manual installation:** If you prefer, install BepInEx 5 x64 yourself, then download the [DLL directly](https://github.com/Tac0-0/Unturned-Inventory-Search/releases/download/v0.1.6-beta/InventorySearchPrototype.dll) and place it in `Unturned/BepInEx/plugins/InventorySearchPrototype/`.

**Updates:** An updater is included and can check GitHub Releases, but its end-to-end upgrade behavior is not tested yet. We'll validate it against a later release.

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

The v0.1.6 DLL was built on Windows and verified in singleplayer and private multiplayer without BattlEye. The installer, uninstaller, and fresh BepInEx setup were tested successfully on one Windows laptop; automatic updates and compatibility with other PCs still need testing. See [beta release notes](RELEASE_NOTES.md). Report issues with Unturned version, BepInEx version, and the relevant `BepInEx/LogOutput.log` entries.

## License

MIT. See [`LICENSE`](LICENSE). This is a community-created mod and is not affiliated with Smartly Dressed Games or BepInEx.