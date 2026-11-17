# Эйдолон: Войны Шва / Eidolon: Seam Wars

Пошаговая стратегия + RPG: глобальная карта и бой на гексах, экономика по дням,
герои вне поля боя, армия как билд. Мир, сшитый из осколков погибших реальностей.

- Дизайн: [docs/GDD.md](docs/GDD.md) (v0.3.0, источник истины)
- Архитектура: [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md), [docs/TDD.md](docs/TDD.md)
- Дорожная карта: [docs/ROADMAP.md](docs/ROADMAP.md)
- Как запускать: [docs/RUNBOOK.md](docs/RUNBOOK.md)
- Правила работы агентов: [AGENTS.md](AGENTS.md)

## Статус

**M0, этап 03 «Контентный конвейер».** Реализованы инфраструктура ядра
(Hex, seeded RNG, диагностика, шина событий), домен (ресурсы, юниты, стаки,
армия, герой, поселение, дневной тик, побег/выкуп) и загрузка контента:
DTO, загрузчик, merge, реестр, валидатор, локализация и прогрессия героя.
Карта, бой и UI — этапы 04+ по [docs/ROADMAP.md](docs/ROADMAP.md).

Контент проверяется командой `validate` (см. [docs/RUNBOOK.md](docs/RUNBOOK.md) §5):
90 сущностей, 26 языков, `ru`/`en` по 238 ключей, 0 ошибок.

Godot-проект в `game/` входит в корневой solution и собирается через
`Godot.NET.Sdk/4.7.2` из NuGet. Целевая версия по ADR-023 — Godot 4.7.2 .NET.
Стартовая сцена `game/scenes/boot/Boot.tscn` логирует версии и загружает
`res://content/core`; проверка — `tools/scripts/run-headless.ps1`.
Подробности — [docs/TOOLCHAIN.md](docs/TOOLCHAIN.md) и
[docs/RUNBOOK.md](docs/RUNBOOK.md).

## Стек

- Godot 4.7.2 .NET (C#), .NET 8
- xUnit
- JSON-контент со стабильными строковыми ID
- `src/Game.Core` — чистое ядро без `using Godot`

## Структура

```
src/Game.Core/          чистое ядро (домен, RNG, события, диагностика)
src/Game.SimRunner/     headless CLI (валидатор и симуляции — этапы 03/08)
tests/Game.Core.Tests/  xUnit-тесты ядра
game/                   Godot-слой: сцены, скрипты, ассеты, JSON-контент
docs/                   проектная документация
prompts/                пошаговые задания для агентов
tools/scripts/          build / test / run / validate / run-headless (PowerShell и bash)
mods/ logs/ saves/ builds/  runtime- и мод-каталоги
```

## Сборка и тесты

```powershell
dotnet build EidolonSeamWars.sln -c Release
dotnet test EidolonSeamWars.sln -c Release
```

Или скриптами:

```powershell
pwsh tools/scripts/build.ps1
pwsh tools/scripts/test.ps1
pwsh tools/scripts/run.ps1 version
pwsh tools/scripts/validate.ps1
```

Если `pwsh` не установлен, используйте Windows PowerShell:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/scripts/build.ps1
```

На Unix:

```bash
bash tools/scripts/build.sh
bash tools/scripts/test.sh
bash tools/scripts/run.sh version
bash tools/scripts/validate.sh
```

Валидатор контента реализован на этапе 03:

```powershell
dotnet run --project src/Game.SimRunner -c Release -- validate
```

Код выхода `0` — контент валиден, `1` — есть ошибки, `2` — ошибка запуска.
Подробности — [docs/RUNBOOK.md](docs/RUNBOOK.md) §5.

## Принципы

1. `Game.Core` не ссылается на Godot.
2. Контент — только JSON, без хардкода юнитов и фракций.
3. Бой и экономика — целочисленная математика per-mille.
4. RNG — только seeded-стримы `map` / `battle` / `loot` / `levelup`.
5. Изменения состояния — команды, лог команд = реплей.
6. Сейвы — версия, миграции, безлимитные слоты.
7. Локализация — ключи, `ru.json` + `en.json`.
8. Никакого автобоя в игровых режимах.
9. Только оригинальные имена и ассеты (см. [docs/IP_SAFETY.md](docs/IP_SAFETY.md)).

## Git

Репозиторий публичный на GitHub. Правила работы с Git — [AGENTS.md](AGENTS.md) §5 и ADR-029.
История решений и проверок — [docs/WORKLOG.md](docs/WORKLOG.md), версии — [docs/CHANGELOG.md](docs/CHANGELOG.md).

## License

- Source code: Apache-2.0. See [LICENSE](LICENSE).
- Game assets, content, story, names, logos: All rights reserved. See [LICENSE-ASSETS.md](LICENSE-ASSETS.md).
- Trademarks: see [TRADEMARKS.md](TRADEMARKS.md).
- Modding: allowed. See [MODDING_POLICY.md](MODDING_POLICY.md).

This is a commercial game project with public source code.
You may study and fork the code, but you may not reuse official assets or branding.
