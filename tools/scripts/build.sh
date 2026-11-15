#!/usr/bin/env bash
# Сборка solution. Запуск: bash tools/scripts/build.sh [Debug|Release]
set -euo pipefail

CONFIGURATION="${1:-Release}"
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

cd "$ROOT"
echo "Сборка EidolonSeamWars.sln (${CONFIGURATION})..."
dotnet build EidolonSeamWars.sln -c "$CONFIGURATION"
