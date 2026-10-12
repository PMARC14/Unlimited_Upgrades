# Unlimited Upgrades

[![GitHub Release](https://img.shields.io/github/v/release/PMARC14/Unlimited_Upgrades?style=flat-square)](https://github.com/PMARC14/Unlimited_Upgrades/releases)
[![Thunderstore](https://img.shields.io/badge/Thunderstore-Unlimited__Upgrades-blue?style=flat-square)](https://thunderstore.io/c/hotds/p/BaconGames/Unlimited_Upgrades/)

**Unlimited Upgrades** is a mod for *House of the Dying Sun* that removes the default 2-slot limit on player ships, allowing you to equip as many Field Upgrades (modifiers) as you want on any ship in your fleet!

---

## Features

- **No Upgrade Limits**: Equip 3, 5, 10, or all available upgrades simultaneously on your Interceptors, Destroyers, and Capital ships.
- **Checklist Toggle Selection**: Upgrades in the selection grid act as toggles—click an unequipped modifier to equip it, or click an equipped modifier to unequip/remove it. No tedious slot swapping required!
- **Smooth Slot Scrolling**: The ship overview hangar UI uses a responsive sliding window:
  - **Mouse Wheel**: Hover your cursor over the modifier slots and scroll up or down.
  - **Keyboard / Controller**: Navigate with **Arrow Keys / WASD / Controller D-pad**; the slots auto-scroll when reaching the edge.
  - **Quick Add Slot**: An empty slot is always appended to the end of the scroll list so you can easily add new upgrades.
- **Capital Ship Crash Safeguards**: Built-in compatibility filters automatically prevent invalid fighter-only modifiers from being attached to capital ships, ensuring zero mission crashes or glitches.
- **Save & Load Compatible**: Seamlessly integrates with the game's save system and the `GameSaves` mod.

---

## Requirements

- **House of the Dying Sun** (Steam)
- **[BepInExPack_HOTDS](https://thunderstore.io/c/hotds/p/BepInEx/BepInExPack_HOTDS/)** (v5.4.1900 or newer)

---

## Installation

### Automatic via r2modman / Thunderstore (Recommended)
1. Install and open **[r2modman](https://github.com/ebkr/r2modmanPlus)** or **Thunderstore Mod Manager**.
2. Select **House of the Dying Sun** as your game.
3. Search for **Unlimited_Upgrades** in the Online tab and click **Download**.
4. Launch the game using **Start Modded**.

### Manual Installation
1. Ensure **BepInEx** is installed in your *House of the Dying Sun* directory.
2. Download the latest release `.zip` from the [Releases](https://github.com/PMARC14/Unlimited_Upgrades/releases) page or Thunderstore.
3. Extract `AllUpgradesMod.dll` into your `BepInEx/plugins/` directory (e.g. `BepInEx/plugins/BaconGames-Unlimited_Upgrades/AllUpgradesMod.dll`).
4. Launch the game.

---

## Building from Source

This repository dynamically references your local game installation or BepInEx paths.

```bash
# Clone the repository
git clone https://github.com/PMARC14/Unlimited_Upgrades.git
cd Unlimited_Upgrades

# Build the Release DLL
dotnet build src/AllUpgradesMod.csproj -c Release

# Package into a Thunderstore-ready ZIP
powershell -ExecutionPolicy Bypass -File package.ps1
```

Maintainers: see [RELEASING.md](RELEASING.md) for how releases are built and checked.

---

## License

This project is open source and available under the [MIT License](LICENSE).
