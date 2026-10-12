#!/usr/bin/env python3
"""Checks that this repo is ready to be packaged and released.

CI can't compile the mod (it needs the game's Assembly-CSharp.dll, which
isn't in this repo), so what it *can* do is catch the mistakes that bite at
release time: version numbers that disagree, a bad Thunderstore manifest, a
wrong-sized icon, or a tag that doesn't match the version.

Usage: python3 scripts/check_release.py [TAG]     e.g. v1.0.2
Exits non-zero (and prints every problem) if anything is wrong.
"""

import json
import re
import struct
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
SEMVER = re.compile(r"^\d+\.\d+\.\d+$")
# Thunderstore: package names are letters, numbers, and underscores only.
NAME = re.compile(r"^[A-Za-z0-9_]+$")


def main(argv: list[str]) -> int:
    problems: list[str] = []

    def need(condition: bool, message: str) -> None:
        if not condition:
            problems.append(message)

    manifest_path = ROOT / "manifest.json"
    manifest: dict = {}
    try:
        manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    except (OSError, ValueError) as error:
        problems.append(f"manifest.json is missing or isn't valid JSON: {error}")

    version = str(manifest.get("version_number", ""))
    if manifest:
        need(bool(NAME.match(str(manifest.get("name", "")))),
             "manifest name may only use letters, numbers, and underscores")
        need(bool(SEMVER.match(version)),
             f"manifest version_number {version!r} isn't major.minor.patch")
        description = str(manifest.get("description", ""))
        need(0 < len(description) <= 250,
             f"manifest description must be 1-250 characters (it is {len(description)})")
        need(str(manifest.get("website_url", "")).startswith("https://"),
             "manifest website_url must be an https URL")
        need(isinstance(manifest.get("dependencies"), list),
             "manifest dependencies must be a list")

    # The same version appears in three places; they must agree.
    csproj = next((ROOT / "src").glob("*.csproj"), None)
    if csproj is None:
        problems.append("no .csproj found under src/")
    else:
        found = re.search(r"<Version>([^<]+)</Version>", csproj.read_text(encoding="utf-8"))
        need(found is not None and found.group(1).strip() == version,
             f"{csproj.name} <Version> ({found.group(1).strip() if found else 'missing'}) "
             f"doesn't match manifest version_number ({version})")

    plugin_versions = []
    for source in (ROOT / "src").glob("*.cs"):
        plugin_versions += re.findall(
            r'\[BepInPlugin\(\s*"[^"]*"\s*,\s*"[^"]*"\s*,\s*"([^"]+)"\s*\)\]',
            source.read_text(encoding="utf-8"),
        )
    need(len(plugin_versions) == 1,
         f"expected exactly one [BepInPlugin] attribute, found {len(plugin_versions)}")
    need(all(v == version for v in plugin_versions),
         f"[BepInPlugin] version {plugin_versions} doesn't match manifest version_number ({version})")

    # Thunderstore requires a 256x256 PNG icon.
    icon = ROOT / "icon.png"
    if not icon.is_file():
        problems.append("icon.png is missing")
    else:
        data = icon.read_bytes()
        if data[:8] != b"\x89PNG\r\n\x1a\n":
            problems.append("icon.png isn't a PNG")
        else:
            width, height = struct.unpack(">II", data[16:24])
            need((width, height) == (256, 256),
                 f"icon.png must be 256x256 for Thunderstore (it is {width}x{height})")

    need((ROOT / "README.md").is_file(), "README.md is missing")

    # A pushed tag must be the manifest's version.
    tag = argv[1].strip() if len(argv) > 1 else ""
    if tag.startswith("v") and SEMVER.match(tag[1:]):
        need(tag[1:] == version,
             f"tag {tag} doesn't match manifest version_number ({version}); "
             "bump the version everywhere first, then tag")

    if problems:
        print("Release check failed:")
        for problem in problems:
            print(f"  - {problem}")
        return 1
    print(f"Release check passed for version {version}.")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
