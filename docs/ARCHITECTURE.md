# Архитектура

| Параметр | Значение |
|---|---|
| Версия | 0.3.0 |
| Статус | Актуально для этапа 03 (контентный конвейер) |
| Дата | 2026-11-15 |
| Источник истины | [docs/GDD.md](GDD.md) §14 |

## Оглавление

1. [Цели и ограничения](#1-цели-и-ограничения)
2. [Слои системы](#2-слои-системы)
3. [Структура solution](#3-структура-solution)
4. [Правила зависимостей](#4-правила-зависимостей)
5. [Доменная модель](#5-доменная-модель)
6. [Команды и события](#6-команды-и-события)
7. [Детерминизм и RNG](#7-детерминизм-и-rng)
8. [Контентный конвейер](#8-контентный-конвейер)
9. [Граница presentation](#9-граница-presentation)
10. [Логирование](#10-логирование)
11. [Сохранения](#11-сохранения)
12. [Моддинг](#12-моддинг)
13. [Тестирование](#13-тестирование)
14. [Риски и техдолг](#14-риски-и-техдолг)
15. [Связанные документы](#15-связанные-документы)

## 1. Цели и ограничения

- Чистое, тестируемое ядро правил без зависимости от движка (ADR-002).
- Детерминированная симуляция: целочисленная математика и seeded RNG (ADR-017, ADR-022).
- Весь контент — данные, а не код (ADR-016).
- Godot 4.7.2 .NET — только presentation, ввод и адаптеры.
- Бюджет 0: только BCL, xUnit и Godot .NET SDK, без сторонних NuGet-пакетов.

## 2. Слои системы

| Слой | Расположение | Ответственность | Зависимости |
|---|---|---|---|
| Ядро | `src/Game.Core` | домен, правила, RNG, события, логирование (абстракция), сейвы, контент-контракты | только BCL |
| CLI / симуляция | `src/Game.SimRunner` | headless-прогоны, валидатор контента, отчёты | `Game.Core` |
| Godot-слой | `game/` | сцены, ввод, рендер, UI, адаптеры движка | `Game.Core` |
| Контент | `game/content/core` | JSON-данные, локализация | никто (данные) |
| Моды | `mods/` | data-паки с `manifest.json` | контент-контракты ядра |
| Инструменты | `tools/scripts` | build/test/run/validate | dotnet CLI |

Направление зависимостей строго одностороннее: `game` → `Game.Core`, `SimRunner` → `Game.Core`.
Обратных ссылок нет.

## 3. Структура solution

Фактическое состояние на этапе 03:

```
EidolonSeamWars.sln
├── src/Game.Core/            classlib net8.0
│   ├── Diagnostics/          LogLevel, LogEvent, IGameLogger
│   ├── Numerics/             IntegerMath
│   ├── Rng/                  SeededRandom, RngStreamKind, RngStreams
│   ├── Events/               IDomainEvent, EventBus
│   ├── Domain/               Hex, HexOffsetLayout, ResourceKind, ResourcePool,
│   │                         DamageType, Resistances, MovementType, UnitDef,
│   │                         VeterancyRules, Stack, Army, HeroStatKind,
│   │                         HeroStats, HeroClassDefinition, LevelUp,
│   │                         HeroProgression, Hero, Settlement
│   ├── Economy/              GrowthAccumulator, HousingStock, RecruitmentContract,
│   │                         IRecruitmentContractSource, RecruitmentBoard,
│   │                         TimedEffect, IDailyAiStep, IAutosaveStep,
│   │                         DailyTickState, DailyTickResult, DailyTick
│   ├── Heroes/               EscapeResult, RansomQuote, CaptivityState, EscapeRules
│   └── Content/              ContentSchema, IContentFileSystem, ContentLoader,
│                             ContentMerge, ContentRegistry, ContentValidator,
│                             ContentEntry, ContentChange, ContentPack, Dto/,
│                             Localization/, Internal/
├── src/Game.SimRunner/       console net8.0 (help/version/validate)
├── tests/Game.Core.Tests/    xUnit
└── game/EidolonSeamWars.csproj  Godot 4.7.2 .NET, ссылается на Game.Core
```

Запланировано по GDD §14.2 (появится на этапах 04–09):

| Пространство имён | Содержимое | Этап | Статус |
|---|---|---|---|
| `Game.Core.Domain` | UnitDef, Stack, Army, Hero, HeroStats, HeroClassDefinition, LevelUp, HeroProgression, Settlement, Resource | 02–03 | реализовано |
| `Game.Core.Economy` | DailyTick, GrowthAccumulator, Income, Recruitment | 02 | реализовано |
| `Game.Core.Heroes` | Equipment и расширение Escape/Ransom | 02 | реализовано |
| `Game.Core.Content` | схемы, DTO, загрузчик, merge, реестр, валидатор, локализация | 03 | реализовано |
| `Game.Core.Battle` | BattleState, TurnQueue, DamageCalculator, MoraleSystem, AbilitySystem | 05 | запланировано |
| `Game.Core.Map` | HexGrid, pathfinding, FogOfWar, MapObject, PartyMovement | 04 | запланировано |
| `Game.Core.Persistence` | Save DTO, миграции, слоты | 07 | запланировано |
| `Game.Core.Modding` | manifest, load order, конфликты | 03 | частично: реализовано в `Content` |
| `Game.Core.Diagnostics` | расширение: файловый логгер, crash-отчёт | 09 | запланировано |

Godot-проект `game/` включён в корневой solution. Сборка идёт через
`Godot.NET.Sdk/4.7.2` из NuGet и не требует установленного редактора.
`application/run/main_scene` указывает на `res://scenes/boot/Boot.tscn`.
Проверка версий и headless-запуск — [docs/TOOLCHAIN.md](TOOLCHAIN.md),
[docs/RUNBOOK.md](RUNBOOK.md).

## 4. Правила зависимостей

1. В `Game.Core` запрещён `using Godot`, `Godot.*` и любые движковые типы.
2. `Game.Core` не ссылается на `Game.SimRunner`, `game/` или тесты.
3. Тесты ссылаются только на `Game.Core`.
4. `game/Directory.Build.props` импортирует корневые свойства и подключает
   `Game.Core` к Godot-csproj.
5. Новые NuGet-пакеты — только через ADR.
6. Запрещены циклические ссылки между пространствами имён: `Domain` не знает о `Battle`,
   `Battle` использует `Domain`, `Economy` использует `Domain`.

Проверка на этапе 01 выполняется вручную (`grep` по `using Godot`), на этапе 09 —
архитектурным тестом.

## 5. Доменная модель

Ключевые сущности этапов 01–02:

- **Hex** — позиция на аксиальной сетке. Реализован.
- **ResourceKind / ResourcePool** — семь ресурсов и иммутабельный набор с
  проверяемым сложением и оплатой.
- **UnitDef** — контентное описание юнита: лидерство, атака, защита, резисты, урон,
  крит, HP, тип передвижения, способности, зарезервированный боезапас.
- **VeterancyRules / Stack** — тип × количество; количество является пулом HP
  (GDD §7.2), ранг ветеранства хранится за стаком (ADR-014).
- **Army** — до 5 слотов, жёсткая проверка лидерства (ADR-006); ёмкость задаётся
  при создании (`hero.Leadership` или 50 для отряда без героя).
- **Hero / HeroStats / HeroClassDefinition / LevelUp** — характеристики, класс,
  артефакты, уровень; детерминированный прирост по весам класса через стрим
  `levelup`; герой вне поля боя (ADR-007).
- **Settlement** — владелец, шаблон, уровень, здания, гарнизон; захват по правилу
  MVP (ADR-013) очищает здания и гарнизон и сбрасывает уровень до 1.
- **DailyTick / GrowthAccumulator / HousingStock / RecruitmentBoard / TimedEffect** —
  дневная экономика (ADR-004): доход, прирост, жилища, контракты, длительности,
  точки расширения ИИ и автосохранения.
- **EscapeRules / RansomQuote / CaptivityState** — побег, плен и выкуп (ADR-012).
- **WorldMap** — гекс-сетка 48×48, объекты, туман войны (этап 04).
- **BattleState** — поле 13×9, очередь хода, эффекты, трупы, командная панель героя
  (этап 05).

Домен иммутабелен там, где это практично: `Hex`, `LogEvent`, `ResourcePool`,
`Resistances`, `HeroStats`, контентные записи.
Мутации состояния — только через команды (ADR-018).

## 6. Команды и события

| Механизм | Назначение | Меняет состояние | Пример |
|---|---|---|---|
| `BattleCommand` | шаг боя | да | атака, способность, ожидание, защита |
| `MapCommand` | шаг карты | да | движение, найм, захват, конец дня |
| `IDomainEvent` | уведомление о факте | нет | `UnitDiedEvent`, `LevelUpEvent` |

`EventBus` (реализован) — синхронная доставка событий в порядке подписки.
Исключение обработчика журналируется и пробрасывается; последующие обработчики
не вызываются. Шина не заменяет команды и не используется для изменения состояния.

## 7. Детерминизм и RNG

- Мастер-сид партии хранится в состоянии и сейве.
- `RngStreams` выводит четыре независимых стрима: `map`, `battle`, `loot`, `levelup`.
- Один и тот же мастер-сид + один и тот же порядок вызовов = одинаковый результат.
- Стрим `battle` не расходуется генерацией карты и наоборот (есть тест).
- `SeededRandom` — SplitMix64; для ограниченных диапазонов используется отбрасывание
  смещения, а не деление по модулю без проверки.
- `RollPerMille()` — единственный рекомендуемый способ броска в правилах.
- Запрещено использовать `System.Random`, `Guid.NewGuid()` и время в доменных правилах.

## 8. Контентный конвейер

```
JSON в game/content/core (+ mods/<modId>/)
        │
        ▼
ContentLoader (файлы → DTO + диагностика)        этап 03
        │
        ▼
ContentMerge (порядок паков, last-wins)           этап 03
        │
        ▼
ContentValidator (схема, ссылки, локализация)     этап 03
        │
        ▼
ContentRegistry (стабильные ID, иммутабельный)     этап 03
        │
        ▼
Игра / SimRunner
```

Правила: `schemaVersion` обязателен, неизвестная версия — ошибка, а не «попробуем».
Все ссылки (юнит → фракция, герой → класс, здание → поселение) валидируются.
Порядок паков детерминирован: зависимости → `loadOrder` → ID.
Одинаковые ID переопределяются последним паком (last-wins), каждое
переопределение попадает в `ContentChange` и лог.
Локализация — только ключи; отсутствие ключа логируется и показывается как `[key]`.
Файловый IO в Core запрещён: чтение идёт через `IContentFileSystem`,
адаптеры — `DiskContentFileSystem` (SimRunner) и `GodotContentFileSystem` (Godot).
Точные формы файлов и коды диагностики — [docs/CONTENT_SCHEMA.md](CONTENT_SCHEMA.md).

## 9. Граница presentation

- `float`/`double` разрешены только в `game/` (анимации, интерполяция, громкость).
- Presentation не содержит игровых формул: только чтение состояния и отправка команд.
- Адаптеры (`game/scripts/Adapters`) переводят доменные типы в Godot-типы и обратно.
- UI не меняет домен напрямую; все действия проходят через команды.
- Карточка юнита отображает данные, но не пересчитывает правила боя.

## 10. Логирование

`IGameLogger` живёт в Core; реализации — в Godot-слое и SimRunner.
Формат: структурированные JSON-строки, `sessionId`, `correlationId`, категории
`Content, Mods, Save, Battle, Economy, Map, AI, Hero, UI, Settings, Loc`.
Обязательные точки и ротация описаны в [docs/LOGGING.md](LOGGING.md).

## 11. Сохранения

- Формат: версия, миграции, безлимитные слоты, метаданные (день, фракция, уровень героя).
- Сохраняется мастер-сид, состояние и журнал команд; журнал позволяет реплей.
- Запись атомарная: временный файл → переименование.
- Схема и миграции — [docs/SAVE_FORMAT.md](SAVE_FORMAT.md), реализация — этап 07.

## 12. Моддинг

- Data-паки в `mods/<modId>/` с `manifest.json`, порядок загрузки и конфликты — этап 03.
- Мод не может нарушать инварианты: лидерство, целочисленная математика, стабильные ID.
- Скрипт-моды (Roslyn) — только M4, с песочницей и permissions.
- Подробности — [docs/MODDING.md](MODDING.md).

## 13. Тестирование

- xUnit, только `Game.Core`; Godot не требуется.
- Тесты инфраструктуры этапа 01: `HexTests`, `SeededRandomTests`, `RngStreamsTests`,
  `EventBusTests`.
- Тесты домена этапа 02: `IntegerMathTests`, `ResourcePoolTests`, `UnitDefTests`,
  `StackTests`, `VeterancyTests`, `ArmyLeadershipTests`, `LevelUpTests`,
  `HeroProgressionTests`, `DailyTickTests`, `SettlementCaptureTests`, `EscapeRansomTests`.
- Тесты контента этапа 03: `ContentPipelineTests`, `ContentValidatorTests`
  (загрузка, merge, порядок, переопределения, локализация, реальный контент).
- Детерминированные тесты: фиксированные сиды, без времени и сети.
- Матрица тестов по этапам — [docs/TESTING.md](TESTING.md).

## 14. Риски и техдолг

| Риск | Влияние | Смягчение |
|---|---|---|
| Godot-проект `game/` входит в solution | сборка зависит от `Godot.NET.Sdk` из NuGet | версия SDK зафиксирована ADR-023, CI собирает solution |
| Версия Godot может отличаться на другой машине | предупреждение в рантайме | `Engine.GetVersionInfo()` + docs/TOOLCHAIN.md |
| Файлы правил этапа 04 в контенте | загрузчик этапа 03 пропускает их с предупреждением | DTO и валидация появятся на этапе 04; сейчас `CONTENT_RULE_UNKNOWN` |
| Логгер только интерфейс | нет файловых логов | реализация и crash-отчёт на этапе 09 |
| Архитектурный тест на Godot отсутствует | случайный `using Godot` в Core | ревью + тест этапа 09 |
| Реплеи без golden-теста RNG | смена алгоритма может остаться незамеченной | ADR-022 + тесты стабильности стримов |
| Переопределение core-ID модом | игрок не понимает, откуда число | журнал `ContentChange` + лог, UI модов — позже |

## 15. Связанные документы

- [docs/GDD.md](GDD.md) §14 — исходная архитектура
- [docs/TDD.md](TDD.md) — технические детали
- [docs/DECISIONS.md](DECISIONS.md) — ADR
- [docs/CONTENT_SCHEMA.md](CONTENT_SCHEMA.md)
- [docs/LOGGING.md](LOGGING.md)
- [docs/TESTING.md](TESTING.md)
- [docs/SAVE_FORMAT.md](SAVE_FORMAT.md)
- [docs/RUNBOOK.md](RUNBOOK.md)
