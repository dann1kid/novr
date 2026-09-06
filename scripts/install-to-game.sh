#!/usr/bin/env bash
# Extract dist/NOVR.zip into an existing Nuclear Option BepInEx folder.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
ZIP="${1:-$ROOT/dist/NOVR.zip}"

candidates=()
if [[ -n "${NUCLEAR_OPTION_GAME_DIR:-}" ]]; then
  candidates+=("$NUCLEAR_OPTION_GAME_DIR")
fi
candidates+=(
  "$HOME/Locations/NuclearOption/Install"
  "$HOME/.steam/steam/steamapps/common/Nuclear Option"
  "$HOME/.steam/debian-installation/steamapps/common/Nuclear Option"
  "$HOME/.local/share/Steam/steamapps/common/Nuclear Option"
  "/mnt/c/Program Files (x86)/Steam/steamapps/common/Nuclear Option"
  "/mnt/c/Program Files/Steam/steamapps/common/Nuclear Option"
  "/c/Program Files (x86)/Steam/steamapps/common/Nuclear Option"
  "/c/Program Files/Steam/steamapps/common/Nuclear Option"
  "C:/Program Files (x86)/Steam/steamapps/common/Nuclear Option"
  "C:/Program Files/Steam/steamapps/common/Nuclear Option"
)

GAME=""
for candidate in "${candidates[@]}"; do
  if [[ -d "$candidate/NuclearOption_Data/Managed" ]]; then
    GAME="$candidate"
    break
  fi
done

if [[ ! -f "$ZIP" ]]; then
  echo "NOVR.zip not found at $ZIP" >&2
  echo "Build first: dotnet build NOVR.Build/NOVR.Build.csproj -c Release" >&2
  exit 1
fi

if [[ -z "$GAME" ]]; then
  echo "Nuclear Option was not found on this machine." >&2
  echo "Set NUCLEAR_OPTION_GAME_DIR to the folder that contains NuclearOption_Data/Managed," >&2
  echo "then run: $0" >&2
  echo "Or extract $ZIP into your existing BepInEx folder." >&2
  exit 2
fi

if [[ ! -d "$GAME/BepInEx" ]]; then
  echo "Found Nuclear Option at: $GAME" >&2
  echo "BepInEx is not installed there yet. Install BepInEx 5.x first." >&2
  exit 3
fi

echo "Installing NOVR into $GAME/BepInEx"
unzip -o "$ZIP" -d "$GAME/BepInEx"
echo "Installed:"
echo "  $GAME/BepInEx/plugins/NOVR/NOVR.dll"
echo "  $GAME/BepInEx/patchers/NOVR/NOVR.Patcher.dll"
echo "Close Nuclear Option before launching from Steam."
