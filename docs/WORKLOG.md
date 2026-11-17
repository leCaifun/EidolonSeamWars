# Журнал работ (WORKLOG)

| Параметр | Значение |
|---|---|
| Версия | 0.4.0 |
| Статус | Актуально |
| Дата | 2026-11-16 |
| Формат | W-XXXX; правила Git — ADR-029 |

## Оглавление

1. [Правила ведения](#1-правила-ведения)
2. [W-0001 · 2026-11-13 · M0 Bootstrap](#2-w-0001--2026-11-13--m0-bootstrap)
3. [W-0002 · 2026-11-13 · Этап 01b: стабилизация](#3-w-0002--2026-11-13--этап-01b-стабилизация)
4. [W-0003 · 2026-11-13 · Этап 02: Core-домен и экономика](#4-w-0003--2026-11-13--этап-02-core-домен-и-экономика)
5. [W-0004 · 2026-11-13 · Этап 03: Content Draft](#5-w-0004--2026-11-13--этап-03-content-draft)
6. [W-0005 · 2026-11-13 · M0 Localization Draft](#6-w-0005--2026-11-13--m0-localization-draft)
7. [W-0006 · 2026-11-15 · Этап 03: контентный конвейер](#7-w-0006--2026-11-15--этап-03-контентный-конвейер)
8. [W-0007 · 2026-11-16 · Этап 04: карта, A*, туман, экран карты](#8-w-0007--2026-11-16--этап-04-карта-a-туман-экран-карты)
9. [Связанные документы](#9-связанные-документы)

## 1. Правила ведения

Каждая задача добавляет запись с полями: ID, дата, этап ROADMAP, цель,
созданные/изменённые/удалённые файлы, тесты и результат, ручные проверки,
что не проверено, риски и техдолг, commit-message (Conventional Commits).
Правила Git — ADR-029. Записи W-0001…W-0006 созданы, пока Git был недоступен (ADR-019),
поэтому их commit-message предназначались для будущего репозитория.

## 2. W-0001 · 2026-11-13 · M0 Bootstrap

**Этап ROADMAP:** M0, часы 0–2 (этап 01 «Каркас проекта и документация»).

**Цель:** создать структуру проекта, документацию, .NET-каркас, базовые
инфраструктурные типы, git-ready файлы и правила логирования. Геймплей не
реализовывать.

### Созданные файлы

**Корень:**

- `README.md`, `LICENSE` (MIT), `CONTRIBUTING.md`
- `.gitignore`, `.gitattributes`, `.editorconfig`
- `Directory.Build.props` (версия 0.3.0, `net8.0`, C# 12)
- `opencode.json`
- `EidolonSeamWars.sln`

**Ядро и тесты:**

- `src/Game.Core/Game.Core.csproj`
- `src/Game.Core/Diagnostics/LogLevel.cs`
- `src/Game.Core/Diagnostics/LogEvent.cs`
- `src/Game.Core/Diagnostics/IGameLogger.cs`
- `src/Game.Core/Rng/SeededRandom.cs`
- `src/Game.Core/Rng/RngStreamKind.cs`
- `src/Game.Core/Rng/RngStreams.cs`
- `src/Game.Core/Events/IDomainEvent.cs`
- `src/Game.Core/Events/EventBus.cs`
- `src/Game.Core/Domain/Hex.cs`
- `src/Game.Core/Domain/HexOffsetLayout.cs`
- `src/Game.SimRunner/Game.SimRunner.csproj`
- `src/Game.SimRunner/Program.cs`
- `tests/Game.Core.Tests/Game.Core.Tests.csproj`
- `tests/Game.Core.Tests/HexTests.cs`
- `tests/Game.Core.Tests/SeededRandomTests.cs`
- `tests/Game.Core.Tests/RngStreamsTests.cs`
- `tests/Game.Core.Tests/EventBusTests.cs`

**Godot-слой:**

- `game/Directory.Build.props`
- `game/icon.svg` (оригинальный SVG: гекс, шов, якорь)
- `game/scripts/Boot/BootController.cs`
- `game/project.godot` — создан Godot 4.7.2 .NET, добавлен `config/version="0.3.0"`
- `game/EidolonSeamWars.csproj`, `game/EidolonSeamWars.sln` — созданы Godot
- `game/.gitignore`, `game/.gitattributes`, `game/scripts/Boot/BootController.cs.uid`

**Скрипты и CI:**

- `tools/scripts/build.ps1`, `build.sh`
- `tools/scripts/test.ps1`, `test.sh`
- `tools/scripts/run.ps1`, `run.sh`
- `tools/scripts/validate.ps1`, `validate.sh`
- `.github/ISSUE_TEMPLATE/bug.yml`
- `.github/ISSUE_TEMPLATE/feature.yml`
- `.github/ISSUE_TEMPLATE/balance.yml`
- `.github/PULL_REQUEST_TEMPLATE.md`
- `.github/workflows/ci.yml`
- `.opencode/command/check.md`
- `.opencode/command/docs.md`
- `.opencode/command/worklog.md`
- `.opencode/command/balance.md`

**Документация:**

- `docs/GDD.md`, `docs/ROADMAP.md`, `docs/DECISIONS.md`, `docs/CHANGELOG.md`
- `docs/WORKLOG.md`
- `docs/ARCHITECTURE.md`, `docs/TDD.md`, `docs/RUNBOOK.md`
- `docs/CONTENT_SCHEMA.md`, `docs/LOGGING.md`, `docs/TESTING.md`
- `docs/IP_SAFETY.md`, `docs/ASSET_LICENSES.md`, `docs/GLOSSARY.md`
- `docs/KNOWN_ISSUES.md`, `docs/UI_SPEC.md`, `docs/BALANCE.md`
- `docs/SAVE_FORMAT.md`, `docs/MODDING.md`, `docs/LOCALIZATION.md`
- `docs/AI.md`, `docs/ART_PIPELINE.md`, `docs/RELEASE_CHECKLIST.md`
- `docs/patchnotes/v0.3.0.md`
- `prompts/README.md`
- `prompts/02_core_domain.md` … `prompts/09_hardening.md`
- `prompts/templates/bugfix.md`, `feature.md`, `balance.md`

**Служебные каталоги:** `mods/`, `logs/`, `saves/`, `builds/` и пустые
подкаталоги `game/` засеяны `.gitkeep`.

### Изменённые файлы

- `docs/GDD.md` — добавлены шапка (версия, статус, дата) и оглавление;
  дизайн-содержание v0.3 не изменялось.
- `docs/ROADMAP.md` — добавлены шапка, оглавление и таблица соответствия
  промтов этапам M0; содержание M0–M5 сохранено.
- `docs/DECISIONS.md` — засеяны ADR-001…ADR-020, дополнительно ADR-021
  (границы MVP и этапность) и ADR-022 (округление и детерминизм RNG).
- `docs/CHANGELOG.md` — добавлен раздел `[Unreleased]`.
- `game/scripts/Boot/BootController.cs` — минимальный вывод версии проекта
  из `application/config/version` в лог Godot.
- `docs/RUNBOOK.md` — переписан: prerequisites, build/test/run/validate,
  ручное создание Godot .NET project, runtime-каталоги, troubleshooting.
- `tools/scripts/*.ps1` — сохранены в UTF-8 с BOM для совместимости
  с Windows PowerShell 5.1.
- `tools/scripts/run.ps1` — исправлена привязка аргументов: `version` больше
  не попадает в `-Configuration`; конфигурация задаётся `ESW_CONFIGURATION`.

### Удалённые файлы

- `src/Game.Core/Class1.cs` — шаблонная пустышка.
- `tests/Game.Core.Tests/UnitTest1.cs` — шаблонная пустышка.

### Тесты и результат

```
dotnet build EidolonSeamWars.sln -c Release
→ Сборка успешно завершена. Предупреждений: 0, Ошибок: 0.

dotnet test EidolonSeamWars.sln -c Release
→ Пройден! : не пройдено 0, пройдено 52, пропущено 0, всего 52.
```

Набор: `HexTests`, `SeededRandomTests`, `RngStreamsTests`, `EventBusTests`.

### Проверено руками

- Структура каталогов соответствует `prompts/01_bootstrap.md`.
- В `src/Game.Core` нет `using Godot`.
- `Game.Core` не содержит `float`/`double` и `System.Random`.
- `Hex`: соседи, дистанция, equality, round-trip offset `odd-r`/`even-r`.
- `SeededRandom`: воспроизводимость по сиду, отсутствие смещения.
- `RngStreams`: независимость стримов, стабильный префикс `esw.rng.v1.`.
- `EventBus`: доставка событий и логирование ошибок.
- `SimRunner help` / `version` работают; неизвестная команда даёт код 2.
- `validate.ps1` честно сообщает, что валидатор появится на этапе 03.
- `build.ps1`, `test.ps1`, `run.ps1`, `validate.ps1` запускаются через
  Windows PowerShell 5.1 (`powershell -File`); `run.ps1 version` возвращает 0,
  `run.ps1 simulate` — код 2 с честным сообщением.
- `dotnet build game/EidolonSeamWars.csproj -c Release` — успешно, 0 предупреждений
  и 0 ошибок; `BootController` компилируется.
- Godot 4.7.2 .NET `--headless --path game --editor --quit` — код 0, `icon.svg`
  импортирован; внутреннее предупреждение GodotTools о hot-reload в headless
  не относится к коду игры.
- `application/config/version="0.3.0"` добавлен в `project.godot`.

### Не проверено

- Главная сцена не задана, играбельных сцен нет: `BootController` не запускался
  в рантайме, только компилировался. Это ожидаемо до этапа 04.
- Bash-скрипты (`*.sh`) не запускались на Windows-машине разработки.
- CI-workflow не запускался: Git и GitHub недоступны.
- Валидатор контента и симуляция отсутствуют по плану (этапы 03 и 08).
- Целевая версия Godot 4.4 и локальная 4.7.2 не сверены на совместимость API
  за пределами этапа 01 (KI-008).

### Риски и техдолг

- Локальный Godot-проект создан 4.7.2 .NET, а ADR-001 фиксирует 4.4. До этапа 04
  нужно решить: зафиксировать 4.4 или оформить новый ADR на 4.7.2.
- `game/EidolonSeamWars.csproj` не входит в корневой solution: сборка Godot-слоя
  выполняется отдельной командой, CI её не проверяет.
- CI-заготовка не проверена в реальном runner.
- `TreatWarningsAsErrors=false`: постепенно ужесточить к этапу 09.

**Commit-message для будущего Git:**

```
chore(bootstrap): create M0 skeleton, core infrastructure and documentation

- add solution with Game.Core, Game.SimRunner and xUnit tests
- implement Hex, SplitMix64 seeded RNG streams, event bus and logging contracts
- seed ADR-001..022 and full project documentation
- add git-ready files, scripts, CI stub and OpenCode commands
- add Godot project structure, original icon, boot controller with version log

Refs: prompts/01_bootstrap.md, GDD v0.3, ADR-019
```

## 3. W-0002 · 2026-11-13 · Этап 01b: стабилизация

**Этап ROADMAP:** M0, часы 2–5 (промт `prompts/01b_stabilize.md`).

**Цель:** зафиксировать Godot 4.7.2 .NET, унифицировать toolchain и
`TargetFramework`, включить Godot-слой в корневой solution и CI, создать
стартовую сцену и headless-проверку. Геймплей не реализовывать.

### Созданные файлы

- `docs/TOOLCHAIN.md` — версии OS/Godot/.NET/PowerShell, команды проверки,
  правило обновления только через ADR.
- `game/scenes/boot/Boot.tscn` — стартовая сцена с `BootController`.
- `tools/scripts/run-headless.ps1`, `tools/scripts/run-headless.sh` — сборка,
  импорт и headless-запуск главной сцены.

### Изменённые файлы

- `docs/DECISIONS.md` — ADR-023 «Фиксация Godot 4.7.2 .NET»; ADR-001 переведён
  в статус «Заменён ADR-023». Номер 023 выбран потому, что 021 и 022 уже заняты
  (правило сквозной нумерации ADR).
- `AGENTS.md`, `README.md`, `prompts/00_mvp.md`, `prompts/01_bootstrap.md`,
  `docs/ARCHITECTURE.md`, `docs/TDD.md`, `docs/RUNBOOK.md`, `docs/GDD.md`
  (техническая строка движка), `docs/ASSET_LICENSES.md`,
  `docs/patchnotes/v0.3.0.md` — Godot 4.4 заменён на 4.7.2.
- `Directory.Build.props` — `TargetFramework=net8.0` вынесен на корень.
- `tests/Game.Core.Tests/Directory.Build.props`, `game/Directory.Build.props` —
  импорт корневых свойств.
- `src/Game.Core/Game.Core.csproj`, `src/Game.SimRunner/Game.SimRunner.csproj`,
  `tests/Game.Core.Tests/Game.Core.Tests.csproj` — убран дублирующий
  `TargetFramework`.
- `game/scripts/Boot/BootController.cs` — `Engine.GetVersionInfo()`, константа
  `ExpectedGodotVersion`, предупреждение без падения.
- `game/project.godot` — `application/run/main_scene="res://scenes/boot/Boot.tscn"`.
- `EidolonSeamWars.sln` — добавлен `game/EidolonSeamWars.csproj`
  в solution folder `game`.
- `.github/workflows/ci.yml` — комментарий и состав сборки приведены к root
  solution с Godot-слоем.
- `docs/KNOWN_ISSUES.md` — KI-008 закрыт; добавлены KI-009 и KI-010.
- `docs/CHANGELOG.md` — записи `[Unreleased]` по 01b.

### Тесты и результат

```
dotnet build EidolonSeamWars.sln -c Release
→ успешно, 0 предупреждений, 0 ошибок; собираются Core, SimRunner, Tests,
  Godot-слой (Godot.NET.Sdk/4.7.2).

dotnet test EidolonSeamWars.sln -c Release --no-build
→ 52 теста, 0 неудачных.

dotnet build game/EidolonSeamWars.csproj -c Release
→ успешно, 0 предупреждений, 0 ошибок.

Godot 4.7.2 --headless --path game --editor --quit
→ код 0.

Godot 4.7.2 --headless --path game --quit
→ код 0; [Boot] Eidolon: Seam Wars v0.3.0; [Boot] Godot 4.7.2 (.NET).
```

### Проверено руками

- `Engine.GetVersionInfo()` возвращает `4.7.2`; `ExpectedGodotVersion`
  совпадает, предупреждения нет.
- В `src/Game.Core` нет `using Godot`, `System.Random`, `TODO`, `float`/`double`.
- `game/Directory.Build.props` и `tests/.../Directory.Build.props` импортируют
  корневые свойства; `LangVersion`, `Version`, `TargetFramework` не потеряны.
- `run-headless.ps1` собирает, импортирует и запускает сцену одной командой.

### Не проверено

- CI-workflow в реальном GitHub runner не запускался (Git/GitHub недоступны).
- Bash-скрипты `run-headless.sh` не запускались на Windows.
- Поведение при реально другой версии Godot не воспроизводилось: ветка
  предупреждения проверена только чтением кода.

### Риски и техдолг

- `Godot.NET.Sdk/4.7.2` требует NuGet; офлайн-сборка невозможна без кэша.
- Godot может регенерировать `game/EidolonSeamWars.csproj`; условный `net9.0`
  для Android сохранён осознанно.
- `TreatWarningsAsErrors=false` остаётся до этапа 09.

**Commit-message для будущего Git:**

```
chore(toolchain): pin Godot 4.7.2 .NET and include game layer in solution

- add ADR-023 replacing ADR-001; sync docs, prompts and GDD engine line
- add docs/TOOLCHAIN.md with exact versions and verification commands
- centralize TargetFramework net8.0 in root Directory.Build.props
- add game project to root solution and CI build
- add Boot.tscn main scene, runtime Godot version check and headless scripts
- close KI-008, add KI-009/KI-010

Refs: prompts/01b_stabilize.md, ADR-023
```

## 4. W-0003 · 2026-11-13 · Этап 02: Core-домен и экономика

**Этап ROADMAP:** M0, часы 2–5 (промт `prompts/02_core_domain.md`).

**Цель:** реализовать чистый домен без Godot: целочисленную математику, семь
ресурсов, юнита и стак, армию с жёстким лидерством, героя и левелап, поселение
с захватом, дневной тик экономики, побег/плен/выкуп и тесты этих правил.
Геймплейный UI и сцены не делать.

### Созданные файлы

**Ядро:**

- `src/Game.Core/Numerics/IntegerMath.cs` — `MulDiv`, `MulDivToInt`, `CeilDiv`.
- `src/Game.Core/Domain/ResourceKind.cs`, `ResourcePool.cs`.
- `src/Game.Core/Domain/DamageType.cs`, `Resistances.cs`, `MovementType.cs`.
- `src/Game.Core/Domain/UnitDef.cs`, `VeterancyRules.cs`, `Stack.cs`, `Army.cs`.
- `src/Game.Core/Domain/HeroStatKind.cs`, `HeroStats.cs`, `HeroClassDefinition.cs`,
  `LevelUp.cs`, `Hero.cs`.
- `src/Game.Core/Domain/Settlement.cs`.
- `src/Game.Core/Economy/GrowthAccumulator.cs`, `HousingStock.cs`,
  `RecruitmentContract.cs`, `IRecruitmentContractSource.cs`, `RecruitmentBoard.cs`,
  `TimedEffect.cs`, `IDailyAiStep.cs`, `IAutosaveStep.cs`, `DailyTickState.cs`,
  `DailyTickResult.cs`, `DailyTick.cs`.
- `src/Game.Core/Heroes/EscapeResult.cs`, `RansomQuote.cs`, `CaptivityState.cs`,
  `EscapeRules.cs`.

**Тесты:**

- `tests/Game.Core.Tests/Support/TestLogger.cs`, `Support/TestContent.cs`.
- `tests/Game.Core.Tests/IntegerMathTests.cs`, `ResourcePoolTests.cs`,
  `UnitDefTests.cs`, `StackTests.cs`, `VeterancyTests.cs`,
  `ArmyLeadershipTests.cs`, `LevelUpTests.cs`, `DailyTickTests.cs`,
  `SettlementCaptureTests.cs`, `EscapeRansomTests.cs`.

### Изменённые файлы

- `docs/ARCHITECTURE.md` — фактическая раскладка пространств имён, доменная
  модель и список тестов этапа 02. `Hero*`/`LevelUp` находятся в `Domain`;
  `Heroes` содержит только побег, плен и выкуп, чтобы не было цикла зависимостей.
- `docs/TDD.md` — контракт `IntegerMath` и формулы этапа 02 (прирост, тик,
  лидерство, ветеранство, герой, побег/выкуп, захват).
- `docs/TESTING.md` — структура тестов, 212 тестов, матрица покрытия, CI
  собирает solution с Godot-слоем.
- `docs/BALANCE.md` — статус реализации формул; числа GDD v0.3 не менялись.
- `docs/CHANGELOG.md`, `docs/patchnotes/v0.3.0.md` — записи этапа 02.
- `docs/WORKLOG.md` — эта запись.

### Тесты и результат

```
dotnet build EidolonSeamWars.sln -c Release
→ успешно, 0 предупреждений, 0 ошибок.

dotnet test EidolonSeamWars.sln -c Release
→ Пройден! : не пройдено 0, пройдено 212, пропущено 0, всего 212.
```

Распределение: 52 инфраструктурных теста этапа 01 и 160 доменных тестов этапа 02.
Новые классы: `IntegerMathTests`, `ResourcePoolTests`, `UnitDefTests`,
`StackTests`, `VeterancyTests`, `ArmyLeadershipTests`, `LevelUpTests`,
`DailyTickTests`, `SettlementCaptureTests`, `EscapeRansomTests`.

### Проверено руками

- `src/Game.Core` не содержит `using Godot`, `System.Random`, `float`/`double`,
  `TODO`.
- Лидерство: перегруз невозможен, отказ не меняет армию и пишется в лог.
- Дневной тик: порядок «доход → прирост → жилища → контракты → эффекты → ИИ →
  автосохранение» проверен тестом с записывающими шагами.
- Прирост: дробный остаток сохраняется в промилле, кап `ceil(growth × 14)`
  достигается за 30 дней при `growth = 1000`.
- Захват поселения: здания и гарнизон очищаются, уровень сбрасывается до 1,
  при ошибке валидации состояние не меняется.
- Побег/выкуп: клампы 20…90%, граница промилле, запрет защитнику осады,
  формула `Σ(unitPrice × count) + heroLevel × 1000 + artifacts × 100`.
- Левелап: выбор характеристики детерминирован для одного сида, мягкий кап 10,
  запись в лог категории `Hero`.

### Не проверено

- Контент, реестр и валидатор отсутствуют (этап 03): реальные юниты, классы и
  цены не загружались.
- Карта, найм через UI, бой, сейвы и ИИ не реализованы по плану этапов 04–08.
- Кривая опыта и очки навыков героя не определены: реализован только прирост
  характеристик по весам класса.
- Баланс не проверен симуляцией: `SimRunner simulate` появится на этапе 08.
- Ветка предупреждения Godot о несовпадении версии проверена только чтением кода.

### Риски и техдолг

- `Hero.TryLevelUp` увеличивает уровень без порогов опыта: до появления контента
  вызывающий код обязан сам решать, когда выдавать левелап.
- `GrowthAccumulator` хранит целые юниты и дробный остаток раздельно; при
  сохранении состояния на этапе 07 потребуется сериализовать оба поля.
- `Settlement.Capture` реализует упрощённое правило MVP (ADR-013); M3 заменит
  его новым ADR.
- `IRecruitmentContractSource` и `IDailyAiStep` — интерфейсы без продакшн-реализаций
  до этапов 03/08.
- `TreatWarningsAsErrors=false` остаётся до этапа 09.

**Commit-message для будущего Git:**

```
feat(core): implement stage 02 domain, economy and deterministic rules

- add IntegerMath with half-away-from-zero rounding and CeilDiv
- add resources, UnitDef, Stack, Army, Hero, HeroClassDefinition, LevelUp, Settlement
- add escape, captivity and ransom rules
- add daily tick with growth, housing, contracts, timed effects and extension points
- add 160 domain tests; total suite 212 tests
- update architecture, TDD, testing, balance, changelog and patchnotes

Refs: prompts/02_core_domain.md, ADR-004, ADR-006, ADR-012, ADR-014, ADR-022
```

## 5. W-0004 · 2026-11-13 · Этап 03. Content Draft

**Этап ROADMAP:** M0, этап 03 (черновик контента).

**Цель:** вручную подготовить полный core-пак контента.

**Создано:** manifest, resources, 7 rules, 3 factions, 15 units, 3 hero classes, 3 heroes,
abilities, spells, buildings, settlements, map objects, ru/en локализация.

**Не проверено:** строгая валидация через ContentValidator, сборка контента в рантайме.

**Риски:** схемы черновые, этап 03 должен формализовать DTO и валидатор.

**Commit-message:** `feat(content): add full core content pack draft`

**Обновлено W-0006:** валидатор и загрузка реализованы; дубликат ID и
несоответствия `heroes/ash.json` / `settlements/root.json` исправлены.

## 6. W-0005 · 2026-11-13 · M0 Localization Draft

**Этап ROADMAP:** M0, локализация (черновик).

**Цель:** вручную подготовлены переводы всего контента из `ru` и `en` для расширенного списка языков.

**Создано:**
- языковые файлы локализации;
- список языков `game/content/core/localization/languages.json`.

**Изменено:**
- документация по локализации.

**Проверено:**
- структурная проверка ключей через `tools/scripts/check-localization.ps1`.

**Не проверено:**
- качество переводов носителями;
- покрытие шрифтов для `cu`, `rue`, `szl`, `csb`, `hsb`, `dsb`, `la`, `cnr`;
- отображение всех языков в реальном UI.

**Риски:**
- машинные или черновые переводы малых языков могут быть неточными;
- возможны проблемы шрифтового покрытия.

**Commit-message:** `feat(localization): add extended language drafts`

**Обновлено W-0006:** статусы языковых файлов приведены к `languages.json`
(ADR-026), `check-localization.ps1` смягчён для необязательных языков.

## 7. W-0006 · 2026-11-15 · Этап 03: контентный конвейер

**Этап ROADMAP:** M0, этап 03 «Content Pipeline» (`prompts/03_content_pipeline.md`).

**Цель:** превратить черновой core-пак W-0004/W-0005 в загружаемый, проверяемый
и детерминированный контент: DTO, загрузчик, merge, реестр, валидатор,
локализация с фолбэком, прогрессия героя по ADR-025, CLI `validate`,
интеграция загрузки в Godot, тесты и документация. Бой, карта, UI и ИИ —
вне этапа.

### Созданные файлы

**Ядро — конвейер (`src/Game.Core/Content`):**

- `ContentSchema.cs` — версия схемы и допустимые токены (`movement`, `damage`,
  типы объектов карты, ресурсы, статусы языков, список языков с обязательной
  проверкой шрифта).
- `IContentFileSystem.cs`, `ContentLoader.cs`, `ContentLoadResult.cs`,
  `ContentPack.cs`, `ContentEntry.cs`.
- `ContentMerge.cs`, `ContentMergeResult.cs`, `ContentChange.cs`,
  `ContentChangeKind.cs`, `ContentRegistry.cs`.
- `ContentValidator.cs`, `ContentValidationResult.cs`, `ContentDiagnostic.cs`,
  `ContentDiagnosticCollection.cs`, `ContentDiagnosticSeverity.cs`.
- `LocalizationLoader.cs`.
- `Internal/ContentJsonReader.cs`, `Internal/InternalContentFileDtos.cs`.

**Ядро — DTO (`src/Game.Core/Content/Dto`):**

- `ModManifestDto`, `FactionDto`, `FactionColorsDto`, `UnitDto`, `StatsDto`,
  `ResistancesDto`, `HeroClassDto`, `HeroDto`, `HeroArmySlotDto`,
  `HeroEquipmentDto`, `AbilityDto`, `AbilityEffectDto`, `SpellDto`,
  `BuildingDto`, `SettlementTemplateDto`, `SettlementLevelDto`,
  `SettlementUpgradeDto`, `RecruitmentDto`, `MapObjectDto`, `MapObjectOptionDto`,
  `ResourceDefinitionDto`, `BattleRulesDto`, `MoraleRulesDto`, `MoraleLevelDto`,
  `MoraleSourcesDto`, `EconomyRulesDto`, `EscapeRulesDto`, `RansomRulesDto`,
  `VeterancyRulesDto`, `VeterancyRankBonusesDto`, `ProgressionRulesDto`,
  `ProgressionFormulaDto`, `LanguagesManifestDto`, `LocalizationLanguageDto`,
  `LocalizationTableDto`.

**Ядро — локализация (`src/Game.Core/Content/Localization`):**

- `LocalizationManifest.cs`, `LanguageDefinition.cs`, `LocalizationTable.cs`,
  `LocalizationSet.cs`, `LocalizationResolver.cs`, `LocalizationStatus.cs`,
  `LocalizationStatusParser.cs`.

**Ядро — прогрессия героя:**

- `src/Game.Core/Domain/HeroProgression.cs`, `HeroProgressionResult.cs`.

**SimRunner:**

- `Program.cs` (команды `help`, `version`, `validate [--content PATH]`,
  коды выхода 0/1/2).
- `DiskContentFileSystem.cs`, `ContentPathResolver.cs`, `ConsoleGameLogger.cs`.

**Godot-адаптеры:**

- `game/scripts/Adapters/GodotContentFileSystem.cs`, `GodotGameLogger.cs`.

**Тесты:**

- `tests/Game.Core.Tests/ContentPipelineTests.cs`,
  `tests/Game.Core.Tests/ContentValidatorTests.cs`,
  `tests/Game.Core.Tests/HeroProgressionTests.cs`.
- `tests/Game.Core.Tests/Support/InMemoryContentFileSystem.cs`,
  `Support/FileContentFileSystem.cs`, `Support/TestPacks.cs`,
  `Support/RealContent.cs`.

**Скрипты:**

- `tools/scripts/validate.ps1`, `tools/scripts/validate.sh`,
  `tools/scripts/check-localization.ps1` (переписан под ADR-026).

### Изменённые файлы

- `src/Game.Core/Domain/Hero.cs` — уровень только через `HeroProgression`,
  внутренний `ApplyLevelUp`, логирование порогов.
- `src/Game.Core/Domain/LevelUp.cs` — убран статический мягкий кап; осталась
  взвешенная выдача `+1` характеристики.
- `tests/Game.Core.Tests/LevelUpTests.cs` — тесты приведены к новой модели.
- `game/scripts/Boot/BootController.cs` — загрузка `res://content/core`,
  merge, валидация, логирование счётчиков и каждого предупреждения загрузчика,
  merge и валидатора, `GD.PushError` + `Quit(1)` при ошибке.
- `src/Game.SimRunner/Game.SimRunner.csproj` — ссылка на Core и копирование
  контента в output.
- `tests/Game.Core.Tests/Directory.Build.props` — копирование
  `game/content/core/**/*.json` в output для `RealContent`.

### Удалённые / переименованные файлы контента

Причины структурные (нарушение стабильных ID или несоответствие GDD §5.4),
дизайн не менялся:

- удалён `game/content/core/units/brass_militia.json` — дубликат ID
  `core.unit.brass.militia` (уникальность ID обязательна).
- файлы `map_objects` переименованы так, чтобы имя файла соответствовало
  стабильному ID записи.
- `game/content/core/heroes/ash.json` переписан как отдельный герой
  `core.hero.ash.selha` (раньше файл описывал чужую фракцию).
- `game/content/core/settlements/root.json` переписан как
  `core.settlement.root` по GDD §5.4 (`maxLevel` 2, слоты Корневых Кланов).
- статусы 19 языковых файлов механически приведены к `languages.json`
  (ADR-026): 11 `draft`, 7 `experimental`, `la` — `complete`. Тексты не
  переводились и не правились.

### Тесты и результат

```
dotnet build EidolonSeamWars.sln -c Debug
→ успешно, 0 предупреждений, 0 ошибок.

dotnet build EidolonSeamWars.sln -c Release
→ успешно, 0 предупреждений, 0 ошибок.

dotnet test EidolonSeamWars.sln -c Debug
→ не пройдено 0, пройдено 262, пропущено 0, всего 262.

dotnet test EidolonSeamWars.sln -c Release
→ не пройдено 0, пройдено 262, пропущено 0, всего 262.

dotnet run --project src/Game.SimRunner -- validate
→ exit 0: 90 сущностей, 26 языков, ru/en по 238 ключей, 7 правил,
  0 ошибок, 2 предупреждения (CONTENT_RULE_UNKNOWN для rules/biomes.json
  и rules/map_generation.json).

powershell -File tools/scripts/validate.ps1
→ exit 0.

powershell -File tools/scripts/check-localization.ps1
→ exit 0 (пропуски необязательных языков — предупреждения).

Godot 4.7.2 .NET headless (run-headless.ps1)
→ exit 0; в логе версия движка, загрузка content/core, счётчики сущностей,
  языков и правил, отсутствие ошибок валидации.
```

Распределение 262 тестов: 52 инфраструктурных (этап 01), 160 доменных
(этап 02), 14 прогрессии (`HeroProgressionTests`), 36 контента
(`ContentPipelineTests` — 25, `ContentValidatorTests` — 11).

Покрытые сценарии контента: загрузка реального пака, обе формы файлов
(одно-сущностный и коллекция), merge и детерминированный порядок,
last-wins и журнал `ContentChange`, «седьмой юнит только через JSON»
(без правки кода), дубликат ID, неизвестная `schemaVersion`, отсутствующая
ссылка, превышение лидерства, паритет `ru`/`en`, лишний ключ, цепочка
фолбэка и возврат ключа при полном промахе.

### Проверено руками

- `src/Game.Core` не содержит `using Godot`, `System.Random`, `float`/`double`,
  `TODO`.
- Валидатор реального контента даёт 0 ошибок; два предупреждения относятся
  только к файлам правил этапа 04 и не теряют данные.
- `validate` возвращает 1 при искусственно испорченном контенте и 2 при
  неверном пути (проверено вручную и тестами `ContentValidatorTests`).
- Порядок merge стабилен: повторные прогоны дают одинаковый журнал изменений.
- Godot headless стартует с загруженным контентом и завершает работу кодом 0.
- Godot headless печатает оба предупреждения `CONTENT_RULE_UNKNOWN` отдельными
  строками — лог движка совпадает с выводом `SimRunner validate`.

### Не проверено

- Визуальный рендер и UI: экранов нет, локализация проверена только по ключам.
- Шрифтовое покрытие `la, cu, rue, szl, csb, hsb, dsb, cnr` (KI-011) — нужен
  реальный рендер шрифта на этапах 06–07.
- Качество переводов носителями (W-0005) не изменилось.
- Файлы `rules/biomes.json` и `rules/map_generation.json` не загружаются:
  DTO и валидация — этап 04 (KI-012).
- Баланс не проверен симуляцией: `SimRunner simulate` — этап 08.
- `validate.sh` не запускался в этой среде: WSL без `dotnet` в PATH (KI-005).

### Риски и техдолг

- `ContentLoader` намеренно пропускает неизвестные файлы правил с
  предупреждением; когда этап 04 добавит DTO, поведение нужно ужесточить
  до ошибки, иначе опечатка в имени правила пройдёт молча.
- Переопределение core-ID модом разрешено (last-wins). Без UI модов игрок
  не увидит, что число изменено; журнал `ContentChange` пока только в логе.
- `LocalizationResolver` возвращает ключ при полном промахе; UI обязан
  показывать `[key]`, а не пустоту (проверка — этап 06).
- Прогрессия героя хранит `xp` накопительно; при сохранении (этап 07) нужно
  сохранять и `level`, и `xp`, и `skillPoints` без пересчёта.
- `CONTENT_RULE_UNKNOWN` — единственный «мягкий» класс диагностики; при
  росте числа правил нужен явный список известных файлов.

**Commit-message для будущего Git:**

```
feat(content): implement stage 03 content pipeline and validator

- add content schema, DTOs, loader, deterministic merge, registry and validator
- add localization manifest, tables, resolver with fallback chain (26 languages)
- add HeroProgression with cumulative XP thresholds (ADR-025)
- add SimRunner validate CLI and Godot content boot adapters
- repair content IDs: duplicate militia, map_objects names, ash hero, root settlement
- add 36 content tests and 14 progression tests; total suite 262 tests
- update content schema, modding, localization, architecture, TDD, testing,
  balance, save format, known issues, GDD 9.6, changelog and patchnotes

Refs: prompts/03_content_pipeline.md, ADR-016, ADR-024, ADR-025, ADR-026
```

**Дополнение (тестовые карты этапа 04, добавлены вне записи):**

```
feat(content): add validated test map fixtures 8x8/16x16/48x48
```

## 8. W-0007 · 2026-11-16 · Этап 04: карта, A*, туман, экран карты

**Этап ROADMAP:** M0, часы 5–8, этап 04 «Adventure Map» (`prompts/04_adventure_map.md`).

**Цель:** привести документацию в порядок, проверить и исправить уже сделанное по этапу 04
и дописать недостающее (презентационный слой карты).

**Важно:** в среде этой записи не было `dotnet` и Godot. Ни сборка, ни тесты, ни запуск
движка не выполнялись. Всё ниже написано по чтению исходников и требует проверки.

### Найдено и исправлено в Core

- Туман: видимость сбрасывалась для всех партий фракции, кроме последней (ADR-033,
  `FogOfWar.ClearVisibility`/`AddVision`, `MapState.RefreshFog`, тест
  `FogOfWar_TwoPartiesOfOneFaction_KeepBothVisionAreas`).
- Движение: очки героя считались из плоского значения JSON, а не по GDD; множитель дороги
  не давал эффекта. Новая шкала ADR-034: местность не менее 2, дорога 1, очки героя
  `12 + Логистика`, отряда без героя 12. Удалены `heroMovementPointsPerDay` и
  `roadMovementCostMultiplierPercent`, обновлены биомы, валидатор, `GameMap`, адаптер,
  эталоны трёх тестовых карт и числа в тестах.
- Путь теперь обходит блокирующие объекты (`blockOccupiedTiles: true` в `MapState`).
- Устаревший фильтр `CONTENT_RULE_UNKNOWN` в тесте реального контента заменён проверкой
  отсутствия этой диагностики.

### Документация

- ADR-029…034 (Git, строгое имя правила, overridesCore, лицензирование, общий туман,
  шкала движения); ADR-024…028 приведены к единому формату, оглавление пересобрано.
- Политика Git приведена к ADR-029 в `AGENTS.md`, `CONTRIBUTING`, `README`, `ROADMAP`,
  `CHANGELOG`, `WORKLOG`, промтах и `.opencode`; разделы `AGENTS.md` перенумерованы.
- Опечатки и китайские вставки удалены; `LICENSE-CONTENT.md` объединён с `LICENSE-ASSETS.md`;
  правообладатель везде Saprinsky-Berozin.
- Обновлены `CONTENT_SCHEMA`, `TDD` (раздел 12), `BALANCE_MAP`, `TESTING`, `RUNBOOK`,
  `KNOWN_ISSUES`, `ASSET_LICENSES` (61 SVG), `test_scenarios/map_movement.md`, `GDD` §9.2.

### Godot-слой (написан без запуска движка)

- Новые: `scripts/Boot/GameSession.cs`, `scripts/Map/HexMetrics.cs`.
- Заполнены: `AdventureMapController`, `HexGridVisual`, `HexTileNode`, `FogOfWarVisual`,
  `MapObjectTokenController`, `PartyTokenController`, `CameraController`,
  `DayCounterController`, `EndDayButtonController`, `ResourcePanelController`.
- Сцены: `AdventureMap`, `HexGridVisual`, `HexTile`, `MapObjectToken`, `PartyToken`;
  пустая `FogTile.tscn` удалена (туман рисуется одним `_Draw`).
- `BootController` после загрузки контента переходит на `AdventureMap.tscn`.
- Ключи `ui.map.*` добавлены в `ru` и `en` (в остальных 24 языках их нет, KI-018).

### Как проверить (по порядку)

1. `dotnet build EidolonSeamWars.sln -c Release` и `dotnet test -c Release` — ожидается
   компиляция без предупреждений и зелёные тесты; обратить внимание на `MapTests`,
   `MapContentTests`, `ContentPipelineTests`, `LevelUpTests`.
2. `dotnet run --project src/Game.SimRunner -c Release -- validate` — ожидается exit 0,
   без `CONTENT_RULE_UNKNOWN`; 24 предупреждения `LOC_*_KEYS_MISSING` для языков, кроме ru/en.
3. Открыть `game/` в Godot 4.7.2 .NET, собрать C# (Build), запустить: Boot должен перейти
   на карту без ошибок в выводе.
4. На карте: виден игрок в стартовом гексе, вокруг открыт туман; клик по соседнему гексу
   двигает токен и уменьшает «Очки движения»; «Завершить день» увеличивает день и
   восстанавливает очки; WASD/колесо/средняя кнопка двигают камеру; клик по тёмному гексу —
   сообщение «не исследован».
5. После проверки обновить KI-004 и KI-014.

### Не проверено и риски

- Всё вышеперечисленное; особенно точность API Godot C# и формат вручную написанных `.tscn`.
- Эталоны `expectedCost` посчитаны отдельной моделью Дейкстры, совпавшей со старыми значениями.
- Бой по столкновению, награды объектов, ИИ партий, сохранение карты — не реализованы.
- Стартовые ресурсы панели — нули (экономика игрока не подключена к карте).

**Commit-message:**

```
feat(map): finish stage 04 map core fixes and add map screen

- fix fog union for multiple parties of one faction (ADR-033)
- rebalance movement: terrain >= 2, road 1, hero MP 12 + logistics (ADR-034)
- block guarded objects mid-route, drop legacy rule filter in tests
- add adventure map presentation layer and boot-to-map transition
- docs: ADR-029..034, unified ADR format, Git policy, licenses, asset registry

Refs: prompts/04_adventure_map.md, ADR-027, ADR-028, ADR-033, ADR-034
```

## 9. Связанные документы

- [AGENTS.md](../AGENTS.md) — правила проекта
- [docs/CHANGELOG.md](CHANGELOG.md) — версии
- [docs/ROADMAP.md](ROADMAP.md) — этапы
- [docs/DECISIONS.md](DECISIONS.md) — ADR-029 о Git и GitHub
- [docs/patchnotes/v0.3.0.md](patchnotes/v0.3.0.md) — патчноут версии
