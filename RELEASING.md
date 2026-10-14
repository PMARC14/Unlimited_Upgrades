# Releasing

Releases are built by GitHub Actions and published to GitHub Releases. You then
upload the same zip to Thunderstore by hand.

## Making a release

1. **Bump the version in all three places** — CI checks they agree:
   - `manifest.json` → `version_number`
   - `src/AllUpgradesMod.csproj` → `<Version>`
   - `src/AllUpgradesPlugin.cs` → the version in `[BepInPlugin(...)]`
2. **Merge to `main`, then push a tag** that matches the version:
   ```bash
   git tag v1.0.2 && git push origin v1.0.2
   ```
3. The **Release** workflow builds the mod, packages
   `BaconGames-Unlimited_Upgrades-1.0.2.zip`, checks it really contains the mod,
   and publishes a GitHub Release with the zip attached.
4. **Upload that zip to Thunderstore** (team `BaconGames`, game House of the
   Dying Sun). Thunderstore won't accept a version it already has, so every
   Thunderstore upload needs a new version number.

### Dry run

*Actions → Release → Run workflow* with **publish** left off builds and packages
but publishes nothing; the zip is attached to the run. Do this first after any
change to the workflow or the setup below.

### Fixing an old release

*Run workflow* with **publish** on rebuilds the version currently in
`manifest.json` and replaces that release's zip (and removes the old, differently
named zip). That's how to repair v1.0.0/v1.0.1, whose zips shipped without the
mod, as long as the mod source hasn't changed since those tags.

## One-time setup: the game reference DLLs

Compiling the mod needs five DLLs that can't be committed to this public repo, so
the workflow reads them from a **private** repo.

1. **Create a private repo** (e.g. `PMARC14/hotds-build-refs`) with exactly this
   layout, copied from your game install and your BepInEx profile:
   ```
   game/dyingsun_Data/Managed/Assembly-CSharp.dll
   game/dyingsun_Data/Managed/UnityEngine.dll
   game/dyingsun_Data/Managed/Rewired_Core.dll
   bepinex/BepInEx.dll
   bepinex/0Harmony.dll
   ```
   (`bepinex/` files come from `...\profiles\Modded\BepInEx\core`, the path at
   the top of the `.csproj`.)
2. **Create a fine-grained personal access token**: GitHub → Settings →
   Developer settings → Fine-grained tokens. Repository access: *only* the
   private repo. Permission: **Contents → Read-only**. Nothing else.
3. **In this repo → Settings → Secrets and variables → Actions**:
   - *Secret* `REFS_TOKEN` = the token
   - *Variable* `REFS_REPO` = `PMARC14/hotds-build-refs` (owner/name)
4. Run the **dry run** above to confirm it builds.

The workflow deletes the checked-out DLLs right after compiling, only ever
packages `AllUpgradesMod.dll`, and fails if any other DLL ends up in the zip. It
never runs for pull requests, so those can't reach the private repo.

> **Terms of use:** this puts copies of the game's and Unity's DLLs on GitHub (in
> a private repo). Many mod projects do, but check the game's terms. Stripping
> the DLLs to metadata only (for example with `NStrip`) before committing them
> keeps them working for compiling while removing the game's code.

### Alternative: build on your own PC

If you'd rather not store the DLLs on GitHub, use a **self-hosted runner** on the
PC that has the game. In `release.yml` change `runs-on` to
`[self-hosted, windows]` and delete the three reference-assembly steps and the
two `-p:` build properties (the `.csproj` defaults point at a normal Steam and
r2modman install). The PC must be on when you build, and never let a
self-hosted runner run pull requests from forks.

Or skip CI entirely: run `package.ps1` locally and publish with
`gh release create v<version> dist/<zip> --generate-notes`. The **CI** workflow
still checks any release you publish by hand.

## What the other workflow does

`ci.yml` runs on every push and pull request (`scripts/check_release.py`: manifest,
versions, icon, tag) and on every release you publish by hand
(`scripts/check_release_asset.py`: the zip must contain the real mod and no other
DLL). Releases created by the Release workflow are checked inside that workflow
instead, because GitHub doesn't trigger other workflows from them.
