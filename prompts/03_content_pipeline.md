ЭТАП 03: КОНТЕНТ-ПАЙПЛАЙН.
Реализовать только загрузку, merge, валидацию, локализацию и проверку существующего контента.
Бой, карту, UI, ИИ НЕ реализовывать — это этапы 04–08.

Прочитай:
- AGENTS.md
- docs/GDD.md, секции 2, 5, 6, 8, 9, 18, 19
- docs/ARCHITECTURE.md, docs/TDD.md, docs/CONTENT_SCHEMA.md
- docs/DECISIONS.md (ADR-023, ADR-024, ADR-025, ADR-026, если есть)
- docs/BALANCE.md, docs/WORKLOG.md (W-0003, W-0004, W-0005, если есть)
- docs/LOCALIZATION.md

======================================================================
ВАЖНО: БОЛЬШАЯ ЧАСТЬ КОНТЕНТА УЖЕ ПОДГОТОВЛЕНА ВРУЧНУЮ
======================================================================

Следующее уже создано вручную в game/content/core и НЕ должно пересоздаваться:
- manifest.json
- resources.json (7 ресурсов)
- rules/ (battle, morale, economy, escape, ransom, veterancy, progression)
- factions/ (brass, ash, root)
- units/ (brass, ash, root — 15 юнитов по GDD §18)
- hero_classes/ (marshal, archivist, pathfinder)
- heroes/ (по герою на фракцию)
- abilities/ (юниты + hero_commands + class_passives)
- spells/ (heroes)
- buildings/ (brass, ash, root)
- settlements/ (brass, ash, root)
- map_objects/ (mines, dwellings, echo_cache, patrols)
- localization/ — ВСЕ языковые файлы:
  ru.json, en.json (complete),
  es, de, pt, it, fr, la, uk, be, rue, cu, pl, cs, sk, szl, csb,
  hsb, dsb, sl, bg, mk, sr, hr, bs, cnr (draft/experimental),
  а также languages.json.

ЗАПРЕЩЕНО:
- перезаписывать существующие JSON без явной структурной причины;
- регенерировать переводы;
- переписывать художественные описания;
- менять числа баланса без нового ADR;
- удалять поля, добавленные вручную;
- менять порядок ключей и форматирование, если это не исправляет ошибку парсинга.

РАЗРЕШЕНО (и нужно сделать):
- добавить недостающие структурные поля в существующие JSON (например, `status`,
  `fallback` в языковых файлах), если их нет;
- исправить битый JSON;
- привести к единой схеме (schemaVersion=1, camelCase), где это не меняет смысла;
- добавить недостающие записи, если их явно не хватает по GDD v0.3;
- зафиксировать найденные структурные проблемы в KNOWN_ISSUES.md.

======================================================================

РЕШЕНИЯ, КОТОРЫЕ НУЖНО ЗАФИКСИРОВАТЬ В DECISIONS.md (если ещё не зафиксированы):

ADR-024: валидатор контента — часть Game.Core.Content, CLI в Game.SimRunner.
ADR-025: кривая опыта героя XpToNext(level) = 50 * level * (level + 1),
         +1 очко навыка за уровень, soft cap 10 задаётся сценарием.
ADR-026: локализация и список языков — languages.json как источник списка,
         статусы complete/draft/experimental, обязательные языки ru/en,
         фолбэк, requiresFontReview для la/cu/rue/szl/csb/hsb/dsb/cnr.

Если эти ADR уже есть — проверить и синхронизировать, не дублировать.
Обновить GDD.md §9 и BALANCE.md под ADR-025 (если ещё не обновлены).

=== 1. СХЕМЫ И DTO ===
Создать src/Game.Core/Content/Dto для сущностей:
Faction, Unit, HeroClass, Hero, Ability, Spell, Building, SettlementTemplate,
MapObject, ResourceDefinition, Rules (battle, morale, economy, escape, ransom,
veterancy, progression), LocalizationTable, ModManifest, LanguagesManifest.

