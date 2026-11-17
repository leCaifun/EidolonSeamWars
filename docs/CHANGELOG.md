# Changelog

| Параметр | Значение |
|---|---|
| Версия | 0.3.0 + `[Unreleased]` |
| Статус | Актуально |
| Дата | 2026-11-16 |
| Формат | [Keep a Changelog](https://keepachangelog.com/ru/1.1.0/) + [SemVer](https://semver.org/lang/ru/) |

## Оглавление

1. [[Unreleased]](#unreleased)
2. [[0.3.0] — 2026-11-13](#030---2026-11-13)
3. [Связанные документы](#связанные-документы)

Формат: Keep a Changelog, версионирование: SemVer.
История решений и проверок — [docs/WORKLOG.md](WORKLOG.md); правила Git — ADR-029.

## [Unreleased]

### Added (этап 04)

- Карта: `HexGrid`, `GameMap`, `Pathfinder` (A*), `FogOfWar`, `Party`, `MapState`,
  команды и журнал, `MapGenerator`, загрузчик тестовых карт; правила `biomes` и
  `map_generation`; тестовые карты 8×8, 16×16, 48×48.
- Godot-слой карты: экран, камера, туман, токены, HUD (не проверен в движке).
- ADR-029…034.

### Added (этап 05a)

- `Game.Core.Battle`: `BattleRules`, `DamageCalculator`, `DamageRequest`, `DamageResult`; `BattleContentAdapter`; `UnitDef.UnitClass/DamageType`; ADR-035.

### Added (этап 05b)

- Мораль: `MoraleRules`, `MoraleLevelRule`, `MoraleContext`, `MoraleRoll`, `MoraleSystem`; `BattleContentAdapter.CreateMoraleRules`; ADR-037.

### Changed (этап 04)

- Шкала движения: местность не менее 2, дорога 1, очки героя `12 + Логистика` (ADR-034).
- `CONTENT_RULE_UNKNOWN` — ошибка (ADR-030); переопределение core-ID требует `overridesCore`.
- Лицензия кода — Apache-2.0 (ADR-032); `LICENSE-CONTENT.md` объединён с `LICENSE-ASSETS.md`.

### Fixed (этап 04)

- Туман войны учитывает все партии фракции (ADR-033).
- Путь обходит блокирующие объекты.
- Бордюр карты «Шов» задаётся данными (`borderBiome`, ADR-036); вес «Шва» убран из распределения (трава 52 %); переименование Void → Seam проверено.
- Партии на промежуточных гексах блокируют транзитный путь; `MapState.ComputeReachable(partyId)` (KI-017).

### Added (до этапа 04)

- Каркас solution `EidolonSeamWars.sln`: `Game.Core`, `Game.SimRunner`,
  `Game.Core.Tests`.
- Инфраструктурные типы ядра: `Hex`, `HexOffsetLayout`, `SeededRandom`,
  `RngStreamKind`, `RngStreams`, `EventBus`, `IDomainEvent`, `IGameLogger`,
  `LogEvent`, `LogLevel`.
- Тесты ядра: `HexTests`, `SeededRandomTests`, `RngStreamsTests`, `EventBusTests`
  (52 теста).
- Документация: архитектура, TDD, runbook, контент-схемы, логирование,
  тестирование, сейвы, UI, баланс, локализация, моддинг, ИИ, IP-безопасность,
  ассеты, глоссарий, известные проблемы, арт-пайплайн, релизный чек-лист.
- ADR-001…ADR-022, включая ADR-021 (границы MVP) и ADR-022 (округление и RNG).
- ADR-024 (валидатор в `Game.Core.Content`), ADR-025 (кривая опыта героя),
  ADR-026 (локализация и список языков), ADR-027 (детерминированная генерация
  карты из одного сида; предварительно, этап 04).
- Патчноут [docs/patchnotes/v0.3.0.md](patchnotes/v0.3.0.md).
- Промты этапов 02–09 и шаблоны bugfix/feature/balance.
- Git-ready файлы: `.gitignore`, `.gitattributes`, `.editorconfig`, `LICENSE`,
  `README.md`, `CONTRIBUTING.md`, issue/PR-шаблоны, заготовка CI.
- Скрипты `tools/scripts/{build,test,run,validate}.{ps1,sh}`.
- Команды OpenCode: `check`, `docs`, `worklog`, `balance`.
- Оригинальная иконка `game/icon.svg` и `BootController` с выводом версии.
- Godot-структура каталогов, `game/Directory.Build.props` со ссылкой на Core,
  `game/project.godot`, `game/EidolonSeamWars.csproj` и `game/EidolonSeamWars.sln`.
- `application/config/version="0.3.0"` в `project.godot`.
- ADR-023 «Фиксация Godot 4.7.2 .NET»; ADR-001 переведён в статус «Заменён».
- `docs/TOOLCHAIN.md`: OS, Godot 4.7.2 .NET, .NET SDK 8.0.428,
  Windows PowerShell 5.1, команды проверки и правило обновления через ADR.
- Стартовая сцена `game/scenes/boot/Boot.tscn` и `application/run/main_scene`.
- `BootController` логирует фактическую версию Godot через
  `Engine.GetVersionInfo()` и предупреждает о расхождении с 4.7.2.
- `tools/scripts/run-headless.ps1` и `run-headless.sh`: сборка, импорт и
  headless-запуск стартовой сцены.
- Godot-слой `game/EidolonSeamWars.csproj` включён в корневой solution.
- Этап 02: `IntegerMath` с округлением половины от нуля и `CeilDiv`.
- Домен этапа 02: `ResourceKind`, `ResourcePool`, `DamageType`, `Resistances`,
  `MovementType`, `UnitDef`, `VeterancyRules`, `Stack`, `Army`, `HeroStatKind`,
  `HeroStats`, `HeroClassDefinition`, `LevelUp`, `Hero`, `Settlement`.
- Экономика этапа 02: `GrowthAccumulator`, `HousingStock`, `RecruitmentContract`,
  `IRecruitmentContractSource`, `RecruitmentBoard`, `TimedEffect`,
  `IDailyAiStep`, `IAutosaveStep`, `DailyTickState`, `DailyTickResult`,
  `DailyTick`.
- Правила этапа 02: `EscapeResult`, `RansomQuote`, `CaptivityState`, `EscapeRules`.
- Тесты этапа 02: `IntegerMathTests`, `ResourcePoolTests`, `UnitDefTests`,
  `StackTests`, `VeterancyTests`, `ArmyLeadershipTests`, `LevelUpTests`,
  `DailyTickTests`, `SettlementCaptureTests`, `EscapeRansomTests`; всего 212 тестов.
- Полный черновик core-пака: фракции, юниты, герои, классы, правила, здания,
  поселения, объекты карты, абилки, заклинания, ru/en локализация.
- Этап 03: контентный конвейер `ContentSchema`, `IContentFileSystem`,
  `ContentLoader`, `ContentMerge`, `ContentRegistry`, `ContentValidator`,
  `ContentEntry`, `ContentChange`, `ContentPack`, `ContentLoadResult`,
  `ContentJsonReader` и коллекция диагностики.
- DTO всех категорий контента (фракции, юниты, классы, герои, способности,
  заклинания, здания, поселения, объекты карты, ресурсы) и всех правил
  (`battle`, `morale`, `economy`, `escape`, `ransom`, `veterancy`, `progression`).
- Локализация этапа 03: `LanguagesManifestDto`, `LocalizationTable`,
  `LocalizationSet`, `LocalizationManifest`, `LocalizationResolver` с цепочкой
  фолбэка и логированием промаха.
- `HeroProgression` по ADR-025: `XpToNext(level) = 50 × level × (level + 1)`,
  накопительные пороги, очки навыка, мягкий кап сценария.
- CLI `SimRunner validate [--content PATH]` с кодами выхода 0/1/2;
  `DiskContentFileSystem`, `ContentPathResolver`, `ConsoleGameLogger`.
- Godot-адаптеры `GodotContentFileSystem`, `GodotGameLogger`; `BootController`
  загружает `res://content/core`, выполняет merge и валидацию, логирует счётчики,
  при ошибке пишет `GD.PushError` и завершает процесс кодом 1.
- Скрипты `tools/scripts/validate.ps1`, `validate.sh`, `check-localization.ps1`.
- Тесты этапа 03: `ContentPipelineTests`, `ContentValidatorTests`,
  `HeroProgressionTests`; общий набор — 262 теста.
- 26 зарегистрированных языков; статусы в `languages.json` синхронизированы
  с языковыми файлами; `ru`/`en` — по 238 ключей.

### Changed

- `docs/GDD.md` и `docs/ROADMAP.md` приведены к единому формату шапки и
  оглавления; дизайн-содержание v0.3 не изменялось.
- `docs/RUNBOOK.md` переписан под создание/восстановление Godot .NET project.
- `docs/WORKLOG.md` дополнен полной записью W-0001.
- `TargetFramework=net8.0` вынесен в корневой `Directory.Build.props`;
  `tests` и `game` импортируют корневые свойства.
- Все упоминания целевого Godot 4.4 заменены на 4.7.2 в `AGENTS.md`,
  документации, промтах, README и `docs/GDD.md` (техническая строка движка).
- CI собирает корневой solution вместе с Godot-слоем.
- `docs/ARCHITECTURE.md`, `docs/TDD.md`, `docs/TESTING.md`, `docs/BALANCE.md`
  дополнены раскладкой пространств имён и контрактами этапа 02; числа GDD v0.3
  не менялись.
- `docs/WORKLOG.md` дополнен записью W-0003.
- Этап 03: `Hero.TryLevelUp` и `LevelUp.SoftLevelCap` удалены; уровень героя
  повышается только через `HeroProgression` с накопительными порогами XP,
  мягкий кап стал параметром сценария.
- Этап 03: `ContentSchema` стал единым источником допустимых токенов
  (`movement`, `damage`, типы объектов карты, ресурсы, статусы языков).
- Этап 03: `SimRunner` из каркаса `help`/`version` превращён в CLI с командой
  `validate`; путь к контенту резолвится `ContentPathResolver`.
- Этап 03: загрузка контента в Godot перенесена в `BootController._Ready`;
  успешный boot логирует число сущностей, языков и правил.
- Этап 03: статусы 19 языковых файлов механически приведены к `languages.json`
  (ADR-026): 11 `draft`, 7 `experimental`, `la` — `complete`.
- `docs/CONTENT_SCHEMA.md` переписан под фактическую схему этапа 03;
  `docs/MODDING.md` — под реальный манифест и порядок загрузки;
  `docs/LOCALIZATION.md` — под 26 языков, фолбэк и статусы.
- `docs/ARCHITECTURE.md`, `docs/TDD.md`, `docs/TESTING.md`, `docs/BALANCE.md`,
  `docs/SAVE_FORMAT.md`, `docs/KNOWN_ISSUES.md`, `docs/IP_SAFETY.md` обновлены
  под этап 03 (конвейер, прогрессия, 262 теста, риски, `GrowthAccumulator`).
- `docs/GDD.md` §9.6: добавлена накопительная формула XP и уточнение, что
  мягкий кап — параметр сценария (дизайн-правила не менялись).
- `docs/WORKLOG.md` дополнен записью W-0006.

### Fixed

- `EventBus` использовал именованные плейсхолдеры в `string.Format`;
  заменены позиционными `{0}`/`{1}`, тест логирования проходит.
- Из solution удалён несуществующий `game/EidolonSeamWars.csproj`;
  Godot-csproj создаётся вручную и подключается отдельно.
- `run.ps1` привязывал первый аргумент (`version`) к `-Configuration`;
  позиционная привязка убрана, конфигурация задаётся `ESW_CONFIGURATION`.
- `.ps1`-скрипты сохранены с UTF-8 BOM: Windows PowerShell 5.1 больше не
  падает на кириллице в строках.
- Godot-проект `game/` создан и собирается отдельно; KI-001 и KI-007 закрыты.
  Целевая версия 4.4 и локальная 4.7.2 были зафиксированы как KI-008;
  закрыто ADR-023 (целевая версия — Godot 4.7.2 .NET).
- KI-008 закрыт: версия Godot зафиксирована ADR-023, проверка добавлена
  в `BootController` и `docs/TOOLCHAIN.md`.
- Этап 03: удалён дубликат ID `core.unit.brass.militia` — файл
  `units/brass_militia.json` дублировал `units/brass.json`.
- Этап 03: файлы `map_objects` переименованы по стабильным ID, чтобы имя файла
  не расходилось с `id` записи.
- Этап 03: `heroes/ash.json` переписан как отдельный герой
  `core.hero.ash.selha`; раньше файл описывал чужую фракцию.
- Этап 03: `settlements/root.json` переписан как `core.settlement.root`
  по GDD §5.4 (`maxLevel` 2, слоты Корневых Кланов).
- Этап 03: `check-localization.ps1` больше не падает на необязательных языках —
  пропуски в них предупреждения (ADR-026), ошибкой остаётся только `ru`/`en`.
- Этап 03: `BootController` выводит в журнал Godot каждое предупреждение
  загрузчика, merge и валидатора, а не только их количество; пропущенные файлы
  правил этапа 04 теперь видны в логе (`CONTENT_RULE_UNKNOWN`).
- Этап 03: `.ps1`-скрипты `validate.ps1` и `check-localization.ps1` сохранены
  с UTF-8 BOM, чтобы Windows PowerShell 5.1 не ломался на кириллице.
- KI-002 и KI-010 закрыты: валидатор и файловая загрузка контента реализованы.

### Removed

- Шаблонные пустышки `src/Game.Core/Class1.cs` и
  `tests/Game.Core.Tests/UnitTest1.cs`.
- Этап 03: `Hero.TryLevelUp` и `LevelUp.SoftLevelCap` — уровень повышается
  только через `HeroProgression`.
- Этап 03: дублирующий файл `game/content/core/units/brass_militia.json`.

### Balance

- Балансных изменений нет: 0.3.0 — первая зафиксированная дизайн-база.
- Этап 03: числа не менялись, но перенесены в JSON-контент и проверяются
  валидатором; стартовые армии занимают 66 / 52 / 55 при лидерстве 70 / 55 / 60
  (запас 4 / 3 / 5).
- Этап 03: кривая опыта героя формализована как
  `50 × level × (level + 1)` (ADR-025); суммарно до 10 уровня — 16 500 XP.
  Это фиксация уже заявленной в GDD прогрессии, а не нерф или бафф.

### Modding

- Зафиксированы правила: JSON + стабильные ID + `schemaVersion`,
  моды в `mods/<modId>/manifest.json`, без хардкода контента.
- Этап 03: манифест пака реализован полностью — `schemaVersion`, `id`, `version`,
  `gameVersionRange`, `dependencies`, `loadOrder`; неизвестное поле — ошибка.
- Этап 03: порядок загрузки детерминирован (зависимости → `loadOrder` → ID);
  переопределение core-ID разрешено правилом last-wins и попадает в журнал
  `ContentChange` (`Kind`, `Category`, `EntityId`, `SourcePack`, `ReplacedPack`).
- Этап 03: мод может переопределять `rules/*.json` целиком, добавлять записи
  и свои языковые файлы с префиксом ключей `modId.`.
- Этап 03: циклические и отсутствующие зависимости — ошибки merge,
  реестр не строится.

### Docs

- Все документы получили шапку (версия, статус, дата), оглавление и ссылки
  на связанные документы.
- Заведены `docs/KNOWN_ISSUES.md` и `docs/RELEASE_CHECKLIST.md`.
- Этап 03: `docs/CONTENT_SCHEMA.md`, `docs/MODDING.md`, `docs/LOCALIZATION.md`
  переписаны под фактическую реализацию (формы файлов, манифест, 26 языков).
- Этап 03: в `docs/ARCHITECTURE.md` добавлена схема конвейера и разделение
  адаптеров `DiskContentFileSystem` / `GodotContentFileSystem`.
- Этап 03: `docs/TESTING.md` описывает 262 теста, поддержку `InMemoryContentFileSystem`,
  `FileContentFileSystem`, `TestPacks`, `RealContent` и допуск на
  `CONTENT_RULE_UNKNOWN` для файлов правил этапа 04.
- Этап 03: `docs/KNOWN_ISSUES.md` закрывает KI-002/KI-010 и заводит KI-012
  (файлы правил этапа 04 пропускаются) и KI-013 (видимость `la` в настройках).

## [0.3.0] - 2026-11-13
### Added
- Full GDD v0.3, roadmap, MVP prompt.
- 7 resources, hex global/battle maps, day-based economy.
- 3 factions, unique hero classes, veterancy, exponential morale.
- Escape/ransom rules, settlement capture with auto-demolition.
### Changed
- Leadership: hard cap, no overload. Hero capacity = hero.Leadership.
- Hero does not participate directly in battle.
- Defend: +25% defense, no resists, no initiative penalty.
- Retaliation is automatic, not a player action.
### Removed
- Day/night, overload, direct hero combat, ammo in MVP.

## Связанные документы

- [docs/GDD.md](GDD.md) — дизайн-источник истины
- [docs/ROADMAP.md](ROADMAP.md) — этапы
- [docs/WORKLOG.md](WORKLOG.md) — история работ
- [docs/DECISIONS.md](DECISIONS.md) — принятые решения
- [docs/patchnotes/v0.3.0.md](patchnotes/v0.3.0.md) — патчноут версии
