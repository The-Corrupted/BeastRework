#!/usr/bin/bash
set -euo pipefail

DEPLOYMENT_DIR="$HOME/.steam/steam/steamapps/common/Hollow Knight Silksong/BepInEx/plugins/BeastRework/"

dotnet build -c Debug
cp ./bin/Debug/netstandard2.1/BeastRework.dll "$DEPLOYMENT_DIR"