Требования:
- System.Text.Json, без новых NuGet-зависимостей.
- camelCase, строгий режим: неизвестные поля = ошибка, комментарии и trailing commas запрещены.
- Только целые числа. Дробные литералы в JSON = ошибка валидации.
- Каждый файл: schemaVersion (int, текущая = 1) и id.
- ID: точечная нотация, нижний регистр, core.unit.brass.militia.
- Все отображаемые строки — только ключи локализации, литералы запрещены.
- DTO должны соответствовать существующим JSON файлам в game/content/core.
  Если поле есть в JSON — оно должно быть в DTO.
  Если поля в JSON нет — добавить его в DTO с Nullable или значением по умолчанию.

Канонический пример юнита (использовать как образец формы, сверять с реальным содержимым):
{
  "schemaVersion": 1,
  "id": "core.unit.brass.militia",
  "factionId": "core.faction.brass",
  "tier": 1,
  "unitClass": "infantry",
  "movementType": "foot",
  "damageType": "physical",
  "leadershipCost": 1,
  "attack": 4, "defense": 5, "initiative": 9, "speed": 4,
  "damageMin": 2, "damageMax": 4,
  "critChancePercent": 5, "critDamagePercent": 150,
  "hitPoints": 12,
  "resistances": { "physical": 0, "magical": 0, "poison": 0, "fire": 0, "firearm": 0 },
  "abilities": ["core.ability.brass.formation"],
  "recruitCost": { "gold": 25 },
  "nameKey": "unit.brass.militia.name",
  "descriptionKey": "unit.brass.militia.desc"
}

=== 2. ЗАГРУЗЧИК, MERGE, РЕЕСТР ===
- IContentFileSystem: абстракция чтения файлов, без прямого IO в Core.
- ContentLoader: читает пак, парсит, отдаёт DTO.
- ContentMerge: объединение паков по ID, last-wins, операции add/replace.
- ContentRegistry: итоговый словарь id → определение, типизированный доступ.
- ModManifest: id, version, schemaVersion, gameVersionRange, dependencies, loadOrder.
- LanguagesManifest: парсинг languages.json, список языков со статусами и fallback.
- LocalizationLoader: загрузка всех языковых файлов с учётом статуса.
- Порядок загрузки детерминированный: топологическая сортировка зависимостей,
  затем loadOrder, затем id по алфавиту. Циклы и отсутствующие зависимости = ошибка.
- Каждый override логировать: пак-источник, пак-получатель, id.

=== 3. ВАЛИДАТОР ===
ContentValidator возвращает список ошибок с указанием файла, поля и id.
Проверки обязательны:
- уникальность ID во всех паках после merge;
- все ссылки резолвятся: factionId, abilities, spells, buildings, heroClassId, mapObject;
- schemaVersion поддерживается;
- tier 1..5, leadershipCost > 0, hitPoints > 0;
- damageMin <= damageMax, все числа целые и неотрицательные где применимо;
- critChancePercent 0..75, critDamagePercent 100..300;
- резисты 0..100, только пять типов: physical, magical, poison, fire, firearm;
- movementType и damageType — валидные enum-значения;
- maxLevel поселения 2..4, соответствует фракции;
- названия зданий уникальны в рамках фракции;
- веса характеристик heroClass: только валидные HeroStatKind, сумма > 0;
- ресурсы: ровно 7 видов, gold — единая валюта;
- локализация: проверка описана в секции 4;
- отсутствие охраняемых имён — не проверять семантически, но добавить чек-лист в IP_SAFETY.md.

Ошибки должны быть пригодны для исправления человеком: путь, поле, ожидание, факт.

ВАЖНО: при запуске validate на существующем контенте в game/content/core
валидатор должен пройти с exit 0. Если существующий контент не проходит валидацию —
это ошибка валидатора, а не контента. Исправлять валидатор, а не переписывать JSON.
Исключение: если в существующем JSON найдена реальная структурная ошибка
(битый JSON, неверный schemaVersion, отсутствующее обязательное поле) —
исправить её и записать в WORKLOG.

=== 4. ЛОКАЛИЗАЦИЯ ===
- LocalizationTable: один экземпляр на язык.
- Ключи: unit.brass.militia.name, faction.brass.name, hero_class.brass.marshal.name,
  building.brass.brass_townhall.name, ability.brass.formation.name, map_object.mine_gold.name.
