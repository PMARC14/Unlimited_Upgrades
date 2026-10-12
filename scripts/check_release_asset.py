#!/usr/bin/env python3
"""Checks that a release zip really contains the mod.

The first two releases shipped a zip with no AllUpgradesMod.dll in it,
because CI can't compile the mod and carried on anyway. This is the guard
against that: run it on any zip before (or right after) publishing it.

Usage: python3 scripts/check_release_asset.py ZIP [VERSION]
"""

import json
import sys
import zipfile

REQUIRED = ("AllUpgradesMod.dll", "manifest.json", "icon.png", "README.md")


def problems_for(path: str, expected_version: str | None = None) -> list[str]:
    """Everything wrong with the zip at `path` (empty list = good to ship)."""
    problems: list[str] = []
    try:
        archive = zipfile.ZipFile(path)
    except (OSError, zipfile.BadZipFile) as error:
        return [f"can't open as a zip: {error}"]

    with archive:
        names = set(archive.namelist())
        for name in REQUIRED:
            if name not in names:
                problems.append(f"missing {name}")

        # Only the mod's own DLL may ship. Anything else would be game or
        # library code copied in by mistake.
        for name in sorted(names):
            if name.lower().endswith(".dll") and name != "AllUpgradesMod.dll":
                problems.append(f"unexpected DLL {name} — only AllUpgradesMod.dll may ship")

        if "AllUpgradesMod.dll" in names:
            dll = archive.read("AllUpgradesMod.dll")
            if dll[:2] != b"MZ":
                problems.append("AllUpgradesMod.dll isn't a Windows DLL (no MZ header)")
            if len(dll) < 4096:
                problems.append(f"AllUpgradesMod.dll is only {len(dll)} bytes — that's not the real mod")

        if "manifest.json" in names and expected_version:
            try:
                version = json.loads(archive.read("manifest.json")).get("version_number")
            except ValueError:
                version = None
            if version != expected_version:
                problems.append(f"manifest.json says version {version!r}, expected {expected_version!r}")
    return problems


def main(argv: list[str]) -> int:
    if len(argv) < 2:
        print(__doc__)
        return 2
    path = argv[1]
    problems = problems_for(path, argv[2] if len(argv) > 2 else None)
    if problems:
        print(f"{path} is NOT a valid release zip:")
        for problem in problems:
            print(f"  - {problem}")
        return 1
    print(f"{path} looks like a real release zip.")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
