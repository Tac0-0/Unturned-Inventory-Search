# Unturned Inventory Search

A lightweight, **client-side** inventory search mod for [Unturned](https://store.steampowered.com/app/304930/Unturned/).

> **v0.1.6 beta**. Tested by the author in a singleplayer world, including oversized More Storage containers. Multiplayer support and a fresh installation on other PCs have **not yet been verified**.

## Features

- Replaces the character-rotation slider with an inventory search field.
- Press **Ctrl+F** while viewing your inventory to focus the search field.
- Matching items receive a green border around their full item rectangle.
- Nonmatching items are dimmed, not rearranged, dropped, or hidden.
- Search updates as you type and supports modded item names.
- Works with personal inventory and opened containers, including oversized modded storage using normal Unturned inventory widgets.
- Clears the search when you leave the inventory or use Esc in the field.

## Download and install

**Get the official v0.1.6-beta installer ZIP from [GitHub Releases](https://github.com/Tac0-0/Unturned-Inventory-Search/releases).** (Link becomes active after the first release is published.)

1. Close Unturned and extract the full installer ZIP.
2. Double-click `INSTALL.cmd`; the installer locates your Unturned installation (or prompts you to select it).
3. If necessary, it downloads official BepInEx 5.4.23.5 x64, verifies its published SHA-256 digest, and installs missing files.
4. The included Inventory Search DLL is copied to `Unturned/BepInEx/plugins/InventorySearchPrototype/`.
5. Launch Unturned **without BattlEye**. Open the inventory with G and press Ctrl+F.

The installer and updater source code are in [`installer/`](installer/). Please review scripts before executing them, especially on shared computers.

**Manual installation:** Install BepInEx 5 x64 first, then place the release asset `InventorySearchPrototype.dll` in `Unturned/BepInEx/plugins/InventorySearchPrototype/`.

**Update mechanism:** After setup, a per-user scheduled task checks the official GitHub Releases feed at login and daily. It checks release-asset SHA-256 digests and does not install an update while Unturned is running. `UPDATE-NOW.cmd` can check manually.

**Uninstall:** Run `UNINSTALL.cmd` from the installer archive. BepInEx and other plugins are preserved.

## Compatibility and limitations

- Client-side **BepInEx 5 + Harmony** plugin. No server-side component required for the search itself.
- Intended for singleplayer and compatible private servers running without BattlEye. Normal Workshop subscriptions **cannot automatically install BepInEx DLLs**, and this mod is **not auto-installed when joining servers**.
- Multiplayer behavior is not yet verified. Do not claim compatibility with BattlEye-protected servers.
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

The v0.1.6 DLL was built on Windows and verified in singleplayer; the release installer and updater have only static checks so far. See [beta release notes](RELEASE_NOTES.md). Report issues with Unturned version, BepInEx version, and the relevant `BepInEx/LogOutput.log` entries.

## License

MIT. See [`LICENSE`](LICENSE). This is a community-created mod and is not affiliated with Smartly Dressed Games or BepInEx.