#!/usr/bin/env bash
# Headless-проверка Godot-проекта: сборка C#, импорт и запуск главной сцены.
# Запуск: bash tools/scripts/run-headless.sh
# Путь к Godot .NET: переменная окружения ESW_GODOT (по умолчанию godot).
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
project="$root/game"
godot="${ESW_GODOT:-godot}"
configuration="${ESW_CONFIGURATION:-Release}"

echo "Сборка Godot-слоя ($configuration)..."
dotnet build "$project/EidolonSeamWars.csproj" -c "$configuration"

echo "Импорт проекта без редактора..."
"$godot" --headless --path "$project" --editor --quit

echo "Запуск главной сцены в headless-режиме..."
"$godot" --headless --path "$project" --quit

echo "Headless-проверка завершена успешно."
