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
        # Keep a managed local copy for opt-in reinstalls while this GitHub
        # release is marked pre-release and skipped by the stable updater.
        addon = release_dir / "mod" / "SmartCraftCraftingAddon.dll"
        if addon.is_file():
            (root / "payload" / "addon").mkdir(exist_ok=True)
            shutil.copy2(addon, root / "payload" / "addon" / addon.name)

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
        "This README belongs to the downloaded release snapshot. After you update, "
        "the old extracted folder and its README keep their original version name.\n\n"
        "**Active installation:** %LOCALAPPDATA%\\InventorySearch\\\n"
        "**Current README:** %LOCALAPPDATA%\\InventorySearch\\README.md\n"
        "**Check installed version:** %LOCALAPPDATA%\\InventorySearch\\SHOW-VERSION.cmd\n\n"
        "For a new installation, extract the ZIP, close Unturned and run INSTALL.cmd. "
        "BepInEx 5 is installed if needed. Launch without BattlEye.\n\n"
        "For updates, close Unturned and run the permanently installed "
        "UPDATE-NOW.cmd or use Start Menu > Inventory Search > Check for Updates. "
        "That action downloads and reconciles the COMPLETE new package. "
        "The originally downloaded ZIP folder is not modified and can safely be "
        "deleted after installation.\n\n"
        "Uninstall from %LOCALAPPDATA%\\InventorySearch\\UNINSTALL.cmd.\n",
        encoding="utf-8"
    )
    # The release ZIP is the source of truth for *all* package-owned files.
    # Future CMD/PS1/docs/resources are discovered automatically: no hardcoded
    # allowlist to update when a new file appears or an old file is retired.
    mappings = [
        ("payload/InventorySearchPrototype.dll", "plugin/InventorySearchPrototype.dll"),
        ("payload/InventorySearchPrototype.sha256", "app/payload/InventorySearchPrototype.sha256"),
    ]
    for file in sorted(root.rglob("*")):
        if not file.is_file():
            continue
        source = file.relative_to(root).as_posix()
        if source in {"update-manifest.json",
                      "payload/InventorySearchPrototype.dll",
                      "payload/InventorySearchPrototype.sha256"}:
            continue
        if source.startswith("payload/mod/"):
            target = "plugin/" + source.removeprefix("payload/mod/")
        elif source == "CONFIG.json":
            # Never overwrite per-user config.json with packaged defaults.
            target = "app/package-CONFIG.json"
        else:
            target = "app/" + source
        mappings.append((source, target))
    # Fail at build time rather than shipping a package that the updater rejects.
    safe_path = re.compile(r"^[A-Za-z0-9_.-]+(/[A-Za-z0-9_.-]+)*$")
    targets = set()
    for source, target in mappings:
        if not safe_path.fullmatch(source) or not safe_path.fullmatch(target):
            raise SystemExit(f"Unsafe packaged path: {source} -> {target}")
        normalized = target.lower()
        if normalized in targets:
            raise SystemExit(f"Duplicate package destination: {target}")
        targets.add(normalized)
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
    # Separately publish Rocket server DLL, never place it in client ZIP.
    server_binary = release_dir / "server" / "SmartCraftServerPlugin.dll"
    if server_binary.is_file():
        shutil.copy2(server_binary, assets / "SmartCraftServerPlugin.dll")
        names.append("SmartCraftServerPlugin.dll")
    (assets / "SHA256SUMS.txt").write_text(
        "".join(f"{hashlib.sha256((assets / n).read_bytes()).hexdigest()}  {n}\n" for n in names),
        encoding="ascii"
    )
    print(f"Full package ready: {archive}; {len(files)} managed files.")
