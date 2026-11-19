# Technical Design Document (TDD)

| Параметр | Значение |
|---|---|
| Версия | 0.4.0 |
| Статус | Актуально для этапа 04 (ядро карты); Godot-слой не проверен |
| Дата | 2026-11-16 |
| Источник истины | [docs/GDD.md](GDD.md), [docs/ARCHITECTURE.md](ARCHITECTURE.md) |

## Оглавление

1. [Назначение](#1-назначение)
2. [Платформа и сборка](#2-платформа-и-сборка)
3. [Hex: аксиальные координаты](#3-hex-аксиальные-координаты)
4. [RNG: SplitMix64 и стримы](#4-rng-splitmix64-и-стримы)
5. [Целочисленная математика](#5-целочисленная-математика)
6. [События и команды](#6-события-и-команды)
7. [Диагностика](#7-диагностика)
8. [Обработка ошибок](#8-обработка-ошибок)
9. [Производительность](#9-производительность)
10. [Интеграция с Godot](#10-интеграция-с-godot)
11. [Состояние этапа 03](#11-состояние-этапа-03)
12. [Карта: этап 04](#12-карта-этап-04)
13. [Бой: этапы 05a–05g (формулы, мораль, поле, очередь, состояние, атака, реплей, итог, способности и герой)](#13-бой-этапы-05a05g-формулы-мораль-поле-очередь-состояние-атака-реплей-итог-способности-и-герой)
14. [Связанные документы](#14-связанные-документы)

## 1. Назначение

Документ фиксирует технические решения, которые не видны из GDD: точные формулы
инфраструктурных типов, алгоритмы RNG, контракты ошибок и границы слоёв.
Дизайн-правила не дублируются — ссылки на секции GDD обязательны.

## 2. Платформа и сборка

| Параметр | Значение |
|---|---|
| Target framework | `net8.0` |
| Язык | C# 12 (`LangVersion=12`) |
| Nullable | включён |
| Implicit usings | включены |
| Версия продукта | `0.3.0` (`Directory.Build.props`) |
| Тесты | xUnit 2.5.3, Microsoft.NET.Test.Sdk 17.8.0 |
| Godot | 4.7.2 .NET (ADR-023), проект создан Godot |

Общие свойства — в корневом `Directory.Build.props`. Версия задаётся один раз и
читается SimRunner из `AssemblyInformationalVersionAttribute`.

`dotnet build EidolonSeamWars.sln` собирает `Game.Core`, `Game.SimRunner`,
тесты и Godot-слой. Godot-слой использует `Godot.NET.Sdk/4.7.2` из NuGet и
не требует установленного редактора (ADR-023). Версии и проверки —
[docs/TOOLCHAIN.md](TOOLCHAIN.md).

## 3. Hex: аксиальные координаты

Реализация: `src/Game.Core/Domain/Hex.cs` (ADR-003).

- Хранимые координаты: `Q`, `R`. `S = −Q − R`.
- Соседи: 6 фиксированных смещений в порядке
  `(1,0)`, `(1,−1)`, `(0,−1)`, `(−1,0)`, `(−1,1)`, `(0,1)`.
- Дистанция: `(|dq| + |dr| + |ds|) / 2`, вычисляется в `long`, результат проверяется
  на переполнение `int`.
- Offset-конверсия (только граница домена):

| Схема | Hex → offset | Offset → Hex |
|---|---|---|
| `OddR` | `col = q + (r − (r & 1)) / 2`, `row = r` | `q = col − (row − (row & 1)) / 2` |
| `EvenR` | `col = q + (r + (r & 1)) / 2`, `row = r` | `q = col − (row + (row & 1)) / 2` |

- Побитовое `& 1` корректно работает и для отрицательных значений в C#.
- `Hex` — `readonly struct`, реализует `IEquatable<Hex>`, `==`, `!=`, `GetHashCode`.
- Некорректный индекс направления и неизвестная схема offset выбрасывают
  `ArgumentOutOfRangeException` с контекстом.
- Round-trip проверяется для `q, r ∈ [−12, 12]` и обеих схем.

## 4. RNG: SplitMix64 и стримы

Реализация: `src/Game.Core/Rng` (ADR-017, ADR-022).

### 4.1. SeededRandom

```
state += 0x9E3779B97F4A7C15
z = state
z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9
z = (z ^ (z >> 27)) * 0x94D049BB133111EB
return z ^ (z >> 31)
```

- `NextUInt64()` — базовый выход.
- `NextUInt32()` — старшие 32 бита (младшие биты SplitMix64 не используются отдельно).
- `Next(min, maxExclusive)` — равномерное целое без смещения: отбрасывание значений
  ниже `(0 − range) % range`, затем `% range`.
- `RollPerMille()` — `Next(0, 1000)`, основной бросок в правилах.
- Сид `0` допустим.

### 4.2. RngStreams

- Мастер-сид партии → четыре стрима: `map`, `battle`, `loot`, `levelup`.
- Ключ вывода: `FNV-1a64("esw.rng.v1." + streamName)`, затем `Mix64(masterSeed ^ hash)`.
- `Get(kind)` кэширует экземпляр: повторный вызов продолжает последовательность.
- Стримы независимы: расход `battle` не сдвигает `map` (покрыто тестом).
- Порядок вызовов внутри стрима — часть контракта реплея. Рефакторинг, меняющий
  порядок бросков, обязан сопровождаться ADR и проверкой реплеев.
- Запрещено использовать `System.Random` в домене.

## 5. Целочисленная математика

- Формулы боя и экономики — в промилле (per-mille), см. GDD §7.4 и ADR-022.
- Промежуточные значения — `long`; приведение к `int` — проверяемое.
- Округление per-mille — «половина от нуля», только на итоговом шаге формулы.
- `float`/`double` в домене запрещены. Исключение — presentation-слой.
- Деньги и ресурсы — целые; никаких дробных количеств.
- Проценты в контенте — целые числа (например, `critChancePercent: 10`,
  резисты `physicalPercent: 20`).

### 5.1. IntegerMath (этап 02)

Реализация: `src/Game.Core/Numerics/IntegerMath.cs`.

| Метод | Контракт |
|---|---|
| `MulDiv(value, multiplier, divisor)` | `value × multiplier / divisor` в `long` с округлением половины от нуля; делитель `0` → `DivideByZeroException`, `long.MinValue` → `ArgumentOutOfRangeException`, переполнение произведения → `OverflowException` с контекстом |
| `MulDivToInt(value, multiplier, divisor)` | то же с проверяемым приведением к `int` |
| `CeilDiv(value, divisor)` | деление неотрицательного значения на положительный делитель с округлением вверх; используется для капа прироста |

Округление половины от нуля реализовано без переполнения:
`|remainder| >= |divisor| − |remainder|` вместо удвоения остатка.
Граничные случаи (`0`, отрицательные, переполнение) покрыты `IntegerMathTests`.

### 5.2. Формулы домена этапа 02

- Прирост: `stock += growthPerDay`; целые юниты переносятся в `AvailableUnits`,
  дробный остаток остаётся в промилле; кап `ceil(growthPerDay × 14)`.
- Жилище: захваченное производит прирост и не превышает `Cap`; незахваченное
  регенерирует на фиксированное число юнитов в день и не выдаёт найм.
- Дневной тик: `Day += 1` → доход → прирост → жилища → контракты → длительности →
  ИИ → автосохранение. ИИ и автосохранение — интерфейсы-точки расширения.
- Лидерство: `Σ(leadershipCost × count) ≤ Capacity`; проверка выполняется до
  изменения армии, отказ логируется.
- Ветеранство: ранг по порогам `0/2/5/10/18`, бонусы `+2` атака/защита и `+1 п.п.`
  крит. шанс/урон за ранг выше первого; крит клампится 75%, урон — 300%.
- Герой: `Mana = 10 + Knowledge × 5`, `MovementPoints = 12 + Logistics` (ADR-034),
  `Leadership = Stats.Leadership`; левелап даёт `+1` к характеристике по весам класса,
  бросок только через стрим `levelup` (точная кривая — §5.3).
- Побег и выкуп: формулы ADR-012; защитник осады получает `Forbidden`.
- Захват поселения: здания и гарнизон очищаются, уровень → 1, шаблон → фракция
  захватчика (ADR-013).

### 5.3. Прогрессия героя (этап 03, ADR-025)

Реализация: `src/Game.Core/Domain/HeroProgression.cs`, `Hero.ApplyLevelUp`.

- `XpToNext(level) = coefficient × level × (level + 1)`, где `coefficient` из
  `rules/progression.json` (сейчас `50`).
- Суммарный XP до перехода с уровня `1` на уровень `steps + 1`:
  `coefficient × steps × (steps + 1) × (steps + 2) / 3`.
- XP не сбрасывается: накопленный опыт сравнивается с порогами накопительно.
- За каждый уровень: `+1` к случайной характеристике по весам класса и
  `+skillPointsPerLevel` очков навыка (сейчас `1`). Очки копятся, не тратятся в MVP.
- Мягкий предел уровня — параметр сценария (`scenarioSoftCap`), а не домена:
  `0` или меньше — без предела. Сейчас `10`.
- Все вычисления — целочисленные, только `long` в промежуточных шагах.
- Пороги и повышения логируются (категория `Hero`).

Публичный `Hero.TryLevelUp` удалён: уровень повышается только через прогрессию
с явными порогами, чтобы не было «бесплатных» уровней.

## 6. События и команды

### Команды

- Интерфейсы `BattleCommand` / `MapCommand` появятся на этапах 02 и 05.
- Команда сериализуема и применяется к состоянию детерминированно.
- Невалидная команда отклоняется с ошибкой и записью в лог, состояние не меняется.

### События

`EventBus` (этап 01):

- `Subscribe<TEvent>(Action<TEvent>) → IDisposable`.
- `Publish<TEvent>(TEvent)` вызывает обработчики в порядке подписки.
- Снимок списка обработчиков берётся под блокировкой, вызовы — вне блокировки.
- Исключение обработчика логируется (`Error`, категория `Events`) и пробрасывается;
  оставшиеся обработчики не вызываются.
- `HasSubscribers<TEvent>()`, `Clear()` — для UI и тестов.
- Шина не потокобезопасна по семантике доставки: publish/subscribe предполагаются
  в игровом потоке.

## 7. Диагностика

- `LogLevel`: `Trace`, `Debug`, `Info`, `Warning`, `Error`, `Critical`.
- `LogEvent` — иммутабельная запись: время UTC, уровень, категория, сообщение,
  `sessionId`, `correlationId`, словарь полей.
- `LogEvent.Create(...)` валидирует непустые категорию и сообщение.
- `IGameLogger`: `IsEnabled(level)` + `Log(LogEvent)`.
  Реализация обязана не бросать исключений при ошибках I/O.
- Категории и точки логирования — [docs/LOGGING.md](LOGGING.md).
- Формат сериализации (JSON-строка) реализуется инфраструктурой, не Core.

## 8. Обработка ошибок

| Граница | Поведение |
|---|---|
| Публичный API домена | валидация аргументов, `Argument*Exception` с именем параметра |
| Контент | ошибка валидации с путём файла и ID записи, загрузка отклоняется |
| Команда | отказ с причиной, состояние не меняется, запись в лог |
| Сейв | миграция или отказ с версией и путём, без частичной записи |
| Pathfinding | ошибка логируется, вызывающий код получает «путь не найден» |
| Godot-слой | ошибки не глотаются: лог + видимое сообщение в UI |

Внутри домена исключения содержат контекст: какие значения, какой ID, какое правило.
`catch (Exception)` без логирования и повторного throw запрещён.

## 9. Производительность

Целевые ориентиры MVP (не преждевременная оптимизация):

- Ход ИИ на карте 48×48 — до 200 мс на слабом ПК.
- Полный бой 13×9 — до 16 МБ managed-аллокаций без учёта UI (ориентир для профилирования).
- Симуляция 100 боёв headless — до 5 секунд.
- Запрещены аллокации в цикле расчёта урона сверх необходимого; `Hex` — struct.
- Профилирование — Godot profiler и замеры SimRunner, только после появления правил.

## 10. Интеграция с Godot

- `game/Directory.Build.props` подключает `Game.Core` к созданному вручную csproj.
- `BootController` (`game/scripts/Boot/BootController.cs`) — минимальная точка входа,
  читает `application/config/version`, пишет версию проекта и фактическую версию
  Godot из `Engine.GetVersionInfo()`. Ожидаемая версия — константа
  `ExpectedGodotVersion = "4.7.2"` презентационного слоя; при несовпадении
  пишется предупреждение, игра не падает.
- Главная сцена — `game/scenes/boot/Boot.tscn`, задана в
  `application/run/main_scene`.
- `BootController` на этапе 03 загружает контент `res://content/core` через
  `GodotContentFileSystem`, выполняет merge и валидацию, пишет счётчики в лог.
  При ошибке — `GD.PushError` и `GetTree().Quit(1)`; при успехе boot продолжается.
- Godot-типы не попадают в Core; адаптеры — в `game/scripts/Adapters`.
- Сигналы Godot не являются доменными событиями; адаптер переводит их в команды.
- Версия в `project.godot` должна совпадать с `Directory.Build.props` и CHANGELOG.

## 11. Состояние этапа 03

Реализовано:

- `Hex`, `HexOffsetLayout`;
- `SeededRandom`, `RngStreamKind`, `RngStreams`;
- `LogLevel`, `LogEvent`, `IGameLogger`;
- `IDomainEvent`, `EventBus`;
- `IntegerMath`, ресурсы, `UnitDef`, `Stack`, `Army`, `Hero`, `HeroStats`,
  `HeroClassDefinition`, `LevelUp`, `HeroProgression`, `Settlement`;
- экономика `GrowthAccumulator`, `HousingStock`, `RecruitmentContract`,
  `RecruitmentBoard`, `TimedEffect`, `DailyTick` с точками расширения ИИ и
  автосохранения;
- формулы побега/выкупа/плена (`EscapeRules`, `RansomQuote`, `CaptivityState`);
- контентный конвейер: `ContentSchema`, `IContentFileSystem`, DTO всех категорий,
  `ContentLoader`, `ContentMerge`, `ContentRegistry`, `ContentValidator`;
- локализация: `LanguagesManifestDto`, `LocalizationTable`, `LocalizationSet`,
  `LocalizationResolver` с цепочкой фолбэка;
- `SimRunner validate [--content PATH]` с кодами выхода 0/1/2;
- адаптеры Godot: `GodotContentFileSystem`, `GodotGameLogger`, загрузка контента в
  `BootController`;
- тесты этих типов (на конец этапа 03: 262 теста, 0 провалов).

Не реализовано и не должно считаться готовым:

- карта, движение, биомы, генерация карты (этап 04); файлы `rules/biomes.json`
  и `rules/map_generation.json` пока пропускаются с `CONTENT_RULE_UNKNOWN`;
- бой, способности, мораль в бою (этап 05);
- UI-карточки и экраны (этапы 06–07);
- сейвы в файлы и миграции (этап 07);
- файловый логгер и crash-отчёт (этап 09);
- ИИ (этап 08), кроме точки расширения в `DailyTick`.`

## 12. Карта: этап 04

Весь код — в `src/Game.Core/Map` (без Godot), все числа целые.

### 12.1. Сетка и карта

- `HexGrid` — прямоугольная сетка `width × height` в offset-схеме (`odd-r`), индекс гекса
  по порядку строк; `Contains`, `IndexOf`, `AllHexes`, `WithinRange`.
- `BiomeDefinition`/`BiomeCatalog` — справочник биомов из `rules/biomes.json`.
- `GameMap` — тайлы, объекты и стоимость входа в каждый гекс. Непроходимый гекс имеет
  стоимость `int.MaxValue`; для проходимых это `movementCostPerHex` биома (ADR-034).
- `MapObjectDefinition`/`MapObjectCatalog`/`MapObjectInstance` — объекты карты; блокирующим
  считается объект с `guarded = true`.
- `MapLayoutDefinition` — представление карты (тайлы, объекты, старты); общий формат для
  генератора и тестовых карт.

### 12.2. Поиск пути

`Pathfinder.FindPath(map, start, goal, blockOccupiedTiles = false, blockedHexes = null)` — A* с целочисленной
эвристикой «расстояние в гексах × 1»; она допустима, пока минимальная стоимость шага равна 1
(дорога, ADR-034). Порядок соседей фиксирован, ничья в очереди решается по (F, G, порядковый
номер), поэтому результат детерминирован. Стоимость пути — сумма стоимостей входа в гексы
без стартового. `ComputeReachable(map, start, maxCost)` строит достижимые гексы в пределах
бюджета (блокирующие объекты не учитываются; гексы из `blockedHexes` достижимы как цель, но не расширяют область). Результат пути — `PathResult` с
`PathFailure`: `None`, `NoPath`, `ImpassableGoal`, `OutOfBounds`, `ImpassableStart`.

### 12.3. Партии и состояние карты

- `Party` — роль (`Player`, `Enemy`, `Neutral`), фракция, необязательный герой, ёмкость
  лидерства, позиция, очки движения и их максимум.
- `MapState` — карта, партии, туман, настройки и номер дня. Единственная точка изменения —
  `Apply(MapCommand)` → `MapCommandResult`.
- Команды (`MapCommandKind`): `MoveParty`, `EndDay`, `EnterObject`.
- Исходы (`MapCommandOutcome`): `Applied`, `DayEnded`, `ObjectEntered`, `Collision`,
  `InvalidParty`, `NoPath`, `NotEnoughMovement`, `Impassable`, `OutOfBounds`, `InvalidObject`.
- Движение: путь строится с обходом гексов с блокирующими объектами (`blockOccupiedTiles`);
  цель может быть занята. Партия перемещается на целевой гекс и платит стоимость пути; если
  там блокирующий объект или другая партия — исход `Collision` (бой передаётся экрану боя,
  автобоя нет). Недостаток очков → `NotEnoughMovement` без перемещения.
  Партии на промежуточных гексах тоже блокируют транзитный путь (`blockedHexes` = гексы других
  партий, как `MapState.GetOtherPartyHexes`): встреча возможна только в целевом гексе; если обхода
  нет — `NoPath`, очки не списываются. `MapState.ComputeReachable(partyId)` строит область
  достижимости по остатку очков партии с учётом этих блокировок.
- Очки движения: герой `12 + Логистика`, отряд без героя `armyMovementPointsPerDay` (ADR-034).

### 12.4. Туман войны

`FogOfWar` хранит состояние каждого гекса по фракциям: `Hidden`, `Explored`, `Visible`.
Видимость фракции — объединение обзоров всех её партий; радиус обзора — максимум из
`fogVisibilityRadius` и `visibilityRange` биома под партией; разведка (`fogExploredRadius`)
остаётся навсегда (ADR-033). Пересчёт — `MapState.RefreshFog` после движения и конца дня.

### 12.5. Команды, журнал, реплей

`MapCommandCodec` сериализует команды в JSON (формат версии 1); `MapCommandJournal` хранит
мастер-сид, команды и их результаты. Реплей: та же стартовая карта и те же команды дают то же
состояние (тест `CommandReplay_ProducesSameFinalState`).

### 12.6. Генерация

`MapGenerator.Generate(MapStartRequest)` — детерминированно по мастер-сиду из стрима `map`
(ADR-027): веса биомов из `biomeDistribution` в порядке ID, стартовые поселения, затем объекты
по `objectCounts`. Параметры — `MapGenerationSettings` (из `rules/map_generation.json`).
Граничное кольцо (гексы, у которых есть сосед вне сетки) заполняется биомом `borderBiome` («Шов»,
непроходимый, ADR-036) после выбора биомов; объекты и старты на кольцо не ставятся. Рукотворные
тестовые карты бордюр не содержат.

### 12.7. Тестовые карты

`HandmadeMapLoader` разворачивает `test_maps/*.json` в `MapLayoutDefinition` и `GameMap`
(ADR-028). Эталонные стоимости, достижимость, число объектов и дистанции стартов лежат в
`testCases` карт и дублируются в `MapTests`.

### 12.8. Граница этапа 04

Не реализовано: бой по столкновению, захват объектов с наградами, ИИ партий, сохранение
карты. Godot-слой (`game/scripts/Map`, `game/scenes/map`) написан без запуска движка и
не проверен (KI-004, KI-014).

## 13. Бой: этапы 05a–05e1 (формулы, мораль, поле, очередь, состояние)

- `BattleRules` — иммутабельные числа из `rules/battle.json`; `BattleContentAdapter` строит правила и `UnitDef`.
- `DamageCalculator.Calculate(DamageRequest, SeededRandom battle) → DamageResult` — порядок шагов и
  контракт бросков в ADR-035. `DamageResult` хранит все шаги для журнала (категория `Battle`, Debug).
- `UnitDef` получил `UnitClass` и `DamageType`.
- 05b: `MoraleRules` (из `rules/morale.json` через `BattleContentAdapter.CreateMoraleRules`), `MoraleContext`,
  `MoraleSystem` (уровень армии, иммунитет, шанс/модификатор по таблице с капами, бросок `RollAction` — один
  бросок `battle`, ADR-037).
- 05c: `BattleField` (сетка 13×9, препятствия, `GetLine`, `HasBlockerBetween`), `DamageCalculator.GetRangedModifierPerMille`
  (штрафы дистанции и препятствия суммируются, ADR-037); `BattleRules.LongDistanceThreshold`.
- 05d: `BattleUnit` (пул здоровья, ветеранство, флаги раунда), `CasualtyResult`, `TurnQueue`/`TurnEntry` (группы A/B,
  тай-брейк, ожидание, дополнительные действия морали, ADR-038).
- 05e1: `BattleState` (расстановка, `Start`, `Apply`, `ComputeReachable`, мораль в начале хода, уровень морали стороны,
  статус боя), `BattleCommand`/`BattleCommandKind`/`BattleCommandOutcome`/`BattleCommandResult`, `BattleSideSetup`,
  `BattleStatus` (ADR-039). Команды: перемещение, ожидание, защита, атака.
- 05e2 (ADR-040): пул очков движения `BattleUnit.MovementPointsRemaining` (перемещение ход не завершает, атака
  завершает); `BattleCommand.Attack` — ближняя атака с ручным или комбинированным подходом (`путь < остатка`),
  дальняя атака стрелков без контратаки; автоматическая контратака (1 за раунд) со штрафом 75 % для стрелков;
  урон с учётом морали, «Защиты», ветеранства; гибель стака → ветеранство атакующему, штраф морали стороне.
- 05f1 (ADR-041): `BattleCommandCodec` (JSON), `BattleCommandJournal` (применение, запись, `Replay`),
  `BattleState.CreateSnapshot()`; атака требует `attackMovementCost` очков (ADR-040).
- 05f2 (ADR-042): `BattleResult`/`BattleStackResult`, `BattleState.CreateResult/CommitResult`, `BattleAftermathResolver`
  (`BattleAftermath`, `BattleAftermathKind`) на формулах `EscapeRules` этапа 02.
- 05g (ADR-043): `BattleCatalog`/`AbilityDef`/`SpellDef`/`EffectDef`/`BattleEffectTypes`, `BattleHero`, `ActiveEffect`;
  команды `UseAbility`, `CastSpell`, `UseCommandAbility`; пассивные эффекты в расчёте удара; регенерация, кулдауны,
  укоренение; `BattleResult.RaisedUnits`. Итог поражения — армия перестаёт существовать (ADR-042).
- Не реализовано: эффекты командных способностей (заглушка), масштабирование Чародейством (KI-021, KI-022).

## 14. Связанные документы

- [docs/GDD.md](GDD.md) §7, §14, §16
- [docs/ARCHITECTURE.md](ARCHITECTURE.md)
- [docs/DECISIONS.md](DECISIONS.md)
- [docs/LOGGING.md](LOGGING.md)
- [docs/TESTING.md](TESTING.md)
- [docs/CONTENT_SCHEMA.md](CONTENT_SCHEMA.md)
