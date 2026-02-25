# Active Context

## Текущие задачи
1. Завершить ревью соответствия MVP к ТЗ после внесённых правок.
2. Выполнить ручной smoke в Unity Editor.
3. Подготовить PR с ветки `feat/tz-alignment`.

## Последние изменения
- Исправлен duplicate board: сохранение связей `item.groupId` через remap старых/новых `group.id`.
- Добавлен drag-over ghost preview при `Project -> Canvas`.
- `Home` в toolbar переключён на `ResetView`.
- Исправлено `Add To Group` из контекстного меню карточки: действие корректно таргетирует карточку под курсором (или текущую selection, если она включает карточку).
- Обновлён `local/README.md` по контрактам поведения.

## Следующие шаги
1. Проверить smoke в Unity: duplicate board/groups, drag ghost, Home reset, Add To Group.
2. Проверить отсутствие новых компиляционных ошибок.
3. Выполнить merge в рабочую ветку после подтверждения.

## План (REQUIREMENTS_OWNER)
1. Закрыть все найденные функциональные gap'ы без расширения MVP scope.
2. Зафиксировать обновлённые архитектурные и поведенческие контракты в README.
3. Завершить задачу commit+push.

## Стратегия (ARCHITECT)
- Изменения локализованы в существующих слоях (`Services`, `UI`, `Styles`), архитектура не менялась.
- Контракты данных/координат сохранены.
- UX улучшения (ghost, Home reset) реализованы минимально-инвазивно.

## REVIEWER checklist
- `DuplicateBoard` не теряет group linkage.
- `Home` соответствует reset-view.
- `Add To Group` корректен для контекстной карточки.
- Drag-over ghost отображается только для валидного prefab drag.

## QA_TESTER заметки
- Автотестов нет.
- Нужен ручной smoke в Unity Editor по обновлённым сценариям.
