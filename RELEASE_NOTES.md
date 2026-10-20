# v0.1.6-beta — first public beta

### Highlights
- Live inventory search field replaces the character rotation slider.
- Ctrl+F to focus search.
- Green outlines on matching item rectangles; darkened nonmatching items.
- Supports modded item names, personal inventory, and More Storage containers.
- Doesn't rearrange, drop, delete, or consume items.

### Requirements
- Unturned on Windows, compatible with the source-assembly version.
- BepInEx 5 for Mono (Windows x64).
- Launch without BattlEye for client-side modification testing.

### Install
Download and extract `InventorySearch-OneClick-v0.1.6-beta.zip`, then double-click `INSTALL.cmd` while Unturned is closed. The ZIP includes the tested client DLL and installer scripts. Alternatively install `InventorySearchPrototype.dll` manually into `BepInEx/plugins/InventorySearchPrototype/`.

### Known limitations
- Singleplayer functionality tested. Multiplayer and the installer on a fresh computer are not yet tested.
- Not installable automatically from a Steam Workshop subscription or on joining a server.
- Official GitHub updater becomes useful once this release is published.
- Always verify third-party scripts before running them.

### Build provenance
- Tested DLL SHA-256: `405662cb3023da2d6cdd7d5d8ef62457e624d654489da8fb729a8a3141bed515`
- Original test platform: Windows 64-bit, BepInEx 5.4.23.5, Unturned Unity 2022.3.62.