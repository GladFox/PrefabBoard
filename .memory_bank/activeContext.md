# Active Context

## Текущие задачи
1. Закрыть остаточные compile-ошибки совместимости `BoardCanvasElement` на старом UI Toolkit API.
2. Проверить отсутствие недоступных методов pointer capture и конфликтов `Vector3/Vector2`.
3. Зафиксировать дополнительный hotfix и push.

## Последние изменения
- На предыдущем шаге добавлен compatibility hotfix (`dd96764`).
- Дополнительно выполнена правка событийной математики:
  - явная нормализация event-позиций в `Vector2`
  - удалены вызовы недоступного pointer-capture API (`CaptureMouse/ReleaseMouse/HasMouseCapture`)
  - сохранена координатная конверсия через `ChangeCoordinatesTo` с нормализацией координат

## Следующие шаги
1. Проверить компиляцию в Unity Editor.
2. Ручной smoke сценариев Canvas (pan/zoom/drag/group move).
3. После подтверждения — merge hotfix.

## План (REQUIREMENTS_OWNER)
1. Внести минимальные правки только для compile compatibility.
2. Не менять функциональные контракты MVP.
3. Обновить Memory Bank и выполнить push.

## Стратегия (ARCHITECT)
- Изменения локализованы только в `BoardCanvasElement`.
- Архитектура слоёв и data contracts не изменены.
- Для максимальной совместимости удалён hard dependency на pointer capture API.

## REVIEWER checklist
- В файле отсутствуют `CaptureMouse/ReleaseMouse/HasMouseCapture`.
- В файле отсутствуют `WorldToLocal` и `evt.position`/`evt.mousePosition`.
- Нет неоднозначных операций `Vector3 - Vector2`.

## QA_TESTER заметки
- Нужен smoke в Unity 2021.3+ на сборку и базовые интеракции Canvas.
