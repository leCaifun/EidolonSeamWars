#!/usr/bin/env bash
# Проверка контента. Запуск:
#   bash tools/scripts/validate.sh
#   bash tools/scripts/validate.sh <path>
# Скрипт вызывает реальную команду Game.SimRunner validate и возвращает её код.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
PROJECT="${ROOT}/src/Game.SimRunner/Game.SimRunner.csproj"

echo "=== Проверка контента ==="
echo "Проект: ${PROJECT}"

if [[ $# -gt 0 ]]; then
    dotnet run --project "${PROJECT}" -- validate --content "$1"
else
    echo "Каталог контента: ${ROOT}/game/content/core"
    dotnet run --project "${PROJECT}" -- validate
fi
