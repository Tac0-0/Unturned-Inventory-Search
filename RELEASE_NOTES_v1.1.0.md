# Inventory Search v1.1.0

## Release goal: exercise the install-once updater

This release carries the same gameplay search behavior as v1.0.0 with BepInEx plugin version metadata and load logging changed to **1.1.0**.

The new v1.1.0 DLL is a version-only byte patch of the previously tested v1.0.0 binary, preserving all functional IL. Future releases should be rebuilt from source using installed Unturned assemblies.

This release also includes **`payload/mod/release-info.txt`**, a harmless text file in the Inventory Search plugin directory. It lets us verify that the *complete-package updater* installs extra mod resources as well as DLLs.

### What to test

- On the up-to-date v1.0.0 install: close Unturned and run **Start → Inventory Search → Check for Updates** (or use the already-installed `%LOCALAPPDATA%\InventorySearch\UPDATE-NOW.cmd`). The updater should download the complete v1.1.0 ZIP and update all managed files.
- Confirm the new DLL reports **1.1.0**, that `BepInEx/plugins/InventorySearchPrototype/release-info.txt` appears, and the permanent updater stays functional.
- Run another check. It should report that Inventory Search is already up to date.
- Launch Unturned without BattlEye and verify the search bar and highlighting work.

### Older v1.0.0 installs

Some early v1.0.0 builds shipped a **DLL-only updater**. These may successfully fetch the new v1.1.0 DLL, but cannot update their own updater scripts or install `release-info.txt`. That is a limitation of the older updater, not this release. Such an install needs a **one-time repair with the latest full-package installer** to enable complete self-updates for later versions. Do not claim the old updater has fully upgraded when only the DLL changes.

### Safety

- Unturned must be closed before updating.
- The full-package updater validates GitHub's release ZIP SHA-256 and each manifest file SHA-256, stages files, and attempts rollback on failure.
- Existing game saves, unrelated BepInEx plugins, and BepInEx runtime files are not replaced.
- Requires a client launch without BattlEye.
