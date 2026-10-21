# Inventory Search v1.2.1

**Full-package self-updater upgrade.** No SmartCraft crafting engine is included. Future crafting implementation remains planned for v1.3+.

- The updater downloads and verifies the entire official release ZIP and its per-file SHA-256 manifest, then reconciles **every package-owned file**: DLLs, CMD launchers, PS1 updater/installer scripts, documentation, and nested package resources.
- Newly added files (including future `INSTALL.cmd`/`UNINSTALL-CRAFTING.cmd` equivalents) install automatically without hardcoding each filename. The packager discovers files present in the release.
- Files retired from a newer version are removed **only** when a previous installed manifest identifies the files as belonging to Inventory Search. Other mods, game saves, BepInEx, local preferences, and unrelated files remain intact.
- The updater no longer assumes that matching DLL/version metadata means the whole package is installed. It checks each installed file's SHA-256 even for the same version, repairing missing or damaged scripts.
- Identical files are left untouched, and installation/replacement has rollback logic. The installed per-user launcher/updater refreshes itself.
- Fresh `INSTALL.cmd` installation uses the same complete manifest to install all application-owned files, including previously unlisted installer commands and supporting scripts.

**Compatibility:** Existing older DLL-only updater generations are not changed retroactively. To join the complete-package update system, those installations need a one-time run of **the latest extracted one-click ZIP's `INSTALL.cmd`**. Subsequent upgrades can use the installed updater. The updater does not modify an old manually extracted ZIP folder; installed tools are synchronized inside `%LOCALAPPDATA%\InventorySearch`.

**Verification:** Synthetic-package and Windows integration tests validated that new commands appear, retired owned commands are removed, unrelated files remain, and a current DLL with missing/damaged commands is repaired.
