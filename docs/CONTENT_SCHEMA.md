# Схема контента (CONTENT_SCHEMA)

| Параметр | Значение |
|---|---|
| Версия | 0.4.0 |
| Статус | Актуально для этапа 04: добавлены биомы, правила карты, тестовые карты |
| Дата | 2026-11-16 |
| Источник истины | [docs/GDD.md](GDD.md) §5, §8, §9, §14.3, §15, §18 |

## Оглавление

1. [Принципы](#1-принципы)
2. [Каталоги](#2-каталоги)
3. [Формы файлов](#3-формы-файлов)
4. [Стабильные ID](#4-стабильные-id)
5. [Общие поля](#5-общие-поля)
6. [Сущности](#6-сущности) (включая 6.11 тестовые карты)
7. [Правила](#7-правила)
8. [Локализация](#8-локализация)
9. [Правила валидации](#9-правила-валидации)
10. [Версионирование и миграции](#10-версионирование-и-миграции)
11. [Моды](#11-моды)
12. [Состояние реального контента](#12-состояние-реального-контента)
13. [Связанные документы](#13-связанные-документы)

## 1. Принципы

1. Никакого хардкода юнитов, фракций, зданий, заклинаний в C# (AGENTS.md, инвариант 3).
2. Каждая запись имеет стабильный строковый ID и человекочитаемое имя через ключ локализации.
3. Все файлы — UTF-8 JSON без комментариев и trailing commas, с обязательным `schemaVersion`.
4. Ссылки между записями — только по ID, не по имени файла и не по порядку.
5. Неизвестное поле — ошибка чтения, а не «попробуем прочитать».
6. Ошибки валидации содержат файл, ID записи и поле.
7. Схема позволяет добавить седьмого юнита только JSON + локализацией (GDD §19).
8. Числа в JSON — только целые. Дробный литерал (`1.5`) отклоняется загрузчиком.

## 2. Каталоги

```
game/content/core/
├── manifest.json          манифест пака
├── resources.json         список семи ресурсов
├── rules/                 battle, morale, economy, escape, ransom,
│                          veterancy, progression, biomes, map_generation
├── factions/              по файлу на фракцию
├── units/                 коллекции юнитов по фракциям
├── hero_classes/          по файлу на класс героя
├── heroes/                по файлу на стартового героя
├── abilities/             коллекции способностей
├── spells/                коллекции заклинаний
├── buildings/             коллекции зданий по фракциям
├── settlements/           по файлу на шаблон поселения
├── map_objects/           коллекции объектов карты
├── test_maps/             тестовые карты в формате layout (ADR-028)
└── localization/          languages.json + по файлу на язык
```

Каталог `game/content/core` — единственный источник core-контента (ADR-016).
Моды лежат отдельно в `mods/<modId>/` и не правят core-файлы.

## 3. Формы файлов

### 3.1. Манифест пака

`manifest.json` — единственный обязательный файл в корне пака.

```json
{
  "schemaVersion": 1,
  "id": "core",
  "version": "0.3.0",
  "gameVersionRange": ">=0.3.0 <0.4.0",
  "dependencies": [],
  "loadOrder": 0
}
```

| Поле | Тип | Обязательно | Правило |
|---|---|---|---|
| `schemaVersion` | int | да | поддерживаемая версия формы манифеста |
| `id` | string | да | `lower_snake_case`, совпадает с `modId` у модов |
| `version` | string | да | SemVer |
| `gameVersionRange` | string | да | диапазон совместимых версий игры |
| `dependencies` | string[] | да | ID паков, которые грузятся раньше |
| `loadOrder` | int | да | порядок при равных зависимостях, целое ≥ 0 |

### 3.2. Одиночная сущность

Каталоги `factions/`, `hero_classes/`, `heroes/`, `settlements/` содержат
один JSON-объект на файл. У него есть собственные `schemaVersion` и `id`.

```json
{
  "schemaVersion": 1,
  "id": "core.faction.brass",
  "nameKey": "faction.brass.name",
  "descriptionKey": "faction.brass.desc"
}
```

### 3.3. Коллекция

Файлы `resources.json`, `units/*.json`, `abilities/*.json`, `spells/*.json`,
`buildings/*.json`, `map_objects/*.json` — конверт с массивом.

```json
{
  "schemaVersion": 1,
  "id": "core.units.brass",
  "units": [
    { "id": "core.unit.brass.militia" }
  ]
}
```

| Каталог | Поле-массив |
|---|---|
| `resources.json` | `resources` |
| `units/` | `units` |
| `abilities/` | `abilities` |
| `spells/` | `spells` |
| `buildings/` | `buildings` |
| `map_objects/` | `objects` |

Важно: элементы коллекции **не содержат** собственного `schemaVersion`.
Версия задаётся конвертом файла. Это осознанное решение этапа 03:
конверт — единица версионирования, запись — единица merge.

### 3.4. Файл правила

`rules/<name>.json` — одиночный объект с `schemaVersion` и `id`.
Имя файла (`battle`, `morale`, …) определяет тип правила.
Неизвестное имя файла правила — ошибка `CONTENT_RULE_UNKNOWN` (ADR-030).
Поддерживаются: `battle`, `morale`, `economy`, `escape`, `ransom`, `veterancy`,
`progression`, `biomes`, `map_generation`.

### 3.5. Языковой файл

`localization/<code>.json` и `localization/languages.json` описаны в
[docs/LOCALIZATION.md](LOCALIZATION.md) и в §8.

## 4. Стабильные ID

Формат: `<pack>.<type>.<group>.<name>`, сегменты в `lower_snake_case`.

| Пример | Значение |
|---|---|
| `core.faction.brass` | фракция Латунный Предел |
| `core.unit.brass.militia` | Ополченец |
| `core.unit.ash.bone_servant` | Костяной служка |
| `core.hero.ash.selha` | Герой Пепельного Ковена |
| `core.ability.brass.iron_order` | «Железный приказ» |
| `core.spell.ash.dark_ray` | «Луч тьмы» |
| `core.building.brass.brass_townhall` | Латунная ратуша |
| `core.map_object.mine_crystal` | Шахта кристаллов |

Правила:

1. ID неизменяем после публикации. Переименование = новая запись + миграция сейвов.
2. ID не содержит локализованного текста.
3. Мод обязан использовать префикс своего `modId`, например `mymod.unit.brass.grenadier`.
4. Дубликат ID в пределах одного пака — ошибка загрузки (`CONTENT_ID_DUPLICATE`).
5. Дубликат ID между паками — не ошибка: последний пак переопределяет запись
   (last-wins), каждое переопределение логируется (см. §11).
6. ID ресурсов — исключение: это короткие имена (`gold`, `wood`, `ore`, `crystal`,
   `sulfur`, `mercury`, `essence`), а не точечная нотация.

## 5. Общие поля

### 5.1. Ключи локализации

Все отображаемые строки — только ключи:

| Поле | Назначение |
|---|---|
| `nameKey` | имя сущности |
| `descriptionKey` | описание сущности |

Литеральный текст в этих полях запрещён. Валидатор проверяет наличие ключа
в `ru` и `en`; для остальных языков пропуск — предупреждение.

### 5.2. Ресурсы

Карты ресурсов — объект `{ "<resourceId>": <int> }`.
Допустимы только семь ID из `resources.json`. Суммы — целые ≥ 0.
Отрицательные значения запрещены; валидатор сообщает
`CONTENT_RESOURCE_UNKNOWN` и `CONTENT_RESOURCE_AMOUNT`.

### 5.3. Статистика героя (`StatsDto`)

```json
{
  "attack": 2,
  "defense": 2,
  "spellpower": 2,
  "knowledge": 2,
  "leadership": 60,
  "logistics": 4
}
```

Все шесть полей обязательны. `leadership` > 0, остальные ≥ 0.

## 6. Сущности

### 6.1. Фракция (`factions/*.json`)

```json
{
  "schemaVersion": 1,
  "id": "core.faction.brass",
  "nameKey": "faction.brass.name",
  "descriptionKey": "faction.brass.desc",
  "colors": {
    "primary": "#B8863B",
    "secondary": "#2C3E50",
    "accent": "#C0392B",
    "background": "#1C2833",
    "frame": "#B8863B"
  },
  "maxSettlementLevel": 3,
  "heroClassId": "core.hero_class.brass.marshal",
  "tags": ["living", "disciplined"]
}
```

| Поле | Тип | Правило |
|---|---|---|
| `id` | string | `core.faction.<name>` |
| `nameKey`, `descriptionKey` | string | ключи локализации |
| `colors` | object | пять HEX-цветов: `primary`, `secondary`, `accent`, `background`, `frame` |
| `maxSettlementLevel` | int | 2…4, соответствует GDD §2.2 |
| `heroClassId` | string | существующий класс героя этой фракции |
| `tags` | string[] | непустые уникальные строки |

### 6.2. Юнит (`units/*.json`, элемент `units[]`)

```json
{
  "id": "core.unit.brass.militia",
  "factionId": "core.faction.brass",
  "tier": 1,
  "unitClass": "infantry",
  "movementType": "foot",
  "damageType": "physical",
  "leadershipCost": 1,
  "attack": 4,
  "defense": 5,
  "initiative": 9,
  "speed": 4,
  "damageMin": 2,
  "damageMax": 4,
  "critChancePercent": 5,
  "critDamagePercent": 150,
  "hitPoints": 12,
  "resistances": { "physical": 0, "magical": 0, "poison": 0, "fire": 0, "firearm": 0 },
  "abilities": ["core.ability.brass.formation"],
  "recruitCost": { "gold": 25 },
  "nameKey": "unit.brass.militia.name",
  "descriptionKey": "unit.brass.militia.desc"
}
```

| Поле | Тип | Правило |
|---|---|---|
| `id` | string | `core.unit.<faction>.<name>` |
| `factionId` | string | существующая фракция |
| `tier` | int | 1…5 |
| `unitClass` | string | класс юнита (например, `infantry`, `shooter`, `cavalry`, `beast`) |
| `movementType` | string | `foot`, `shooter`, `cavalry`, `beast` |
| `damageType` | string | `physical`, `magical`, `poison`, `fire`, `firearm` |
| `leadershipCost` | int | > 0 |
| `attack`, `defense`, `initiative`, `speed` | int | ≥ 0 |
| `damageMin`, `damageMax` | int | ≥ 0, `damageMin ≤ damageMax` |
| `critChancePercent` | int | 0…75 |
| `critDamagePercent` | int | 100…300 |
| `hitPoints` | int | > 0 |
| `resistances` | object | пять полей, каждое 0…100 |
| `abilities` | string[] | существующие способности |
| `recruitCost` | resource map | непустая карта ресурсов |
| `promotionIds` | string[] | зарезервировано под M2; на этапе 03 предупреждение `CONTENT_PROMOTION_NOT_IMPLEMENTED` |
| `ammo` | int | зарезервировано; в MVP не влияет, валидатор предупреждает при > 0 |

`health` в реальном JSON называется `hitPoints`; `resists` — `resistances`.
Это расхождение с ранним черновиком зафиксировано здесь как фактическая схема.

### 6.3. Класс героя (`hero_classes/*.json`)

```json
{
  "schemaVersion": 1,
  "id": "core.hero_class.root.pathfinder",
  "factionId": "core.faction.root",
  "nameKey": "hero_class.root.pathfinder.name",
  "descriptionKey": "hero_class.root.pathfinder.desc",
  "startingStats": { "attack": 2, "defense": 2, "spellpower": 2, "knowledge": 2, "leadership": 60, "logistics": 4 },
  "statWeights": { "attack": 15, "defense": 15, "spellpower": 15, "knowledge": 10, "leadership": 25, "logistics": 20 },
  "passiveAbilityId": "core.ability.class.trails",
  "commandAbilityId": "core.ability.hero.root_call",
  "spellId": "core.spell.root.thorns"
}
```

| Поле | Правило |
|---|---|
| `factionId` | существующая фракция |
| `startingStats` | все шесть характеристик, `leadership` > 0 |
| `statWeights` | все шесть весов ≥ 0, сумма > 0 |
| `passiveAbilityId` | существующая пассивная способность |
| `commandAbilityId` | существующая командная способность |
| `spellId` | существующее заклинание |

### 6.4. Герой (`heroes/*.json`)

```json
{
  "schemaVersion": 1,
  "id": "core.hero.ash.selha",
  "factionId": "core.faction.ash",
  "classId": "core.hero_class.ash.archivist",
  "nameKey": "hero.ash.selha.name",
  "descriptionKey": "hero.ash.selha.desc",
  "level": 1,
  "xp": 0,
  "skillPoints": 0,
  "stats": { "attack": 1, "defense": 2, "spellpower": 4, "knowledge": 3, "leadership": 55, "logistics": 2 },
  "army": [
    { "unitId": "core.unit.ash.bone_servant", "count": 10 }
  ],
  "equipment": { "head": null, "neck": null, "leftHand": null, "rightHand": null,
    "shoulders": null, "torso": null, "back": null, "belt": null, "legs": null,
    "feet": null, "ring1": null, "ring2": null },
  "artifacts": []
}
```

| Поле | Правило |
|---|---|
| `factionId`, `classId` | существуют; класс принадлежит фракции |
| `level` | ≥ 1 |
| `xp` | ≥ 0 |
| `skillPoints` | ≥ 0 |
| `stats` | все шесть характеристик |
| `army` | 1…5 слотов, юниты уникальны, принадлежат фракции, суммарное лидерство ≤ `stats.leadership` |
| `equipment` | 12 слотов, значения — ID артефактов или `null` |
| `artifacts` | строковые ID |

Стартовая армия героя проверяется по жёсткому лидерству (ADR-006).

### 6.5. Способность (`abilities/*.json`, элемент `abilities[]`)

```json
{
  "id": "core.ability.hero.iron_order",
  "nameKey": "ability.hero.iron_order.name",
  "descriptionKey": "ability.hero.iron_order.desc",
  "kind": "command",
  "trigger": "onCommand",
  "target": "allAllies",
  "usesPerBattle": 1,
  "durationRounds": 2,
  "effects": [
    { "type": "defenseModifier", "value": 3 }
  ]
}
```

| Поле | Правило |
|---|---|
| `kind` | `passive`, `active`, `command` |
| `trigger` | непустая строка; известные значения проверяются валидатором |
| `target` | необязательная строка цели |
| `usesPerBattle` | ≥ 0, если задано |
| `cooldownRounds`, `durationRounds`, `radius`, `range` | ≥ 0, если заданы |
| `effects` | непустой массив |

`AbilityEffectDto`:

| Поле | Назначение |
|---|---|
| `type` | обязательный тип эффекта |
| `value` | целое значение |
| `unitId`, `count` | призыв юнита |
| `damageType`, `min`, `max` | урон |
| `maxTier`, `terrain`, `maxCountPerStack` | ограничения эффекта |
| `durationRounds` | длительность эффекта |
| `minHexes` | минимальная дистанция |

Свободные скрипты в эффектах запрещены: только известные типы.

### 6.6. Заклинание (`spells/*.json`, элемент `spells[]`)

```json
{
  "id": "core.spell.root.thorns",
  "nameKey": "spell.root.thorns.name",
  "descriptionKey": "spell.root.thorns.desc",
  "school": "magical",
  "manaCost": 8,
  "target": "enemyStack",
  "durationRounds": 1,
  "effects": [
    { "type": "damage", "damageType": "magical", "min": 6, "max": 10 },
    { "type": "root", "durationRounds": 1 }
  ]
}
```

| Поле | Правило |
|---|---|
| `school` | непустая школа магии; на этапе 03 известна `magical` |
| `manaCost` | ≥ 0 |
| `target` | непустая строка |
| `durationRounds` | ≥ 0, если задано |
| `effects` | непустой массив |

### 6.7. Здание (`buildings/*.json`, элемент `buildings[]`)

```json
{
  "id": "core.building.brass.muster_ground",
  "factionId": "core.faction.brass",
  "nameKey": "building.brass.muster_ground.name",
  "descriptionKey": "building.brass.muster_ground.desc",
  "requiredLevel": 1,
  "cost": { "gold": 500, "wood": 5 },
  "recruitment": { "unitId": "core.unit.brass.militia", "growthPerDayMilli": 1000, "stockCap": 14 }
}
```

| Поле | Правило |
|---|---|
| `factionId` | существующая фракция |
| `requiredLevel` | 1…`maxSettlementLevel` фракции |
| `cost`, `dailyIncome` | карты ресурсов, целые ≥ 0 |
| `recruitment` | необязательно; юнит существует и принадлежит фракции |
| `growthPerDayMilli` | > 0 (промилле) |
| `stockCap` | > 0 |
| Имя здания | уникально в рамках фракции (`nameKey`) |

### 6.8. Поселение (`settlements/*.json`)

```json
{
  "schemaVersion": 1,
  "id": "core.settlement.brass",
  "factionId": "core.faction.brass",
  "nameKey": "settlement.brass.name",
  "descriptionKey": "settlement.brass.desc",
  "maxLevel": 3,
  "levels": [
    { "level": 1, "buildSlots": 4 },
    { "level": 2, "buildSlots": 6 },
    { "level": 3, "buildSlots": 8 }
  ],
  "defaultBuildings": ["core.building.brass.brass_townhall"],
  "upgrades": [
    { "fromLevel": 1, "toLevel": 2, "cost": { "gold": 2500, "ore": 15, "wood": 10 }, "days": 1 }
  ]
}
```

| Поле | Правило |
|---|---|
| `factionId` | существующая фракция |
| `maxLevel` | 2…4, совпадает с `faction.maxSettlementLevel` |
| `levels` | по одному уровню 1…`maxLevel`, `buildSlots` возрастает |
| `defaultBuildings` | непусто, здания существуют и принадлежат фракции, число ≤ слотов уровня 1 |
| `upgrades` | шаги `fromLevel` → `fromLevel + 1`, без пропусков и дублей |
| `cost` | карта ресурсов, целые ≥ 0 |
| `days` | ≥ 0 |

### 6.9. Объект карты (`map_objects/*.json`, элемент `objects[]`)

```json
{
  "id": "core.map_object.mine_gold",
  "type": "mine",
  "nameKey": "map_object.mine_gold.name",
  "descriptionKey": "map_object.mine_gold.desc",
  "guarded": true,
  "incomePerDay": { "gold": 100 }
}
```

| Поле | Правило |
|---|---|
| `type` | `mine`, `dwelling`, `echoCache`, `patrol` |
| `guarded` | bool |
| `incomePerDay` | карта ресурсов; для шахт непуста и > 0 |
| `factionId` | для жилищ — существующая фракция |
| `recruitment` | для жилищ — юнит существует и принадлежит фракции |
| `options` | для эхо-схронов — непустой массив опций |
| `heroId`, `army` | для патрулей — герой и армия согласованы |

`MapObjectOptionDto`:

| Поле | Назначение |
|---|---|
| `id`, `nameKey`, `descriptionKey` | идентификация опции |
| `requiresBattle` | bool |
| `cost`, `reward` | карты ресурсов |
| `delayedRewardDays` | ≥ 0, если задано |
| `delayedReward` | карта ресурсов |

Награды могут содержать `reputation` (не ресурс, а награда).

### 6.10. Ресурс (`resources.json`, элемент `resources[]`)

```json
{ "id": "gold", "nameKey": "resource.gold.name", "role": "currency" }
```

| Поле | Правило |
|---|---|
| `id` | один из семи: `gold`, `wood`, `ore`, `crystal`, `sulfur`, `mercury`, `essence` |
| `role` | `currency` или `material`; ровно один `currency` — `gold` |
| `nameKey` | ключ локализации |

### 6.11. Тестовая карта (`test_maps/*.json`)

Карта задаётся строками `layout`: один символ — один гекс, `legend` связывает символ
с ID биома (ADR-028). Координаты — offset (`col`, `row`), схема `odd-r`.

```json
{
  "schemaVersion": 1,
  "id": "core.test_map.small_8x8",
  "nameKey": "test_map.small_8x8.name",
  "width": 8, "height": 8, "offsetType": "odd-r",
  "legend": { "G": "core.biome.grass", "R": "core.biome.road" },
  "layout": ["GGGGGGGG", "..."],
  "objects": [ { "col": 6, "row": 0, "objectId": "core.map_object.mine_gold" } ],
  "startPositions": { "player": { "col": 1, "row": 5, "factionId": "core.faction.brass", "heroId": "core.hero.brass.idra_veyn" } },
  "testCases": [ { "id": "MS-01", "type": "movementCost", "from": { "col": 1, "row": 5 }, "to": { "col": 2, "row": 5 }, "expectedCost": 2 } ]
}
```

| Поле | Правило |
|---|---|
| `width`, `height` | совпадают с размерами `layout` |
| `layout` | `height` строк по `width` символов, каждый символ есть в `legend` |
| `legend` | не пуста; значения — существующие ID биомов |
| `objects[]` | `objectId` — существующий объект карты; координаты внутри карты |
| `startPositions` | роль → позиция внутри карты, фракция, необязательный герой |
| `testCases[].type` | `movementCost`, `unreachable`, `collision`, `fogRadius`, `fullConnectivity`, `objectCount`, `minStartDistance` |
| `testCases[].id` | непустой, уникален в пределах карты |

Эталонные стоимости (`expectedCost`) считаются по шкале ADR-034; при смене стоимостей
биомов их нужно пересчитать (см. [docs/BALANCE_MAP.md](BALANCE_MAP.md)).

## 7. Правила

Все правила — целочисленные. Источник чисел — `game/content/core/rules/*.json`.

### 7.1. `rules/battle.json`

`fieldWidth` (13), `fieldHeight` (9), `armySlots` (5), `maxArmySlots` (7),
`heroOnBattlefield` (false), `defendDefenseBonusPercent` (25),
`defendResistBonusPercent` (0), `defendInitiativePenalty` (0),
`retaliationPerRound` (1), `retaliationIsAction` (false),
`waitMovesToEndOfRound`, `waitAffectsNextRound`,
`attackDefenseModifierPerPoint` (50), `attackDefenseModifierMin` (300),
`attackDefenseModifierMax` (4000), `critChanceCapPercent` (75),
`critDamageCapPercent` (300), `damageTypes` (пять типов).

### 7.2. `rules/morale.json`

`minLevel` (−4), `maxLevel` (4), `extraActionChanceCapPercent` (35),
`statModifierCapPercent` (16), `maxExtraActionsPerUnitPerRound` (1),
`immuneTags` (`undead`, `construct`), `levels[]` с `level`, `nameKey`,
`chancePercent`, `statModifierPercent`, `sources` с модификаторами морали.

### 7.3. `rules/economy.json`

`baseSettlementIncomePerDay`, `growthCapDays` (14),
`growthPerDayMilliByTier` (тиры 1…5), `captureDeletesBuildings` (true),
`captureResetsLevel` (1), `captureSwitchesTemplateToCaptor` (true),
`dailyTickOrder[]` (фиксированный порядок дневного тика).

### 7.4. `rules/escape.json`

`baseChancePercent` (65), `heroLogisticsMultiplier` (3),
`enemyLogisticsMultiplier` (2), `minChancePercent` (20),
`maxChancePercent` (90), `artifactsLostMin` (1), `artifactsLostMax` (3),
`captivityDays` (5), `armyLostOnEscape` (true),
`defenderCanEscape` (false), `herolessArmyLossPercent` (40).

### 7.5. `rules/ransom.json`

`unitPriceMultiplierPercent` (100), `heroLevelCost` (1000),
`artifactCost` (100), `keepsArmy` (true), `keepsArtifacts` (true),
`movePointsAfter` (0), `defenderCanRansom` (false).

### 7.6. `rules/veterancy.json`

`countsDefeatedStacks` (true), `thresholds` (`0, 2, 5, 10, 18`),
`rankNameKeys` (пять ключей), `perRankAboveFirst` с `attack`, `defense`,
`critChancePercent`, `critDamagePercent`.

### 7.7. `rules/progression.json`

```json
{
  "schemaVersion": 1,
  "id": "core.rules.progression",
  "unlimitedLevel": true,
  "xpToNextFormula": {
    "type": "quadratic",
    "coefficient": 50,
    "expression": "50 * level * (level + 1)"
  },
  "skillPointsPerLevel": 1,
  "scenarioSoftCap": 10
}
```

`XpToNext(level) = coefficient × level × (level + 1)` (ADR-025).
`scenarioSoftCap` задаётся сценарием; 0 или меньше — без предела.
`skillPointsPerLevel` — очки навыка за уровень.

### 7.8. `rules/biomes.json`

```json
{ "schemaVersion": 1, "id": "core.rules.biomes", "biomes": [
  { "id": "core.biome.grass", "nameKey": "biome.grass.name", "tileId": "hex_grass",
    "passable": true, "movementCostPerHex": 2, "visibilityRange": 2 } ] }
```

| Поле | Правило |
|---|---|
| `id` | стабильный ID `core.biome.*`, уникален |
| `nameKey`, `tileId` | ключ локализации и ID тайла спрайта, обязательны |
| `passable` | проходим ли биом для наземных отрядов |
| `movementCostPerHex` | проходимый биом: не менее 2, дорога — ровно 1 (ADR-034); непроходимый — 999 |
| `visibilityRange` | не менее 0; фактический обзор — максимум из него и `fogVisibilityRadius` |
| `flyerPassable`, `flyerMovementCost` | необязательны; стоимость для летающих не менее 2 |
| `road` | необязательный флаг дороги |

Коды: `CONTENT_BIOMES_EMPTY`, `CONTENT_BIOME_DUPLICATE`, `CONTENT_BIOME_NAME_KEY`,
`CONTENT_BIOME_TILE`, `CONTENT_BIOME_MOVEMENT_COST`, `CONTENT_BIOME_PASSABLE_COST`,
`CONTENT_BIOME_ROAD_COST`, `CONTENT_BIOME_FLYER_COST`, `CONTENT_BIOME_VISIBILITY`.

### 7.9. `rules/map_generation.json`

| Поле | Правило |
|---|---|
| `gridWidth`, `gridHeight` | размеры карты, 48×48 в MVP |
| `offsetType` | `odd-r` или `even-r` |
| `defaultBiome` | ID биома из `biomeDistribution` |
| `borderBiome` | необязательный ID непроходимого биома граничного кольца («Шов», ADR-036); не входит в `biomeDistribution` |
| `biomeDistribution` | ID биома → вес; сумма весов 100 |
| `playerStartPositions` | `minDistanceBetweenPlayers`, `distanceFromBorder` |
| `objectCounts` | `mines`, `dwellings`, `echoCaches`, `patrols` (не менее 0) |
| `armyMovementPointsPerDay` | очки движения отряда без героя, не менее 1; очки героя — `12 + Логистика` (ADR-034) |
| `fogVisibilityRadius`, `fogExploredRadius` | радиусы тумана, не менее 0 |

Коды: `CONTENT_MAP_WIDTH`, `CONTENT_MAP_HEIGHT`, `CONTENT_MAP_OFFSET_TYPE`,
`CONTENT_MAP_DISTRIBUTION_SUM`, `CONTENT_MAP_DISTRIBUTION_VALUE`, `CONTENT_MAP_BORDER_PASSABLE`,
`CONTENT_MAP_BORDER_IN_DISTRIBUTION`, `CONTENT_MAP_START_DISTANCE`,
`CONTENT_MAP_BORDER_DISTANCE`, `CONTENT_MAP_OBJECT_COUNT`, `CONTENT_MAP_OBJECT_TYPE`,
`CONTENT_MAP_ARMY_MP`, `CONTENT_MAP_FOG_VISIBILITY`, `CONTENT_MAP_FOG_EXPLORED`.

## 8. Локализация

- Ключи: `<domain>.<type>.<group>.<name>[.<field>]`, например
  `unit.brass.militia.name`, `faction.brass.name`, `ui.menu.new_game`.
- Файлы: `game/content/core/localization/languages.json` и `<code>.json`.
- Значения — только строки; форматирование — через именованные плейсхолдеры.
- Отсутствующий ключ в рантайме: возвращается сам ключ и пишется `Warning`,
  категория `Loc`.
- Наборы ключей `ru` и `en` обязаны совпадать; расхождение — ошибка.
- В остальных языках лишние ключи запрещены, пропуски — предупреждение (ADR-026).
- Подробности — [docs/LOCALIZATION.md](LOCALIZATION.md).

## 9. Правила валидации

Валидатор (`SimRunner validate`) проверяет:

1. JSON синтаксически валиден, UTF-8, без комментариев и trailing commas.
2. `schemaVersion` поддерживается (`1`); неизвестное поле — ошибка.
3. Дубликаты ID в пределах пака; после merge — last-wins с журналом.
4. Все ссылки существуют: фракция, юнит, класс, способность, заклинание,
   здание, герой, объект карты.
5. Числовые диапазоны: тир, резисты, HP, лидерство, криты, стоимости, длительности.
6. Локализация: паритет `ru`/`en`, отсутствие лишних ключей, статусы языков.
7. Стартовые армии героев и патрулей соответствуют лидерству и слотам.
8. `maxLevel` поселения соответствует фракции, шаги улучшений непрерывны.
9. Имена зданий уникальны в рамках фракции.
10. Ровно семь ресурсов, `gold` — единственная валюта.
11. Зарезервированные поля MVP (`ammo`, `promotionIds`) не влияют на игру
    и дают предупреждение при ненулевом значении.
12. Биомы, правила генерации карты и тестовые карты (разделы 6.11, 7.8, 7.9).
13. Переопределение core-ID модом без `overridesCore` — ошибка
    `CONTENT_CORE_OVERRIDE_FORBIDDEN` (ADR-031).

Диагностика содержит код, серьёзность, путь файла, ID сущности и поле.
Коды сгруппированы префиксами: `CONTENT_*` — структура и ссылки,
`LOC_*` — локализация. Полный список кодов — в `ContentValidator.cs`.

## 10. Версионирование и миграции

- `schemaVersion` — целое, увеличивается при несовместимом изменении формы данных.
- Совместимое добавление необязательного поля версию не меняет.
- Загрузчик поддерживает текущую версию (`1`); более старые — через миграцию.
- Миграция контента выполняется только явной командой и логируется.
- Сейвы хранят версию контента, с которым созданы (см. SAVE_FORMAT).

## 11. Моды

- Мод — каталог `mods/<modId>/` с `manifest.json` и контентом.
- Загрузка: `core` → моды по зависимостям, затем `loadOrder`, затем ID.
- Одинаковые ID переопределяются последним паком (last-wins);
  каждое переопределение пишется в журнал (`ContentChange`).
- Переопределить ID из core можно только с `"overridesCore": true` в манифесте
  (ADR-031); иначе ошибка `CONTENT_CORE_OVERRIDE_FORBIDDEN`.
- Мод не может менять код, только данные (до M4).
- Подробности — [docs/MODDING.md](MODDING.md).

## 12. Состояние реального контента

На 2026-11-16 `game/content/core` содержит:

| Категория | Количество |
|---|---:|
| Фракции | 3 |
| Юниты | 15 |
| Классы героев | 3 |
| Герои | 3 |
| Способности | 21 |
| Заклинания | 3 |
| Здания | 18 |
| Поселения | 3 |
| Объекты карты | 14 |
| Ресурсы | 7 |
| Языки | 26 |
| Ключи `en` / `ru` | 238 / 238 |
| Загруженные правила | 9 (добавлены `biomes`, `map_generation`) |
| Тестовые карты | 3 (`small_8x8`, `medium_16x16`, `full_48x48`) |

Ожидается, что реальный контент проходит валидатор с exit 0 без диагностики
`CONTENT_RULE_UNKNOWN`. Счётчики сущностей выводит `SimRunner validate`; после этапа 04
их нужно сверить с этой таблицей (сборка в среде автора правок не запускалась).

## 13. Связанные документы

- [docs/GDD.md](GDD.md) §5, §8, §9, §15, §18
- [docs/ARCHITECTURE.md](ARCHITECTURE.md) §8
- [docs/MODDING.md](MODDING.md)
- [docs/LOCALIZATION.md](LOCALIZATION.md)
- [docs/TESTING.md](TESTING.md)
- [docs/DECISIONS.md](DECISIONS.md) — ADR-016, ADR-024, ADR-025, ADR-026
