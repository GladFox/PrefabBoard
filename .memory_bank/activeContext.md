# Active Context

## Текущие задачи
1. Устранить компиляционные ошибки UI Toolkit API-совместимости в `BoardCanvasElement`.
2. Проверить отсутствие вызовов недоступных API (`WorldToLocal`, `CapturePointer`, `ReleasePointer`, `HasPointerCapture`) в целевом файле.
3. Зафиксировать hotfix в отдельной ветке и запушить.

## Последние изменения
- В `BoardCanvasElement` заменены несовместимые вызовы событий/координат:
  - `WheelEvent`: `evt.position` -> `evt.localMousePosition`
  - `Pointer*Event`: использование `evt.localPosition`
  - `Drag*Event`: `evt.localMousePosition`
- Захват указателя переведён на совместимый API:
  - `CapturePointer` -> `CaptureMouse`
  - `HasPointerCapture/ReleasePointer` -> `HasMouseCapture/ReleaseMouse`
- Для drag карточки/группы использована конвертация координат в Canvas:
  - `ChangeCoordinatesTo(this, evt.localPosition)`

## Следующие шаги
1. Проверить сборку в Unity Editor.
2. Проверить интеракции drag/select/group move в UI.
3. После подтверждения — merge hotfix в рабочую ветку.

## План (REQUIREMENTS_OWNER)
1. Внести минимальный совместимый фикс только в UI-слое.
2. Не менять UX-контракты и поведение MVP.
3. Обновить Memory Bank и выполнить push.

## Стратегия (ARCHITECT)
- Изменения локализованы в `BoardCanvasElement`.
- Архитектурные слои `Data/Services/UI` не менялись.
- Контракты данных и сохранения состояния не затронуты.

## REVIEWER checklist
- Нет вызовов несовместимых API в `BoardCanvasElement`.
- Логика pan/zoom/dnd/selection сохранена.
- Нет изменений в моделях данных.

## QA_TESTER заметки
- Нужен ручной smoke в Unity 2021.3+: compile + pan/zoom + drag/drop + context actions.
