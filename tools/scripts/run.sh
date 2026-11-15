#!/usr/bin/env bash
# Запуск SimRunner. Пример: bash tools/scripts/run.sh version
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
CONFIGURATION="${CONFIGURATION:-Release}"

cd "$ROOT"
dotnet run --project src/Game.SimRunner/Game.SimRunner.csproj -c "$CONFIGURATION" -- "$@"
