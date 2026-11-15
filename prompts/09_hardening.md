# Этап 09 — Стабилизация и приёмка M0

| Параметр | Значение |
|---|---|
| Версия | 0.3.0 |
| Статус | Задание |
| Дата | 2026-11-13 |
| Этап ROADMAP | M0, часы 22–24 |
| Источник истины | [AGENTS.md](../AGENTS.md), [docs/GDD.md](../docs/GDD.md) §19 |

## Оглавление

1. [Цель](#1-цель)
2. [Прочитать](#2-прочитать)
3. [Задачи](#3-задачи)
4. [Проверки](#4-проверки)
5. [Документация](#5-документация)
6. [Критерии приёмки M0](#6-критерии-приёмки-m0)
7. [Отчёт](#7-отчёт)

## 1. Цель

Стабилизировать MVP: закрыть тестовые пробелы, проверить логирование и
производительность, обновить патчноут и пройти критерии приёмки GDD §19.
Новых геймплейных фич не добавлять.

## 2. Прочитать

- [AGENTS.md](../AGENTS.md) — Definition of Done.
- [docs/GDD.md](../docs/GDD.md) §19 — критерии приёмки MVP.
- [docs/TESTING.md](../docs/TESTING.md) — полный план тестов.
- [docs/RELEASE_CHECKLIST.md](../docs/RELEASE_CHECKLIST.md) — чек-лист.
- [docs/KNOWN_ISSUES.md](../docs/KNOWN_ISSUES.md) — известные проблемы.
- [docs/LOGGING.md](../docs/LOGGING.md) — обязательные точки логов.
- [docs/DECISIONS.md](../docs/DECISIONS.md) — проверить, что решения не нарушены.

## 3. Задачи

1. Закрыть пробелы в тестах: все пункты [docs/TESTING.md](../docs/TESTING.md)
   для M0 должны иметь покрытие.
2. Добавить архитектурный тест: `Game.Core` не ссылается на Godot и не содержит
   `float`/`double` в доменных типах.
3. Проверить формат логов: JSON-строки, `sessionId`, `correlationId`, уровень,
   категория; ротация 5×10 МБ; crash-отчёт с последними 200 строками.
4. Проверить все обязательные точки логирования из [docs/LOGGING.md](../docs/LOGGING.md).
5. Прогнать полный цикл руками: меню → карта → бой → осада → сейв → лоад.
6. Прогнать `SimRunner smoke` и `simulate`, зафиксировать результаты.
7. Проверить, что новый юнит добавляется только JSON и проходит валидатор.
8. Проверить RU/EN, настройки, безлимитные сейвы и миграции.
9. Обновить [docs/KNOWN_ISSUES.md](../docs/KNOWN_ISSUES.md) и
   [docs/RELEASE_CHECKLIST.md](../docs/RELEASE_CHECKLIST.md).
10. Создать `docs/patchnotes/vX.Y.Z.md` и обновить CHANGELOG.

## 4. Проверки

```powershell
dotnet build EidolonSeamWars.sln -c Release
dotnet test EidolonSeamWars.sln -c Release
powershell -NoProfile -ExecutionPolicy Bypass -File tools/scripts/validate.ps1
dotnet run --project src/Game.SimRunner -c Release -- smoke
dotnet run --project src/Game.SimRunner -c Release -- simulate --seed 123 --battles 100
```

Дополнительно:

- поиск `using Godot` в `src/Game.Core`;
- поиск `float`/`double` в домене;
- поиск `System.Random` в домене;
- проверка, что все пользовательские строки — ключи локализации;
- проверка отсутствия TODO в реализованной части.

## 5. Документация

- [docs/TESTING.md](../docs/TESTING.md) — итоговое покрытие.
- [docs/LOGGING.md](../docs/LOGGING.md) — проверенные точки.
- [docs/KNOWN_ISSUES.md](../docs/KNOWN_ISSUES.md) — открытые проблемы.
- [docs/RELEASE_CHECKLIST.md](../docs/RELEASE_CHECKLIST.md) — пройденные пункты.
- [docs/ROADMAP.md](../docs/ROADMAP.md) — статус M0.
- [docs/patchnotes/](../docs/patchnotes/) — патчноут версии.
- [docs/WORKLOG.md](../docs/WORKLOG.md) и [docs/CHANGELOG.md](../docs/CHANGELOG.md).

## 6. Критерии приёмки M0

Все пункты [docs/GDD.md](../docs/GDD.md) §19 обязательны. Особенно:

- полный цикл меню→карта→бой→осада→сейв→лоад играбелен;
- карточки юнитов показывают все поля, рамка/фон фракции;
- ожидание и защита работают по правилам;
- герой не на поле, кастует через панель, качается;
- мораль экспоненциальная и описательная;
- побег/выкуп работают;
- захват поселения автоудаляет здания;
- тестовая битва симулируется;
- настройки и RU/EN работают;
- логи пишутся;
- в `Game.Core` нет `using Godot`;
- новый юнит добавляется только JSON.

## 7. Отчёт

Указать: итоговые результаты build/test/validate/smoke/simulate, пройденные пункты
GDD §19, что не проверено, открытые риски, готовность к M1.