- Никаких строк в контенте напрямую.
- Отсутствующий ключ в рантайме логируется как Warning и возвращает ключ, не пустую строку.

Расширенный список языков:
- Источник: game/content/core/localization/languages.json.
- Каждый языковой файл: schemaVersion, id, language, status, fallback, entries.
- Статусы:
  - complete: 100% покрытие обязательно;
  - draft: пропуски = предупреждения;
  - experimental: пропуски = предупреждения, скрыт в настройках.
- Обязательные языки для 100% проверки: ru, en.
- Ключи в ru и en должны совпадать на 100%. Несоответствие = ошибка.
- Для остальных языков:
  - все ключи, которые есть в файле, должны быть также в en (лишних ключей нет);
  - отсутствующие ключи — предупреждение, не ошибка;
  - не переписывать художественные переводы.
- Фолбэк: выбранный язык → `fallback` из языкового файла → глобальный `fallbackLanguage`
  (en по умолчанию) → вернуть ключ как строку.
- Флаг requiresFontReview=true обязателен для:
  la, cu, rue, szl, csb, hsb, dsb, cnr.
- В настройках MVP показывать только языки со статусом complete и
  visibleInSettings=true.

=== 5. ПРОВЕРИТЬ СУЩЕСТВУЮЩИЙ КОНТЕНТ, НЕ ПЕРЕСОЗДАВАЯ ЕГО ===
Прогнать валидатор на существующем game/content/core.

Если валидатор находит ошибки в существующем контенте —
разделить их на две категории:

КАТЕГОРИЯ A (реальные ошибки в JSON):
- битый JSON;
- неверный schemaVersion;
- отсутствующее обязательное поле по схеме;
- неверный тип поля (строка вместо числа);
- дубликат ID;
- битая ссылка на несуществующий объект.

КАТЕГОРИЯ B (слишком строгие правила валидатора):
- валидатор требует поле, которого в существующем JSON нет, но в GDD его нет;
- валидатор отвергает существующие числа, которые соответствуют GDD v0.3;
- валидатор отвергает существующие ключи локализации.

Действия:
- Категорию A: исправить в JSON минимально, записать в WORKLOG, обновить patchnotes.
- Категорию B: исправить правила валидатора, чтобы существующий контент проходил.

После прогона validate на существующем контенте результат должен быть: exit 0.

=== 6. ПРОГРЕССИЯ ГЕРОЯ ===
- Добавить HeroProgression: начисление XP, проверка порогов из progression.json,
  левелап через стрим levelup, начисление очка навыка.
- Убрать публичный Hero.TryLevelUp без порогов или сделать внутренним.
- Обновить тесты этапа 02, не ломая поведение прироста характеристик.
- Soft cap 10 применяется сценарием, не хардкодом в домене.

=== 7. SIMRUNNER И СКРИПТЫ ===
- Команда: dotnet run --project src/Game.SimRunner -- validate [--content <path>]
- ContentPathResolver: явный аргумент, иначе поиск game/content/core вверх от BaseDirectory.
- Код возврата: 0 при успехе, 1 при ошибках валидации.
- Вывод: сводка по пакам, количество сущностей, список ошибок и предупреждений.
- tools/scripts/validate.ps1 и validate.sh вызывают реальную команду, убрать сообщение-заглушку.
- Добавить tools/scripts/check-localization.ps1 для сравнения ключей по языкам,
  если его ещё нет.

=== 8. GODOT BOOT ===
- BootController загружает контент через адаптер IContentFileSystem для res://content/core.
- Логирует: количество паков, юнитов, фракций, героев, языков, ключей локализации.
- При ошибке валидации — понятный лог Critical и корректное завершение, без тихого продолжения.
- Никакого геймплея и UI на этом этапе.

