ЭТАП 0: КАРКАС ПРОЕКТА И ДОКУМЕНТАЦИЯ.
Геймплей НЕ реализовывать.

Прочитай:
- AGENTS.md
- docs/GDD.md, секции 0, 14, 16, 19
- docs/ROADMAP.md
- docs/DECISIONS.md
- docs/CHANGELOG.md

Цель:
Создать структуру проекта, документацию, .NET-каркас, базовые инфраструктурные типы,
git-ready файлы и правила логирования.Игру пока не делать.

ВАЖНО:
На момент этапа 01 Git был недоступен (ADR-019, заменён ADR-029); git-команд не выполнять.
Историю вести через docs/WORKLOG.md.

ЗАДАЧИ:

1. Создать каталоги:
   src/
   tests/
   tools/scripts/
   mods/
   logs/
   saves/
   builds/
   .github/workflows/
   .github/ISSUE_TEMPLATE/
   .opencode/command/
   docs/patchnotes/
   prompts/templates/
   game/content/core/rules/
   game/content/core/factions/
   game/content/core/units/
   game/content/core/hero_classes/
   game/content/core/heroes/
   game/content/core/abilities/
   game/content/core/spells/
   game/content/core/buildings/
   game/content/core/settlements/
   game/content/core/map_objects/
   game/content/core/localization/
   game/scenes/boot/
   game/scenes/menu/
   game/scenes/map/
   game/scenes/battle/
   game/scenes/ui/
   game/scenes/test/
   game/scripts/Boot/
   game/scripts/Menu/
   game/scripts/Map/
   game/scripts/Battle/
   game/scripts/UI/
   game/scripts/Adapters/
   game/assets/generated/
   game/assets/sprites/
   game/assets/icons/
   game/assets/fonts/
   game/assets/audio/
   game/assets/shaders/
   game/assets/themes/

2. Создать корневые файлы:
   AGENTS.md
   opencode.json
   README.md
   LICENSE
   CONTRIBUTING.md
   .gitignore
   .gitattributes
   .editorconfig
   Directory.Build.props

3. Создать/обновить документы:
   docs/GDD.md
   docs/ROADMAP.md
   docs/DECISIONS.md
   docs/CHANGELOG.md
   docs/WORKLOG.md
   docs/ARCHITECTURE.md
   docs/TDD.md
   docs/RUNBOOK.md
   docs/CONTENT_SCHEMA.md
   docs/LOGGING.md
   docs/TESTING.md
   docs/IP_SAFETY.md
   docs/ASSET_LICENSES.md
   docs/GLOSSARY.md
   docs/KNOWN_ISSUES.md
   docs/UI_SPEC.md
   docs/BALANCE.md
   docs/SAVE_FORMAT.md
   docs/MODDING.md
   docs/LOCALIZATION.md
   docs/AI.md
   docs/ART_PIPELINE.md
   docs/RELEASE_CHECKLIST.md
   docs/patchnotes/v0.3.0.md

   Каждый документ должен иметь:
   - шапку: версия, статус, дата;
   - оглавление;
   - реальное содержание по GDD v0.3;
   - ссылки на связанные документы.
   Не создавать пустые пустышки.

4. docs/DECISIONS.md:
   Засеять ADR-001…ADR-020:
   001 Godot 4.7.2 + C#
   002 Game.Core без Godot
   003 гексы на карте и в бою
   004 дневной тик
   005 семь ресурсов
   006 жёсткое лидерство по King’s Bounty
   007 герой вне боя
   008 защита без резистов
   009 контратака автоматическая
   010 пять резистов
   011 мораль экспоненциальная гибридная
   012 формулы побега и выкупа
   013 захват поселения с автоудалением зданий
   014 бесплатное ветеранство 1–5
   015 боезапас отложен до M2
   016 контент в game/content/core
   017 целочисленная математика
   018 лог команд = реплей
   019 workflow без Git
   020 IP-политика и лицензии

5. .NET-каркас:
   Создать solution EidolonSeamWars.
   Создать:
   - src/Game.Core/Game.Core.csproj, net8.0 classlib
   - src/Game.SimRunner/Game.SimRunner.csproj, net8.0 console
   - tests/Game.Core.Tests/Game.Core.Tests.csproj, xUnit

   References:
   - Game.SimRunner → Game.Core
   - Game.Core.Tests → Game.Core

6. Godot:
   Godot-проект game/ пользователь создаст вручную.
   От тебя требуется:
   - структура папок game/
   - game/Directory.Build.props со ссылкой на Game.Core
   - game/icon.svg, оригинальный SVG
   - game/scripts/Boot/BootController.cs, минимальный вывод версии в лог

   Если project.godot ещё не создан — не выдумывать его формат.
   Написать в RUNBOOK инструкцию ручного создания Godot .NET project.

7. Game.Core:
   Создать только инфраструктурные типы:
   - Diagnostics/LogLevel.cs
   - Diagnostics/LogEvent.cs
   - Diagnostics/IGameLogger.cs
   - Rng/SeededRandom.cs
   - Rng/RngStreams.cs
   - Events/IDomainEvent.cs
   - Events/EventBus.cs
   - Domain/Hex.cs

   Hex реализовать полностью:
   - axial coordinates q/r
   - соседи
   - дистанция
   - equality
   - ToString
   - тесты

   Геймплейные типы не создавать. Это этапы 02+.

8. Тесты:
   Создать tests/Game.Core.Tests/HexTests.cs:
   - соседние гексы
   - дистанция
   - equality
   - round-trip координат, если реализован offset

9. tools/scripts:
   Создать:
   - build.ps1
   - build.sh
   - test.ps1
   - test.sh
   - run.ps1
   - run.sh
   - validate.ps1
   - validate.sh

   Пока validate может запускать будущую команду SimRunner validate,
   но если команды ещё нет — скрипт должен честно сообщать, что валидатор появится на этапе 03.

10. .github:
   Создать заготовки:
   - ISSUE_TEMPLATE/bug.yml
   - ISSUE_TEMPLATE/feature.yml
   - ISSUE_TEMPLATE/balance.yml
   - PULL_REQUEST_TEMPLATE.md
   - workflows/ci.yml

   CI должен быть заготовкой:
   restore → build → test → validate.
   Не требовать Git для локальной работы.

11. .opencode/command:
   Создать:
   - check.md
   - docs.md
   - worklog.md
   - balance.md

12. prompts:
   Создать:
   - prompts/README.md
   - prompts/02_core_domain.md
   - prompts/03_content_pipeline.md
   - prompts/04_adventure_map.md
   - prompts/05_battle.md
   - prompts/06_ui_cards.md
   - prompts/07_menu_settings_save.md
   - prompts/08_ai_sim.md
   - prompts/09_hardening.md
   - prompts/templates/bugfix.md
   - prompts/templates/feature.md
   - prompts/templates/balance.md

   Каждый промт должен ссылаться на AGENTS.md и GDD.
   Не реализовывать код этим этапом.

ПРОВЕРКА:
- dotnet build без ошибок.
- dotnet test зелёный.
- В Game.Core нет using Godot.
- Все документы созданы и содержат реальное содержание.
- WORKLOG содержит W-0001.
- CHANGELOG обновлён.

ЗАВЕРШЕНИЕ:
Выдать отчёт:
- дерево созданных файлов;
- команды проверки;
- результаты build/test;
- что не проверено;
- риски;
- следующий промт: prompts/02_core_domain.md.