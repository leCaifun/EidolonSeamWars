# RUNBOOK

| Параметр | Значение |
|---|---|
| Версия | 0.3.0 |
| Статус | Актуально для этапа 03 |
| Дата | 2026-11-15 |
| Источник истины | [docs/TOOLCHAIN.md](TOOLCHAIN.md), [docs/ARCHITECTURE.md](ARCHITECTURE.md) |

## Оглавление

1. [Предварительные требования](#1-предварительные-требования)
2. [Сборка](#2-сборка)
3. [Тесты](#3-тесты)
4. [Запуск SimRunner](#4-запуск-simrunner)
5. [Валидация контента](#5-валидация-контента)
6. [Godot .NET project: состояние и восстановление](#6-godot-net-project-состояние-и-восстановление)
7. [Headless-запуск и стартовая сцена](#7-headless-запуск-и-стартовая-сцена)
8. [Запуск игры из Godot](#8-запуск-игры-из-godot)
9. [Каталоги runtime](#9-каталоги-runtime)
10. [Версия проекта](#10-версия-проекта)
11. [Диагностика проблем](#11-диагностика-проблем)
12. [Связанные документы](#12-связанные-документы)

## 1. Предварительные требования

| Компонент | Версия | Обязательность |
|---|---|---|
| .NET SDK | 8.0.x (проверено 8.0.428) | Да |
| Godot | **4.7.2 .NET** (`mono`) | Для запуска игры |
| PowerShell | Windows PowerShell 5.1 или PowerShell 7+ | Для `.ps1`-скриптов |
| Bash | любой POSIX | Для `.sh`-скриптов |

Точные версии, пути и правило обновления — [docs/TOOLCHAIN.md](TOOLCHAIN.md).
`pwsh` не обязателен: скрипты проверены в Windows PowerShell 5.1 и сохранены
с UTF-8 BOM.

```powershell
dotnet --version
$PSVersionTable.PSVersion.ToString()
```

Godot должен быть именно .NET-сборкой: обычная версия не умеет C#.
Целевая версия зафиксирована ADR-023.

## 2. Сборка

```powershell
dotnet build EidolonSeamWars.sln -c Release
```

Или скриптом:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/scripts/build.ps1
```

На Unix:

```bash
bash tools/scripts/build.sh
```

Корневой solution содержит четыре проекта:

- `src/Game.Core` — classlib `net8.0`, без Godot;
- `src/Game.SimRunner` — console `net8.0`, ссылается на Core;
- `tests/Game.Core.Tests` — xUnit, ссылается на Core;
- `game/EidolonSeamWars.csproj` — Godot-слой (`Godot.NET.Sdk/4.7.2`),
  ссылается на Core через `game/Directory.Build.props`.

`TargetFramework` задан в корневом `Directory.Build.props`; Godot-csproj
дублирует `net8.0`, потому что редактор может его регенерировать.
Godot-слой собирается через NuGet-пакет SDK и не требует установленного
редактора. CI собирает тот же корневой solution.

## 3. Тесты

```powershell
dotnet test EidolonSeamWars.sln -c Release
```

Или:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/scripts/test.ps1
```

Ожидаемый результат этапа 01b: 52 теста, 0 неудачных. После этапа 02 число
тестов вырастет; актуальная матрица — [docs/TESTING.md](TESTING.md).

## 4. Запуск SimRunner

```powershell
dotnet run --project src/Game.SimRunner -c Release -- help
dotnet run --project src/Game.SimRunner -c Release -- version
```

Скрипт-обёртка:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/scripts/run.ps1 version
```

Конфигурация задаётся переменной окружения `ESW_CONFIGURATION`
(по умолчанию `Release`), аргументы передаются SimRunner.

На этапе 03 поддержаны `help`, `version` и `validate`. Неизвестная команда
завершается кодом 2; команды `smoke` и `simulate` не притворяются работающими
и появятся на этапах 05/08.

## 5. Валидация контента

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/scripts/validate.ps1
```

На Unix:

```bash
bash tools/scripts/validate.sh
```

Прямой вызов с указанием каталога контента:

```powershell
dotnet run --project src/Game.SimRunner -c Release -- validate --content game/content/core
```

Валидатор этапа 03 читает `manifest.json`, все категории контента и
`localization/languages.json`, выполняет merge, проверяет схему, ссылки,
лидерство, диапазоны и локализацию. Коды выхода: `0` — контент валиден
(предупреждения допустимы), `1` — есть ошибки, `2` — ошибка запуска
(например, каталог не найден).

Ожидаемый результат на текущем контенте: 90 сущностей, 26 языков,
`ru`/`en` по 238 ключей, 7 загруженных правил, 0 ошибок и 2 предупреждения
`CONTENT_RULE_UNKNOWN` для `rules/biomes.json` и `rules/map_generation.json`
(их загрузит этап 04).

## 6. Godot .NET project: состояние и восстановление

Проект создан Godot 4.7.2 .NET: `game/project.godot`,
`game/EidolonSeamWars.csproj`, `game/EidolonSeamWars.sln`,
`game/scenes/boot/Boot.tscn` и `game/scripts/Boot/BootController.cs`
существуют и проверены.

### 6.1. Импорт существующего каталога

1. Запустите Godot 4.7.2 .NET.
2. В Project Manager выберите **Import**.
3. Укажите путь к каталогу `game/`.
4. Нажмите **Import & Edit**. Godot обновит `.godot/` и импорт ассетов.

Не создавайте проект заново: это потеряет подготовленную структуру.

### 6.2. Настройки проекта

| Настройка | Значение |
|---|---|
| `application/config/name` | `EidolonSeamWars` |
| `application/config/version` | `0.3.0` |
| `application/config/features` | Godot 4.7, C#, Forward Plus |
| `application/run/main_scene` | `res://scenes/boot/Boot.tscn` |
| `dotnet/project/assembly_name` | `EidolonSeamWars` |
| `display/window/stretch/mode` | `canvas_items` |

Версия в `project.godot` должна совпадать с `Directory.Build.props` и
[docs/CHANGELOG.md](CHANGELOG.md). Разрешение окна и остальные параметры UI
фиксируются на этапе 06 по [docs/UI_SPEC.md](UI_SPEC.md).

### 6.3. C#-сборка внутри Godot

1. Убедитесь, что в правом верхнем углу редактора отображается кнопка **Build**.
2. Нажмите **Build**, если Godot просит пересобрать C#.
3. `game/Directory.Build.props` импортирует корневые свойства и добавляет
   `ProjectReference` на `../src/Game.Core/Game.Core.csproj`.

Сборка из терминала:

```powershell
dotnet build game/EidolonSeamWars.csproj -c Release
```

Godot создаёт собственный `game/EidolonSeamWars.sln`; корневой solution —
основной и включает Godot-слой.

## 7. Headless-запуск и стартовая сцена

Главная сцена — `game/scenes/boot/Boot.tscn` с узлом `Boot` и скриптом
`BootController`. В `_Ready()` она печатает версию проекта и фактическую
версию Godot, а при расхождении с ожидаемой `4.7.2` пишет предупреждение,
но не падает.

Полная проверка одной командой:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/scripts/run-headless.ps1 `
  -GodotPath 'C:\path\to\Godot_v4.7.2-stable_mono_win64_console.exe'
```

На Unix:

```bash
ESW_GODOT=/path/to/godot bash tools/scripts/run-headless.sh
```

Скрипт собирает Godot-слой, затем выполняет импорт и запуск:

```powershell
& $Godot --headless --path game --editor --quit
& $Godot --headless --path game --quit
```

Ожидаемый вывод запуска:

```
[Boot] Eidolon: Seam Wars v0.3.0
[Boot] Godot 4.7.2 (.NET)
```

Код возврата 0 означает успех. Предупреждение GodotTools о таймере hot-reload
в headless-режиме не относится к коду игры.

## 8. Запуск игры из Godot

```powershell
godot --path game
```

Если `godot` не в `PATH`, укажите полный путь к .NET-сборке Godot, либо
запустите редактор и нажмите **F5**. На этапе 03 играбельных экранов ещё нет:
стартовая сцена логирует версии и загружает контент. Полный цикл
меню→карта→бой→осада→сейв→лоад появляется по этапам
[docs/ROADMAP.md](ROADMAP.md).

## 9. Каталоги runtime

| Каталог | Назначение | Содержимое в репозитории |
|---|---|---|
| `logs/` | структурированные логи, 5 файлов по 10 МБ | пусто, `.gitkeep` |
| `saves/` | безлимитные слоты сохранений | пусто, `.gitkeep` |
| `builds/` | локальные сборки и экспорт | пусто, `.gitkeep` |
| `mods/` | data-паки `mods/<modId>/manifest.json` | пусто, `.gitkeep` |

Содержимое этих каталогов не коммитится (см. `.gitignore`). Формат логов —
[docs/LOGGING.md](LOGGING.md), формат сейвов — [docs/SAVE_FORMAT.md](SAVE_FORMAT.md).

## 10. Версия проекта

Версия `0.3.0` задаётся один раз для .NET — в корневом `Directory.Build.props`.
`Game.SimRunner` читает её из `AssemblyInformationalVersionAttribute` и печатает
командой `version`. В Godot версия живёт в `application/config/version` и
читается `BootController`. При смене версии обновите оба места,
[docs/CHANGELOG.md](CHANGELOG.md) и создайте `docs/patchnotes/vX.Y.Z.md`.

## 11. Диагностика проблем

| Симптом | Причина | Решение |
|---|---|---|
| `dotnet build` не находит `Game.Core` | запуск не из корня | перейдите в корень репозитория |
| Godot не показывает кнопку **Build** | установлена не-.NET версия Godot | установите .NET-сборку Godot (`mono`) |
| `[Boot] Ожидался Godot 4.7.2...` | другая версия редактора | см. [docs/TOOLCHAIN.md](TOOLCHAIN.md); версия обновляется только ADR |
| `BootController` не компилируется в Godot | не собран `game/EidolonSeamWars.csproj` | нажмите **Build** или запустите `run-headless.ps1` |
| `validate.ps1` возвращает код 1 | в контенте есть ошибки | смотрите строки `ОШИБКА` в выводе: файл, поле, ID |
| `validate.ps1` возвращает код 2 | не найден каталог контента | передайте `--content <path>` или запустите из корня репозитория |
| `CONTENT_RULE_UNKNOWN` для `biomes`/`map_generation` | файлы правил этапа 04 | предупреждение, не ошибка; загрузка появится на этапе 04 |
| `run.ps1 simulate` возвращает код 2 | симуляция ещё не реализована | этап 05/08 |
| Тесты падают с `using Godot` в Core | нарушен ADR-002 | уберите Godot-типы из `src/Game.Core` |
| Кириллица в консоли PowerShell выглядит неверно | кодировка консоли | `chcp 65001` или `[Console]::OutputEncoding = [Text.Encoding]::UTF8` |

## 12. Связанные документы

- [docs/TOOLCHAIN.md](TOOLCHAIN.md) — версии и проверки
- [docs/GDD.md](GDD.md) — дизайн-источник истины
- [docs/ARCHITECTURE.md](ARCHITECTURE.md) — слои и зависимости
- [docs/TESTING.md](TESTING.md) — тесты и критерии
- [docs/LOGGING.md](LOGGING.md) — логирование
- [docs/SAVE_FORMAT.md](SAVE_FORMAT.md) — сохранения
- [docs/RELEASE_CHECKLIST.md](RELEASE_CHECKLIST.md) — релизный чек-лист
- [CONTRIBUTING.md](../CONTRIBUTING.md) — правила вклада
