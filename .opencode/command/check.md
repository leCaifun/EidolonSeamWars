---
description: Полная проверка проекта (build, test, validate, инварианты)
---

Выполни проверку проекта «Эйдолон: Войны Шва» по AGENTS.md §3 и §8.
Аргументы (необязательно): $ARGUMENTS

1. Прочитай AGENTS.md и последнюю запись docs/WORKLOG.md.
2. Запусти `dotnet build EidolonSeamWars.sln -c Release`.
3. Запусти `dotnet test EidolonSeamWars.sln -c Release`.
4. Запусти `pwsh tools/scripts/validate.ps1` (или `bash tools/scripts/validate.sh`).
5. Проверь, что в `src/Game.Core` нет `using Godot`.
6. Проверь, что изменённые правила отражены в тестах и документах.
7. Дай отчёт: что проверено, результаты команд, что не проверено, риски.

Ничего не исправляй молча: сначала перечисли найденные проблемы.
Git-команды не выполнять.
