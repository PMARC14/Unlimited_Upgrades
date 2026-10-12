# Releasing

Releases are built **on a PC with House of the Dying Sun installed**. CI can't
compile the mod: it needs the game's `Assembly-CSharp.dll`, which can't be
committed to this repo. (Earlier releases v1.0.0 and v1.0.1 were built by CI
anyway and shipped **without** `AllUpgradesMod.dll` — re-upload those zips.)

## Steps

1. **Bump the version in all three places** — CI checks they agree:
   - `manifest.json` → `version_number`
   - `src/AllUpgradesMod.csproj` → `<Version>`
   - `src/AllUpgradesPlugin.cs` → the version in `[BepInPlugin(...)]`
2. **Build the package** (needs the game and BepInEx; see the paths at the top
   of the `.csproj`):
   ```powershell
   powershell -ExecutionPolicy Bypass -File package.ps1
   ```
   This writes `dist/pmadd-Unlimited_Upgrades-<version>.zip`.
3. **Sanity-check the zip** (optional but quick):
   ```bash
   python3 scripts/check_release_asset.py dist/pmadd-Unlimited_Upgrades-<version>.zip <version>
   ```
4. **Publish**:
   ```bash
   gh release create v<version> dist/pmadd-Unlimited_Upgrades-<version>.zip --generate-notes
   ```
   Then upload the same zip to Thunderstore.

## What CI does

- Every push and pull request: `scripts/check_release.py` checks the manifest,
  the three version numbers, the 256×256 icon, and (for tags) that the tag
  matches the version.
- Every published release: downloads its `.zip` and fails if the real mod isn't
  inside (missing DLL, not a Windows DLL, wrong version). If you attach the zip
  after publishing and the check ran too early, just re-run the job.
