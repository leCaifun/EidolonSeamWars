# Реестр лицензий ассетов

| Параметр | Значение |
|---|---|
| Версия | 0.4.0 |
| Статус | Актуально для этапа 04 |
| Дата | 2026-11-16 |
| Источник истины | [docs/GDD.md](GDD.md) §1.2, §13 |

## Оглавление

1. [Правила](#1-правила)
2. [Текущие ассеты](#2-текущие-ассеты)
3. [Допустимые лицензии](#3-допустимые-лицензии)
4. [Шаблон записи](#4-шаблон-записи)
5. [Шрифты](#5-шрифты)
6. [Инструменты](#6-инструменты)
7. [Проверка](#7-проверка)
8. [Связанные документы](#8-связанные-документы)

## 1. Правила

- Ни один ассет не попадает в игру без записи в этом реестре.
- Для каждого ассета фиксируются: файл, автор, источник, лицензия, дата, изменения.
- CC-BY требует атрибуции в игре или в файле notices.
- Ассеты с неясной лицензией запрещены (ADR-020).
- Удалённый ассет остаётся в истории реестра со статусом «удалён».

## 2. Текущие ассеты

| Файл | Автор | Источник | Лицензия | Дата | Статус |
|---|---|---|---|---|---|
| `game/icon.svg` | Авторы проекта | Создан вручную для проекта: оригинальная геометрия «гекс + шов + якорь» | Собственная (LICENSE-ASSETS.md) | 2026-11-13 | используется |
| `game/assets/sprites/map/hex_desert.svg` | Авторы проекта | Сгенерирован `tools/scripts/generate_assets.py` (процедурная геометрия, без сторонних исходников) | Собственная (LICENSE-ASSETS.md) | 2026-11-16 | используется (этап 04, не проверен в Godot) |
| `game/assets/sprites/map/hex_forest.svg` | Авторы проекта | Сгенерирован `tools/scripts/generate_assets.py` (процедурная геометрия, без сторонних исходников) | Собственная (LICENSE-ASSETS.md) | 2026-11-16 | используется (этап 04, не проверен в Godot) |
| `game/assets/sprites/map/hex_grass.svg` | Авторы проекта | Сгенерирован `tools/scripts/generate_assets.py` (процедурная геометрия, без сторонних исходников) | Собственная (LICENSE-ASSETS.md) | 2026-11-16 | используется (этап 04, не проверен в Godot) |
| `game/assets/sprites/map/hex_mountain.svg` | Авторы проекта | Сгенерирован `tools/scripts/generate_assets.py` (процедурная геометрия, без сторонних исходников) | Собственная (LICENSE-ASSETS.md) | 2026-11-16 | используется (этап 04, не проверен в Godot) |
| `game/assets/sprites/map/hex_road.svg` | Авторы проекта | Сгенерирован `tools/scripts/generate_assets.py` (процедурная геометрия, без сторонних исходников) | Собственная (LICENSE-ASSETS.md) | 2026-11-16 | используется (этап 04, не проверен в Godot) |
| `game/assets/sprites/map/hex_snow.svg` | Авторы проекта | Сгенерирован `tools/scripts/generate_assets.py` (процедурная геометрия, без сторонних исходников) | Собственная (LICENSE-ASSETS.md) | 2026-11-16 | используется (этап 04, не проверен в Godot) |
| `game/assets/sprites/map/hex_swamp.svg` | Авторы проекта | Сгенерирован `tools/scripts/generate_assets.py` (процедурная геометрия, без сторонних исходников) | Собственная (LICENSE-ASSETS.md) | 2026-11-16 | используется (этап 04, не проверен в Godot) |
| `game/assets/sprites/map/hex_void.svg` | Авторы проекта | Сгенерирован `tools/scripts/generate_assets.py` (процедурная геометрия, без сторонних исходников) | Собственная (LICENSE-ASSETS.md) | 2026-11-16 | используется (этап 04, не проверен в Godot) |
| `game/assets/sprites/map/hex_water.svg` | Авторы проекта | Сгенерирован `tools/scripts/generate_assets.py` (процедурная геометрия, без сторонних исходников) | Собственная (LICENSE-ASSETS.md) | 2026-11-16 | используется (этап 04, не проверен в Godot) |
| `game/assets/sprites/map/objects/token_army_neutral.svg` | Авторы проекта | Сгенерирован `tools/scripts/generate_assets.py` (процедурная геометрия, без сторонних исходников) | Собственная (LICENSE-ASSETS.md) | 2026-11-16 | используется (этап 04, не проверен в Godot) |
| `game/assets/sprites/map/objects/token_dwelling_ash.svg` | Авторы проекта | Сгенерирован `tools/scripts/generate_assets.py` (процедурная геометрия, без сторонних исходников) | Собственная (LICENSE-ASSETS.md) | 2026-11-16 | используется (этап 04, не проверен в Godot) |
| `game/assets/sprites/map/objects/token_dwelling_brass.svg` | Авторы проекта | Сгенерирован `tools/scripts/generate_assets.py` (процедурная геометрия, без сторонних исходников) | Собственная (LICENSE-ASSETS.md) | 2026-11-16 | используется (этап 04, не проверен в Godot) |
| `game/assets/sprites/map/objects/token_dwelling_root.svg` | Авторы проекта | Сгенерирован `tools/scripts/generate_assets.py` (процедурная геометрия, без сторонних исходников) | Собственная (LICENSE-ASSETS.md) | 2026-11-16 | используется (этап 04, не проверен в Godot) |
| `game/assets/sprites/map/objects/token_echo_cache.svg` | Авторы проекта | Сгенерирован `tools/scripts/generate_assets.py` (процедурная геометрия, без сторонних исходников) | Собственная (LICENSE-ASSETS.md) | 2026-11-16 | используется (этап 04, не проверен в Godot) |
| `game/assets/sprites/map/objects/token_flag_ash.svg` | Авторы проекта | Сгенерирован `tools/scripts/generate_assets.py` (процедурная геометрия, без сторонних исходников) | Собственная (LICENSE-ASSETS.md) | 2026-11-16 | используется (этап 04, не проверен в Godot) |
| `game/assets/sprites/map/objects/token_flag_brass.svg` | Авторы проекта | Сгенерирован `tools/scripts/generate_assets.py` (процедурная геометрия, без сторонних исходников) | Собственная (LICENSE-ASSETS.md) | 2026-11-16 | используется (этап 04, не проверен в Godot) |
| `game/assets/sprites/map/objects/token_flag_root.svg` | Авторы проекта | Сгенерирован `tools/scripts/generate_assets.py` (процедурная геометрия, без сторонних исходников) | Собственная (LICENSE-ASSETS.md) | 2026-11-16 | используется (этап 04, не проверен в Godot) |
| `game/assets/sprites/map/objects/token_hero_ash.svg` | Авторы проекта | Сгенерирован `tools/scripts/generate_assets.py` (процедурная геометрия, без сторонних исходников) | Собственная (LICENSE-ASSETS.md) | 2026-11-16 | используется (этап 04, не проверен в Godot) |
| `game/assets/sprites/map/objects/token_hero_brass.svg` | Авторы проекта | Сгенерирован `tools/scripts/generate_assets.py` (процедурная геометрия, без сторонних исходников) | Собственная (LICENSE-ASSETS.md) | 2026-11-16 | используется (этап 04, не проверен в Godot) |
| `game/assets/sprites/map/objects/token_hero_root.svg` | Авторы проекта | Сгенерирован `tools/scripts/generate_assets.py` (процедурная геометрия, без сторонних исходников) | Собственная (LICENSE-ASSETS.md) | 2026-11-16 | используется (этап 04, не проверен в Godot) |
| `game/assets/sprites/map/objects/token_mine_crystal.svg` | Авторы проекта | Сгенерирован `tools/scripts/generate_assets.py` (процедурная геометрия, без сторонних исходников) | Собственная (LICENSE-ASSETS.md) | 2026-11-16 | используется (этап 04, не проверен в Godot) |
| `game/assets/sprites/map/objects/token_mine_essence.svg` | Авторы проекта | Сгенерирован `tools/scripts/generate_assets.py` (процедурная геометрия, без сторонних исходников) | Собственная (LICENSE-ASSETS.md) | 2026-11-16 | используется (этап 04, не проверен в Godot) |
| `game/assets/sprites/map/objects/token_mine_gold.svg` | Авторы проекта | Сгенерирован `tools/scripts/generate_assets.py` (процедурная геометрия, без сторонних исходников) | Собственная (LICENSE-ASSETS.md) | 2026-11-16 | используется (этап 04, не проверен в Godot) |
| `game/assets/sprites/map/objects/token_mine_mercury.svg` | Авторы проекта | Сгенерирован `tools/scripts/generate_assets.py` (процедурная геометрия, без сторонних исходников) | Собственная (LICENSE-ASSETS.md) | 2026-11-16 | используется (этап 04, не проверен в Godot) |
| `game/assets/sprites/map/objects/token_mine_ore.svg` | Авторы проекта | Сгенерирован `tools/scripts/generate_assets.py` (процедурная геометрия, без сторонних исходников) | Собственная (LICENSE-ASSETS.md) | 2026-11-16 | используется (этап 04, не проверен в Godot) |
| `game/assets/sprites/map/objects/token_mine_sulfur.svg` | Авторы проекта | Сгенерирован `tools/scripts/generate_assets.py` (процедурная геометрия, без сторонних исходников) | Собственная (LICENSE-ASSETS.md) | 2026-11-16 | используется (этап 04, не проверен в Godot) |
| `game/assets/sprites/map/objects/token_mine_wood.svg` | Авторы проекта | Сгенерирован `tools/scripts/generate_assets.py` (процедурная геометрия, без сторонних исходников) | Собственная (LICENSE-ASSETS.md) | 2026-11-16 | используется (этап 04, не проверен в Godot) |
| `game/assets/sprites/map/objects/token_patrol_ash.svg` | Авторы проекта | Сгенерирован `tools/scripts/generate_assets.py` (процедурная геометрия, без сторонних исходников) | Собственная (LICENSE-ASSETS.md) | 2026-11-16 | используется (этап 04, не проверен в Godot) |
| `game/assets/sprites/map/objects/token_patrol_brass.svg` | Авторы проекта | Сгенерирован `tools/scripts/generate_assets.py` (процедурная геометрия, без сторонних исходников) | Собственная (LICENSE-ASSETS.md) | 2026-11-16 | используется (этап 04, не проверен в Godot) |
| `game/assets/sprites/map/objects/token_patrol_root.svg` | Авторы проекта | Сгенерирован `tools/scripts/generate_assets.py` (процедурная геометрия, без сторонних исходников) | Собственная (LICENSE-ASSETS.md) | 2026-11-16 | используется (этап 04, не проверен в Godot) |
| `game/assets/sprites/map/objects/token_settlement_ash.svg` | Авторы проекта | Сгенерирован `tools/scripts/generate_assets.py` (процедурная геометрия, без сторонних исходников) | Собственная (LICENSE-ASSETS.md) | 2026-11-16 | используется (этап 04, не проверен в Godot) |
| `game/assets/sprites/map/objects/token_settlement_brass.svg` | Авторы проекта | Сгенерирован `tools/scripts/generate_assets.py` (процедурная геометрия, без сторонних исходников) | Собственная (LICENSE-ASSETS.md) | 2026-11-16 | используется (этап 04, не проверен в Godot) |
| `game/assets/sprites/map/objects/token_settlement_root.svg` | Авторы проекта | Сгенерирован `tools/scripts/generate_assets.py` (процедурная геометрия, без сторонних исходников) | Собственная (LICENSE-ASSETS.md) | 2026-11-16 | используется (этап 04, не проверен в Godot) |
| `game/assets/sprites/ui/map/icon_blocked.svg` | Авторы проекта | Сгенерирован `tools/scripts/generate_assets.py` (процедурная геометрия, без сторонних исходников) | Собственная (LICENSE-ASSETS.md) | 2026-11-16 | используется (этап 04, не проверен в Godot) |
| `game/assets/sprites/ui/map/icon_danger.svg` | Авторы проекта | Сгенерирован `tools/scripts/generate_assets.py` (процедурная геометрия, без сторонних исходников) | Собственная (LICENSE-ASSETS.md) | 2026-11-16 | используется (этап 04, не проверен в Godot) |
| `game/assets/sprites/ui/map/icon_explored.svg` | Авторы проекта | Сгенерирован `tools/scripts/generate_assets.py` (процедурная геометрия, без сторонних исходников) | Собственная (LICENSE-ASSETS.md) | 2026-11-16 | используется (этап 04, не проверен в Godot) |
| `game/assets/sprites/ui/map/icon_flyer.svg` | Авторы проекта | Сгенерирован `tools/scripts/generate_assets.py` (процедурная геометрия, без сторонних исходников) | Собственная (LICENSE-ASSETS.md) | 2026-11-16 | используется (этап 04, не проверен в Godot) |
| `game/assets/sprites/ui/map/icon_hidden.svg` | Авторы проекта | Сгенерирован `tools/scripts/generate_assets.py` (процедурная геометрия, без сторонних исходников) | Собственная (LICENSE-ASSETS.md) | 2026-11-16 | используется (этап 04, не проверен в Godot) |
| `game/assets/sprites/ui/map/icon_movement_cost.svg` | Авторы проекта | Сгенерирован `tools/scripts/generate_assets.py` (процедурная геометрия, без сторонних исходников) | Собственная (LICENSE-ASSETS.md) | 2026-11-16 | используется (этап 04, не проверен в Godot) |
| `game/assets/sprites/ui/map/icon_passable.svg` | Авторы проекта | Сгенерирован `tools/scripts/generate_assets.py` (процедурная геометрия, без сторонних исходников) | Собственная (LICENSE-ASSETS.md) | 2026-11-16 | используется (этап 04, не проверен в Godot) |
| `game/assets/sprites/ui/map/icon_road.svg` | Авторы проекта | Сгенерирован `tools/scripts/generate_assets.py` (процедурная геометрия, без сторонних исходников) | Собственная (LICENSE-ASSETS.md) | 2026-11-16 | используется (этап 04, не проверен в Godot) |
| `game/assets/sprites/ui/map/icon_selected.svg` | Авторы проекта | Сгенерирован `tools/scripts/generate_assets.py` (процедурная геометрия, без сторонних исходников) | Собственная (LICENSE-ASSETS.md) | 2026-11-16 | используется (этап 04, не проверен в Godot) |
| `game/assets/sprites/ui/map/icon_visibility.svg` | Авторы проекта | Сгенерирован `tools/scripts/generate_assets.py` (процедурная геометрия, без сторонних исходников) | Собственная (LICENSE-ASSETS.md) | 2026-11-16 | используется (этап 04, не проверен в Godot) |
| `game/assets/sprites/ui/panels/panel_bottom_dark.svg` | Авторы проекта | Сгенерирован `tools/scripts/generate_assets.py` (процедурная геометрия, без сторонних исходников) | Собственная (LICENSE-ASSETS.md) | 2026-11-16 | используется (этап 04, не проверен в Godot) |
| `game/assets/sprites/ui/panels/panel_button_hover.svg` | Авторы проекта | Сгенерирован `tools/scripts/generate_assets.py` (процедурная геометрия, без сторонних исходников) | Собственная (LICENSE-ASSETS.md) | 2026-11-16 | используется (этап 04, не проверен в Godot) |
| `game/assets/sprites/ui/panels/panel_button_normal.svg` | Авторы проекта | Сгенерирован `tools/scripts/generate_assets.py` (процедурная геометрия, без сторонних исходников) | Собственная (LICENSE-ASSETS.md) | 2026-11-16 | используется (этап 04, не проверен в Godot) |
| `game/assets/sprites/ui/panels/panel_button_pressed.svg` | Авторы проекта | Сгенерирован `tools/scripts/generate_assets.py` (процедурная геометрия, без сторонних исходников) | Собственная (LICENSE-ASSETS.md) | 2026-11-16 | используется (этап 04, не проверен в Godot) |
| `game/assets/sprites/ui/panels/panel_card_dark.svg` | Авторы проекта | Сгенерирован `tools/scripts/generate_assets.py` (процедурная геометрия, без сторонних исходников) | Собственная (LICENSE-ASSETS.md) | 2026-11-16 | используется (этап 04, не проверен в Godot) |
| `game/assets/sprites/ui/panels/panel_resource_slot.svg` | Авторы проекта | Сгенерирован `tools/scripts/generate_assets.py` (процедурная геометрия, без сторонних исходников) | Собственная (LICENSE-ASSETS.md) | 2026-11-16 | используется (этап 04, не проверен в Godot) |
| `game/assets/sprites/ui/panels/panel_side_left.svg` | Авторы проекта | Сгенерирован `tools/scripts/generate_assets.py` (процедурная геометрия, без сторонних исходников) | Собственная (LICENSE-ASSETS.md) | 2026-11-16 | используется (этап 04, не проверен в Godot) |
| `game/assets/sprites/ui/panels/panel_side_right.svg` | Авторы проекта | Сгенерирован `tools/scripts/generate_assets.py` (процедурная геометрия, без сторонних исходников) | Собственная (LICENSE-ASSETS.md) | 2026-11-16 | используется (этап 04, не проверен в Godot) |
| `game/assets/sprites/ui/panels/panel_tooltip.svg` | Авторы проекта | Сгенерирован `tools/scripts/generate_assets.py` (процедурная геометрия, без сторонних исходников) | Собственная (LICENSE-ASSETS.md) | 2026-11-16 | используется (этап 04, не проверен в Godot) |
| `game/assets/sprites/ui/panels/panel_top_dark.svg` | Авторы проекта | Сгенерирован `tools/scripts/generate_assets.py` (процедурная геометрия, без сторонних исходников) | Собственная (LICENSE-ASSETS.md) | 2026-11-16 | используется (этап 04, не проверен в Godot) |
| `game/assets/sprites/ui/resources/icon_crystal.svg` | Авторы проекта | Сгенерирован `tools/scripts/generate_assets.py` (процедурная геометрия, без сторонних исходников) | Собственная (LICENSE-ASSETS.md) | 2026-11-16 | используется (этап 04, не проверен в Godot) |
| `game/assets/sprites/ui/resources/icon_essence.svg` | Авторы проекта | Сгенерирован `tools/scripts/generate_assets.py` (процедурная геометрия, без сторонних исходников) | Собственная (LICENSE-ASSETS.md) | 2026-11-16 | используется (этап 04, не проверен в Godot) |
| `game/assets/sprites/ui/resources/icon_gold.svg` | Авторы проекта | Сгенерирован `tools/scripts/generate_assets.py` (процедурная геометрия, без сторонних исходников) | Собственная (LICENSE-ASSETS.md) | 2026-11-16 | используется (этап 04, не проверен в Godot) |
| `game/assets/sprites/ui/resources/icon_mercury.svg` | Авторы проекта | Сгенерирован `tools/scripts/generate_assets.py` (процедурная геометрия, без сторонних исходников) | Собственная (LICENSE-ASSETS.md) | 2026-11-16 | используется (этап 04, не проверен в Godot) |
| `game/assets/sprites/ui/resources/icon_ore.svg` | Авторы проекта | Сгенерирован `tools/scripts/generate_assets.py` (процедурная геометрия, без сторонних исходников) | Собственная (LICENSE-ASSETS.md) | 2026-11-16 | используется (этап 04, не проверен в Godot) |
| `game/assets/sprites/ui/resources/icon_sulfur.svg` | Авторы проекта | Сгенерирован `tools/scripts/generate_assets.py` (процедурная геометрия, без сторонних исходников) | Собственная (LICENSE-ASSETS.md) | 2026-11-16 | используется (этап 04, не проверен в Godot) |
| `game/assets/sprites/ui/resources/icon_wood.svg` | Авторы проекта | Сгенерирован `tools/scripts/generate_assets.py` (процедурная геометрия, без сторонних исходников) | Собственная (LICENSE-ASSETS.md) | 2026-11-16 | используется (этап 04, не проверен в Godot) |

Иных звуковых и шрифтовых ассетов нет. JSON-контент и документация — авторские материалы проекта.

## 3. Допустимые лицензии

| Лицензия | Использование | Условия |
|---|---|---|
| MIT | сторонний код и ассеты под MIT | сохранить уведомление |
| Apache-2.0 | исходный код проекта (ADR-032) | сохранить уведомление и NOTICE при наличии |
| CC0 | графика, звук, шрифты | без обязательств, но фиксируется источник |
| CC-BY 3.0/4.0 | графика, звук, шрифты | обязательная атрибуция |
| OFL 1.1 | шрифты | сохранить лицензию, не продавать шрифт отдельно |
| Собственная | любые | авторство проекта подтверждено |

Запрещены: NC, ND, «только для некоммерческого использования», unknown license,
fan art, ripped assets.

## 4. Шаблон записи

```
Файл:
Автор:
Источник (URL/архив):
Лицензия:
Дата добавления:
Изменения:
Атрибуция (если нужна):
```

## 5. Шрифты

- Шрифты не добавлены; UI использует системный шрифт Godot или
  будущий OFL-шрифт.
- Перед добавлением проверить поддержку кириллицы и право на встраивание.
- Файл лицензии шрифта хранится рядом со шрифтом в `game/assets/fonts`.
- Реестр обновляется до первого использования шрифта в UI.

## 6. Инструменты

| Инструмент | Назначение | Лицензия/условия |
|---|---|---|
| Godot 4.7.2 .NET (целевая по ADR-023) | движок | MIT |
| .NET 8 SDK | сборка | MIT |
| xUnit | тесты | Apache-2.0 |
| VS Code / OpenCode | разработка | по лицензиям продуктов |

Инструменты не поставляются как ассеты игры. Условия AI-инструментов проверяются
отдельно до коммерческого релиза.

## 7. Проверка

- [ ] Каждый файл в `game/assets/**` есть в реестре.
- [ ] Лицензия допускает коммерческое использование.
- [ ] Атрибуция добавлена, если требуется.
- [ ] Нет файлов с неизвестным происхождением.
- [ ] Реестр обновлён в той же задаче, что и ассет.

## 8. Связанные документы

- [docs/IP_SAFETY.md](IP_SAFETY.md)
- [docs/ART_PIPELINE.md](ART_PIPELINE.md)
- [docs/DECISIONS.md](DECISIONS.md) — ADR-020
- [docs/RELEASE_CHECKLIST.md](RELEASE_CHECKLIST.md)
- [LICENSE](../LICENSE)
