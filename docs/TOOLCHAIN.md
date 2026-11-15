# Toolchain (инструменты и версии)

| Параметр | Значение |
|---|---|
| Версия | 0.3.0 |
| Статус | Актуально |
| Дата | 2026-11-13 |
| Источник истины | [docs/DECISIONS.md](DECISIONS.md) ADR-023 |

## Оглавление

1. [Назначение](#1-назначение)
2. [Зафиксированные версии](#2-зафиксированные-версии)
3. [TargetFramework](#3-targetframework)
4. [Команды проверки](#4-команды-проверки)
5. [Переменные окружения](#5-переменные-окружения)
6. [Правило обновления версии](#6-правило-обновления-версии)
7. [Связанные документы](#7-связанные-документы)

## 1. Назначение

Документ фиксирует фактическую среду, в которой собирается, тестируется и
запускается проект. Расхождение версий без ADR запрещено: агент и CI должны
работать с одним и тем же Godot и .NET.

## 2. Зафиксированные версии

| Компонент | Версия | Где проверено |
|---|---|---|
| OS (локально) | Windows 11, x64 | Этап 01b |
| OS (CI) | `ubuntu-latest` | `.github/workflows/ci.yml` |
| Godot | **4.7.2 .NET** (`4.7.2.stable.mono.official.ed1daf0bf`) | Импорт, сборка C#, headless-запуск |
| Godot .NET SDK | `Godot.NET.Sdk/4.7.2` | `game/EidolonSeamWars.csproj` |
| .NET SDK | **8.0.428** | `dotnet --version` |
| Target framework | `net8.0` | Все проекты solution |
| C# | 12 (`LangVersion=12`) | `Directory.Build.props` |
| PowerShell | **Windows PowerShell 5.1.26100.9818** | `$PSVersionTable.PSVersion` |
| Bash | POSIX (Linux/macOS/CI) | `.sh`-скрипты |
| xUnit | 2.5.3 | `tests/Game.Core.Tests` |
| Microsoft.NET.Test.Sdk | 17.8.0 | `tests/Game.Core.Tests` |

Локальный путь Godot .NET (эталон для проверок 01b):

```
C:\Users\Сильвестр\Desktop\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe
```

Путь не является частью репозитория. На другой машине используйте свой путь
или переменную `ESW_GODOT`.

`pwsh` (PowerShell 7+) в локальной среде отсутствует. Все `.ps1`-скрипты
сохранены с UTF-8 BOM и проверены в Windows PowerShell 5.1. Для запуска:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/scripts/build.ps1
```

Godot обязан быть .NET-сборкой (`mono`): обычная версия не собирает C#.

## 3. TargetFramework

| Проект | TargetFramework | Примечание |
|---|---|---|
| `src/Game.Core` | `net8.0` | Из корневого `Directory.Build.props` |
| `src/Game.SimRunner` | `net8.0` | Из корневого `Directory.Build.props` |
| `tests/Game.Core.Tests` | `net8.0` | Импортирует корневой props |
| `game/EidolonSeamWars.csproj` | `net8.0`, для Android `net9.0` | Условная строка — требование Godot SDK, на desktop не влияет |

`tests/Game.Core.Tests/Directory.Build.props` и `game/Directory.Build.props`
явно импортируют корневой `Directory.Build.props`, поэтому версия, `LangVersion`
и `Nullable` задаются в одном месте. Godot-csproj может регенерироваться
редактором; его `TargetFramework` продублирован осознанно.

## 4. Команды проверки

Версия .NET:

```powershell
dotnet --version
```

Ожидается `8.0.428` или другой `8.0.x`.

Версия PowerShell:

```powershell
$PSVersionTable.PSVersion.ToString()
```

Версия Godot:

```powershell
& 'C:\path\to\Godot_v4.7.2-stable_mono_win64_console.exe' --version
```

Ожидается строка, начинающаяся с `4.7.2.stable.mono`.

Версия Godot из рантайма проекта:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/scripts/run-headless.ps1 `
  -GodotPath 'C:\path\to\Godot_v4.7.2-stable_mono_win64_console.exe'
```

В логе должны появиться строки:

```
[Boot] Eidolon: Seam Wars v0.3.0
[Boot] Godot 4.7.2 (.NET)
```

`BootController` сравнивает фактическую версию с константой
`ExpectedGodotVersion = "4.7.2"`. При несовпадении пишется `GD.PushWarning`,
игра продолжает работу.

Полная проверка:

```powershell
dotnet build EidolonSeamWars.sln -c Release
dotnet test EidolonSeamWars.sln -c Release --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File tools/scripts/validate.ps1
```

## 5. Переменные окружения

| Переменная | Значение по умолчанию | Назначение |
|---|---|---|
| `ESW_CONFIGURATION` | `Release` | Конфигурация для `run.ps1` и `run-headless.*` |
| `ESW_GODOT` | `godot` | Путь к исполняемому файлу Godot .NET |

## 6. Правило обновления версии

1. Обновление Godot, .NET SDK или `TargetFramework` — только через новый ADR
   в [docs/DECISIONS.md](DECISIONS.md). ADR-023 фиксирует текущую версию.
2. После обновления синхронно правятся: `AGENTS.md`, `docs/TOOLCHAIN.md`,
   `docs/RUNBOOK.md`, `docs/ARCHITECTURE.md`, `docs/TDD.md`, `prompts/*.md`,
   `.github/workflows/ci.yml`, `game/EidolonSeamWars.csproj`,
   `BootController.ExpectedGodotVersion`.
3. Проверки из §4 выполняются заново; результаты фиксируются в
   [docs/WORKLOG.md](WORKLOG.md).
4. Молчаливое расхождение версий запрещено. Если фактическая версия отличается
   от целевой, это либо ошибка среды, либо повод для нового ADR.

## 7. Связанные документы

- [docs/DECISIONS.md](DECISIONS.md) — ADR-023
- [docs/RUNBOOK.md](RUNBOOK.md) — сборка, тесты, запуск
- [docs/ARCHITECTURE.md](ARCHITECTURE.md) — слои и зависимости
- [docs/TDD.md](TDD.md) — технические детали
- [AGENTS.md](../AGENTS.md) — правила проекта
