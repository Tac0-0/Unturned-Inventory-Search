#!/usr/bin/env python3
"""Package a full Inventory Search release for the self-updater.

Usage: python3 scripts/package-release.py 1.1.0
Requires a versioned DLL at dist/v1.1.0/InventorySearchPrototype.dll.
"""
import hashlib
import json
import pathlib
import re
import shutil
import sys
import tempfile
import zipfile

BASE = pathlib.Path(__file__).resolve().parent.parent
if len(sys.argv) != 2 or not re.fullmatch(r"[1-9]\d*\.\d+\.\d+", sys.argv[1]):
    raise SystemExit("Usage: python3 scripts/package-release.py 1.1.0")
version = sys.argv[1]
tag = "v" + version
release_dir = BASE / "dist" / tag
binary_path = release_dir / "InventorySearchPrototype.dll"
binary = binary_path.read_bytes()
digest = hashlib.sha256(binary).hexdigest()
# A .NET UTF-8 attribute string encodes its length immediately before the bytes
# for short semantic versions (<=127 bytes).
if len(version) >= 128 or (bytes([len(version)]) + version.encode()) not in binary:
    raise SystemExit(f"Compiled DLL does not contain BepInEx plugin version {version}")
print(f"Package {tag}: DLL sha256={digest}")

assets = BASE / "release-assets"
assets.mkdir(exist_ok=True)
with tempfile.TemporaryDirectory(prefix="inventory-search-build-") as work:
    root = pathlib.Path(work) / f"InventorySearch-OneClick-{tag}"
    shutil.copytree(BASE / "installer", root)
    (root / "payload").mkdir(exist_ok=True)
    shutil.copy2(binary_path, root / "payload" / "InventorySearchPrototype.dll")
    (root / "payload" / "InventorySearchPrototype.sha256").write_text(digest + "\n", encoding="ascii")
    if (release_dir / "mod").is_dir():
        shutil.copytree(release_dir / "mod", root / "payload" / "mod", dirs_exist_ok=True)

    conf_path = root / "CONFIG.json"
    config = json.loads(conf_path.read_text(encoding="utf-8-sig"))
    config["version"] = version
    conf_path.write_text(json.dumps(config, indent=2) + "\n", encoding="utf-8")

    install_path = root / "Install.ps1"
    install = install_path.read_text(encoding="utf-8")
    install, count = re.subn(r"\$expectedHash = '[A-Fa-f0-9]{64}'",
                             f"$expectedHash = '{digest.upper()}'", install)
    if count != 1:
        raise SystemExit("Expected exactly one pinned DLL SHA256 in Install.ps1")
    install, count = re.subn(
        r"https://github\.com/Tac0-0/Unturned-Inventory-Search/releases/download/v\d+\.\d+\.\d+/InventorySearchPrototype\.dll",
        f"https://github.com/Tac0-0/Unturned-Inventory-Search/releases/download/{tag}/InventorySearchPrototype.dll",
        install)
    if count != 1:
        raise SystemExit("Expected exactly one release DLL download URL in Install.ps1")
    install_path.write_text(install, encoding="utf-8")

    (root / "README.md").write_text(
        f"# Inventory Search {tag}\n\n"
        "Extract the ZIP, close Unturned, and run INSTALL.cmd once. "
        "BepInEx 5 is installed if needed. Launch without BattlEye.\n\n"
        "After installing, you can delete this downloaded ZIP. "
        "The permanent updater runs at login and daily, or use "
        "Check for Updates in the Windows Start menu. "
        "Updates install the complete mod and updater from verified GitHub ZIP releases.\n\n"
        "Uninstall from %LOCALAPPDATA%\\InventorySearch\\UNINSTALL.cmd.\n",
        encoding="utf-8"
    )
    mappings = [
        ("payload/InventorySearchPrototype.dll", "plugin/InventorySearchPrototype.dll"),
        ("UPDATE-NOW.cmd", "app/UPDATE-NOW.cmd"),
        ("UNINSTALL.cmd", "app/UNINSTALL.cmd"),
        ("Uninstall.ps1", "app/Uninstall.ps1"),
        ("Install.ps1", "app/Install.ps1"),
        ("CONFIG.json", "app/package-CONFIG.json"),
        ("README.md", "app/README.md"),
        ("Update.ps1", "app/Update.ps1"),
        ("Manage-CraftingAddon.ps1", "app/Manage-CraftingAddon.ps1"),
        ("CRAFTING-ADDON.cmd", "app/CRAFTING-ADDON.cmd"),
        ("UNINSTALL-CRAFTING.cmd", "app/UNINSTALL-CRAFTING.cmd"),
    ]
    mod_dir = root / "payload" / "mod"
    if mod_dir.is_dir():
        for file in sorted(mod_dir.rglob("*")):
            if file.is_file():
                relative = file.relative_to(mod_dir).as_posix()
                mappings.append(("payload/mod/" + relative, "plugin/" + relative))
    files = []
    optional_files = []
    for src, target in mappings:
        entry = {"source": src, "target": target,
                 "sha256": hashlib.sha256((root / src).read_bytes()).hexdigest()}
        if target == "plugin/SmartCraftCraftingAddon.dll":
            # OLD UPDATERS process ALL manifest.files without understanding
            # optionalFeature. Therefore NEVER put the add-on in that array.
            # A separate optionalFiles section is ignored by old versions.
            entry["optionalFeature"] = "smartcraft-crafting"
            optional_files.append(entry)
        else:
            files.append(entry)
    (root / "update-manifest.json").write_text(
        json.dumps({"schemaVersion": 1, "version": tag,
                    "files": files, "optionalFiles": optional_files}, indent=2) + "\n",
        encoding="utf-8"
    )

    archive = assets / f"InventorySearch-OneClick-{tag}.zip"
    with zipfile.ZipFile(archive, "w", compression=zipfile.ZIP_DEFLATED, compresslevel=9) as zipped:
        for file in sorted(root.rglob("*")):
            if file.is_file():
                zipped.write(file, file.relative_to(root.parent))
    with zipfile.ZipFile(archive, "r") as zipped:
        if zipped.testzip() is not None:
            raise SystemExit("ZIP CRC verification failed")
    shutil.copy2(binary_path, assets / "InventorySearchPrototype.dll")
    names = ["InventorySearchPrototype.dll", archive.name]
    (assets / "SHA256SUMS.txt").write_text(
        "".join(f"{hashlib.sha256((assets / n).read_bytes()).hexdigest()}  {n}\n" for n in names),
        encoding="ascii"
    )
    print(f"Full package ready: {archive}; {len(files)} managed files.")
