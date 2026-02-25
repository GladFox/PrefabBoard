# Active Context

## Текущие задачи
1. Закрыть компиляционную ошибку UI Toolkit: `CS0103 The name 'AddManipulator' does not exist in the current context`.
2. Проверить, что контекстное меню карточек и групп продолжает работать через `ContextualMenuPopulateEvent`.
3. Зафиксировать hotfix в git и push в отдельную ветку.

## Последние изменения
- В `PrefabCardElement` и `GroupFrameElement` заменён вызов `AddManipulator(...)` на `RegisterCallback<ContextualMenuPopulateEvent>(...)` для совместимости API.
- Выполнена проверка истории от `last_checked_commit`.

## Следующие шаги
1. Проверить сборку в Unity Editor.
2. Если сборка успешна — создать PR/слить hotfix в рабочую ветку.

## План (REQUIREMENTS_OWNER)
1. Внести минимальный точечный фикс без изменения поведения UX.
2. Сохранить обработчик контекстного меню.
3. Обновить Memory Bank и выполнить push.

## Стратегия (ARCHITECT)
- Изменение локализовано в UI-слое и не затрагивает Data/Services.
- Поведение контекстного меню сохраняется, меняется только способ подписки на событие.

## REVIEWER checklist
- Нет архитектурных регрессий.
- Нет новых зависимостей.
- Memory Bank обновлён.

## QA_TESTER заметки
- Проверить, что ошибка компиляции исчезла.
- Проверить RMB-контекстное меню у карточек и групп.
