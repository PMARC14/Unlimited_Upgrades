#!/usr/bin/env python3
"""Builds the Thunderstore package zip from a compiled AllUpgradesMod.dll.

Usage: python3 scripts/make_package.py DLL [--out dist] [--namespace BaconGames]

Writes <out>/<Namespace>-<name>-<version>.zip (name and version come from
manifest.json) and prints its path. The zip holds exactly the mod DLL,
manifest.json, README.md and icon.png. It is verified before this script
returns, so a bad package fails here instead of reaching a release.
"""

import argparse
import json
import sys
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(Path(__file__).resolve().parent))
from check_release_asset import problems_for  # noqa: E402

DLL_NAME = "AllUpgradesMod.dll"
PACKAGED = ("manifest.json", "README.md", "icon.png")


def build(dll: Path, out_dir: Path, namespace: str) -> Path:
    manifest = json.loads((ROOT / "manifest.json").read_text(encoding="utf-8"))
    name, version = manifest["name"], manifest["version_number"]

    if not dll.is_file():
        raise SystemExit(f"error: {dll} doesn't exist — did the build succeed?")

    out_dir.mkdir(parents=True, exist_ok=True)
    zip_path = out_dir / f"{namespace}-{name}-{version}.zip"
    if zip_path.exists():
        zip_path.unlink()

    with zipfile.ZipFile(zip_path, "w", zipfile.ZIP_DEFLATED) as archive:
        archive.write(dll, DLL_NAME)
        for filename in PACKAGED:
            archive.write(ROOT / filename, filename)

    problems = problems_for(str(zip_path), version)
    if problems:
        zip_path.unlink()
        raise SystemExit("error: refusing to produce a bad package:\n  - " + "\n  - ".join(problems))
    return zip_path


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    parser.add_argument("dll", type=Path, help="path to the compiled AllUpgradesMod.dll")
    parser.add_argument("--out", type=Path, default=ROOT / "dist")
    parser.add_argument("--namespace", default="BaconGames", help="Thunderstore team/namespace")
    args = parser.parse_args()
    # Forward slashes so the path works in bash on Windows runners (basename etc.).
    print(build(args.dll, args.out, args.namespace).as_posix())
    return 0


if __name__ == "__main__":
    sys.exit(main())