=== 9. ТЕСТЫ ===
Добавить тесты:
- существующий контент в game/content/core загружается и проходит валидацию целиком;
- дубликат ID отклоняется;
- битая ссылка отклоняется с указанием поля;
- отсутствующий ключ локализации в `ru` или `en` отклоняется;
- отсутствующий ключ в `draft`/`experimental` языке — предупреждение;
- дробное число в JSON отклоняется;
- неизвестное поле отклоняется;
- порядок загрузки паков детерминирован;
- override по ID применяется и логируется;
- циклическая зависимость отклоняется;
- languages.json соответствует множеству языковых файлов: нет лишних, нет пропусков;
- языковые файлы имеют обязательные поля (schemaVersion, id, language, status, fallback, entries);
- ru и en имеют одинаковое множество ключей;
- добавление 7-го юнита только JSON: временный пак, загрузка, валидация, реестр содержит юнит,
  изменения кода не требуются;
- progression: пороги XP, множественные левелапы за одно начисление, детерминизм стрима,
  soft cap сценария, очко навыка за уровень;
- все 15 юнитов соответствуют таблицам GDD §18 по значениям.

=== 10. ДОКУМЕНТАЦИЯ ===
Обновить:
- CONTENT_SCHEMA.md: полные схемы всех сущностей, примеры, правила ID и версий;
  сверить с существующим контентом и пометить, если реальная схема шире описанной;
- MODDING.md: структура пака, manifest, порядок, override, конфликты;
- LOCALIZATION.md: конвенции ключей, покрытие, поведение при отсутствии ключа,
  список языков, статусы, фолбэк, requiresFontReview;
- BALANCE.md: все числа теперь живут в JSON, указать файлы-источники;
- SAVE_FORMAT.md: требование сериализовать целую часть и остаток GrowthAccumulator;
- ARCHITECTURE.md, TDD.md, TESTING.md, IP_SAFETY.md, KNOWN_ISSUES.md;
- KNOWN_ISSUES.md: KI-009 про шрифтовое покрытие для la/cu/rue/szl/csb/hsb/dsb/cnr;
- GDD.md §9 и BALANCE.md под ADR-025;
- CHANGELOG.md [Unreleased], WORKLOG.md запись W-0004 и/или W-0005/W-0006 по факту,
  patchnotes/v0.3.0.md.

=== 11. ПРОВЕРКИ ===
- dotnet build EidolonSeamWars.sln -c Release — 0 предупреждений, 0 ошибок
- dotnet test EidolonSeamWars.sln -c Release
- dotnet run --project src/Game.SimRunner -- validate — exit 0 на существующем контенте
- powershell -File tools/scripts/validate.ps1 — exit 0
- powershell -File tools/scripts/check-localization.ps1 — exit 0
- Godot --headless --path game --quit — контент загружается, лог без ошибок
- в src/Game.Core нет using Godot, System.Random, TODO, float, double
- все Markdown-ссылки разрешаются
- существующие JSON-файлы в game/content/core не переписаны без причины:
  сравнить diff, каждая правка должна быть обоснована в WORKLOG

=== 12. ЗАПРЕТЫ ===
- Git не использовать.
- Бой, карту, UI, ИИ не реализовывать.
- Числа баланса не менять произвольно: только значения из GDD v0.3.
- Не хардкодить контент в коде.
- Не оставлять заглушку вместо валидатора.
- НЕ перезаписывать существующий контент в game/content/core без явной структурной причины.
- НЕ переписывать художественные переводы.
- НЕ удалять поля, уже присутствующие в существующих JSON.

=== 13. ПОРЯДОК РАБОТЫ ===
1. Прочитать AGENTS.md, GDD, DECISIONS.md, WORKLOG.
2. Проверить существующий контент: прогнать dotnet build, dotnet test,
   попытаться прочитать все JSON в game/content/core.
3. Спроектировать DTO так, чтобы они соответствовали существующему контенту.
4. Реализовать загрузчик, merge, реестр.
5. Реализовать валидатор и добиться exit 0 на существующем контенте.
6. Реализовать HeroProgression.
7. Реализовать SimRunner validate.
8. Реализовать BootController адаптер.
9. Написать тесты.
10. Обновить документацию и WORKLOG.
11. Выдать отчёт.

ЗАВЕРШЕНИЕ:
Отчёт: что сделано, результаты проверок, что не проверено, риски,
записи WORKLOG и новые/обновлённые ADR, diff по существующему контенту
(список изменённых JSON с причиной каждой правки),
следующий шаг prompts/04_adventure_map.md.