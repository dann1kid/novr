# [Nuclear Option](https://store.steampowered.com/app/2168680/Nuclear_Option/) VR

NOVR is a reworked version of [UUVR](https://github.com/Raicuparta/uuvr) designed and optimized for Nuclear Option.

This tree is based on the [dann1kid/novr](https://github.com/dann1kid/novr) fork of [InfernoSuperNova/novr](https://github.com/InfernoSuperNova/novr), then patched for remaining VR gaps.

## 0.4.13

- **Black screen fix:** the VR UI overlay no longer submits a second headset view. Menus and the cursor are drawn by the game camera. Full-screen native menu backgrounds that clipped the HMD are gone.

## 0.4.12

- **Stock menus no longer follow the headset.** The server browser and other leftover game canvases are hidden while native VR UI is on, and the mouse cursor is driven in OpenXR even if the OS cursor is hidden.

## 0.4.11

- **Opaque menu plane:** stock world-space canvases are no longer left on the headset origin. Leaning back should not leave a wall in the middle of the view.

## 0.4.10

- **Black square fix:** the game's fade canvas is no longer a world-space quad through the HMD. It stays hidden except during a real scene fade.

## 0.4.9

- **Menu cursor restored:** the VR UI overlay camera draws in the headset again (0.4.3 stereo overlay). The 0.4.8 zoom hook stays disabled so the HMD should not go black.
- **Center occlusion:** the VR cursor stays at least 1 m from the eyes, and the fade canvas no longer sits on the HMD unless a fade is actually visible.

## 0.4.8

- **Black headset fix:** disable the XRPass zoom hook that could zero the projection matrix, and stop the VR UI camera from submitting its own empty stereo view.

## 0.4.7

- **VR entry restored:** a failed Harmony patch or a not-yet-ready headset no longer prevents OpenXR from starting. Yaw is not recentered when tracking is first acquired.

## 0.4.6

- **Tighter HUD layout:** pitch/climb ladder is a narrow, view-cone ladder instead of a wide wraparound ring. Weapon/status plates sit closer to the boresight.
- **HMD/radar contacts** use a smaller off-boresight ring so markers stay in central vision, and the HMD tape is attached to the headset instead of world origin.
- **Cockpit recenter:** yaw calibration uses seated heading, recenters when you spawn, and Home / VR CENTER works in the cockpit. Look forward during the countdown.
- **Proximity gradient:** nearer contacts and waypoints are larger, closer, and brighter; distant ones recede.

## 0.4.5

- **Stereoscopic VR zoom** using the game's `Zoom View` action. Configure speed, max zoom, and instant zoom-out in Settings → Controls.
- **Menus, map, spawn UI, and chat** now sit in front of the headset instead of at world origin, which also fixes VR cursor / highlight mismatch in stock menus.
- **Cockpit HUD plates** (weapons, HMD, target designator, status) follow the headset camera.
- **Encyclopedia, changelog, mission editor, and similar stock overlays** are shown in world space; click **VR UI ON** to return to the native VR menu.
- **VR CENTER** recenters both headset tracking and the native menu plane.
- Crash and leak fixes: APIBus scene-reload throw, infinite ejection retry, TargetCam FOV dictionary, headset calibration null refs.

## User Installation

### Recommended: GUI installer

1. Close Nuclear Option before installing or updating the mod.
2. Download the latest installer from the [NOVR releases page](https://github.com/dann1kid/novr/releases/latest):
    - **Windows:** `NOVR.Installer-Win.exe`
    - **Linux/Proton:** `NOVR.Installer-Linux`
3. Run the installer.
    - On Linux, you may need to make it executable first: `chmod +x NOVR.Installer-Linux`.
4. If Nuclear Option is not found automatically, choose the game folder manually.
    - The selected folder must contain `NuclearOption_Data/Managed`.
5. Click **Install**.
6. Launch Nuclear Option from Steam.

The installer can also update, repair, or uninstall NOVR after it is installed.

### What the installer does

The installer:

- Finds your Nuclear Option install.
- Installs BepInEx 5.x if it is missing.
- Downloads the latest NOVR release zip.
- Installs NOVR into:
    - `BepInEx/plugins/NOVR`
    - `BepInEx/patchers/NOVR`
- Writes the installed NOVR version to `BepInEx/plugins/NOVR/version.txt`.
- On Linux/Proton, attempts to configure the `winhttp` Wine override needed by BepInEx.

Use BepInEx 5.x only. Do not use BepInEx 6.x unless the project explicitly says it is supported.

### Manual zip install

Use this only if the installer does not work for your setup.

1. Close Nuclear Option.
2. Install [BepInEx 5.x](https://github.com/BepInEx/BepInEx/releases/latest) into the Nuclear Option game folder.
    - After installing BepInEx, this folder should exist: `Nuclear Option/BepInEx/core`.
3. Download `NOVR.zip` from the [latest NOVR release](https://github.com/dann1kid/novr/releases/latest).
4. Extract the contents of `NOVR.zip` into `Nuclear Option/BepInEx`.
5. Confirm these files exist:

    ```text
    Nuclear Option/BepInEx/plugins/NOVR/NOVR.dll
    Nuclear Option/BepInEx/patchers/NOVR/NOVR.Patcher.dll
    ```

6. Launch Nuclear Option from Steam.

`NOVR.zip` contains `plugins` and `patchers` folders. Extract it into the existing `BepInEx` folder, not directly into the game root.

### First launch behavior

On game startup, the NOVR BepInEx patcher copies required XR support files into `NuclearOption_Data`. These files may be overwritten every time the game starts.

If the game is already running while installing or rebuilding, Windows may prevent those files from being replaced. Close Nuclear Option before installing, updating, or building the mod.

### Linux/Proton notes

The installer tries to set the required `winhttp` override automatically. If BepInEx does not load under Proton, configure the game's Wine prefix manually so `winhttp` uses `native,builtin`.

See the [BepInEx running under Proton guide](https://docs.bepinex.dev/articles/advanced/proton_wine.html) for the manual setup.

## Building

These steps are for developers building NOVR from source.

1. Install a .NET/MSBuild toolchain that can build SDK-style .NET Framework 4.8 projects.
    - **Windows:** Visual Studio 2022 or Build Tools for Visual Studio 2022 with the **.NET Framework 4.8 targeting pack** installed.
    - **Linux:** the .NET SDK plus Mono/MSBuild and .NET Framework reference assemblies. Distro package names vary, but you usually want packages such as `dotnet-sdk`, `mono`, `msbuild`/`mono-msbuild`, and `mono-reference-assemblies`/`.NET Framework 4.8 reference assemblies`.
2. Have Nuclear Option installed at:
    - **Linux:** `~/.steam/steam/steamapps/common/Nuclear Option` OR `~/.steam/debian-installation/steamapps/common/Nuclear Option` OR `~/.local/share/Steam/steamapps/common/Nuclear Option`
    - **Windows:** `C:\Program Files (x86)\Steam\steamapps\common\Nuclear Option` OR `C:\Program Files\Steam\steamapps\common\Nuclear Option` OR `D:\SteamLibrary\steamapps\common\Nuclear Option`

    If none of these options work for you, set the `NUCLEAR_OPTION_GAME_DIR` environment variable or pass `/p:NuclearOptionGameDir="path\to\Nuclear Option"` when building.
3. Ensure BepInEx 5.x is installed inside the Nuclear Option directory.
4. Build the `Release` configuration from your IDE of choice. JetBrains Rider is tested; Visual Studio should work too. To build from the command line, run this from the project root:

    ```bash
    dotnet build NuclearOptionVirtualRealityMod.sln -c Release
    ```

    The build output is written under `build-output`. If Nuclear Option is found, the same files are also copied into its BepInEx folders. A Release build of `NOVR.Build` writes `dist/NOVR.zip`.

    If Nuclear Option is not installed, the plugin still compiles against **compile-only stubs** in `tools/stubs`. Those stub DLLs match the game assembly names so the plugin binds to the real game at runtime. **Never copy `lib/game-stubs` into the game.** The stub build runs automatically when the game folder is missing:

    ```bash
    dotnet build NOVR.Build/NOVR.Build.csproj -c Release
    ```

    Then extract `dist/NOVR.zip` into `Nuclear Option/BepInEx`. On Windows, with Steam at the default path and BepInEx already installed:

    ```powershell
    powershell -ExecutionPolicy Bypass -File scripts\install-to-game.ps1
    ```

    That copies into `C:\Program Files (x86)\Steam\steamapps\common\Nuclear Option\BepInEx`. Override the folder with `-GameDir` if needed. On Linux/Proton, use `scripts/install-to-game.sh` or set `NUCLEAR_OPTION_GAME_DIR`.

## Installer development

The GUI installer is built with Avalonia. Building it in `Release` automatically publishes single-file launchers for Linux and Windows into `dist/`:

```bash
dotnet build NOVR.Installer/NOVR.Installer.csproj -c Release
```

Outputs:

- `dist/NOVR.Installer-Linux`
- `dist/NOVR.Installer-Win.exe`

Building the full solution in `Release` also creates `dist/NOVR.zip`, ready to upload as the mod release asset.

## License

    Nuclear Option VR Mod
    Copyright (C) 2026 InfernoSuperNova (DeltaWing)

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.

# Original README below

> # Universal Unity VR
>
> [![Raicuparta's VR mods](https://raicuparta.com/img/badge.svg)](https://raicuparta.com)
>
> Use [Rai Pal](https://pal.raicuparta.com) to install this mod.
>
> ## License
>
>     Rai Pal
>     Copyright (C) 2024  Raicuparta
>
>     This program is free software: you can redistribute it and/or modify
>     it under the terms of the GNU General Public License as published by
>     the Free Software Foundation, either version 3 of the License, or
>     (at your option) any later version.
>
>     This program is distributed in the hope that it will be useful,
>     but WITHOUT ANY WARRANTY; without even the implied warranty of
>     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
>     GNU General Public License for more details.
>
>     You should have received a copy of the GNU General Public License
>     along with this program.  If not, see <https://www.gnu.org/licenses/>.
