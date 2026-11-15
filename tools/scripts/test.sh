#!/usr/bin/env bash
# Запуск тестов solution. Запуск: bash tools/scripts/test.sh [Debug|Release]
set -euo pipefail

CONFIGURATION="${1:-Release}"
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

cd "$ROOT"
echo "Тесты EidolonSeamWars.sln (${CONFIGURATION})..."
dotnet test EidolonSeamWars.sln -c "$CONFIGURATION" --logger 'console;verbosity=normal'
