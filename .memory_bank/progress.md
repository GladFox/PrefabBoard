# Progress

## Что работает
- PrefabBoard MVP работает в EditorWindow (canvas, pan/zoom, карточки, группы, drag&drop).
- Multi-board работает по модели board-per-file (`PrefabBoardAsset` как отдельные `.asset`).
- Открытие конкретной доски из инспектора `PrefabBoardAsset` через кнопку `Open`.
- Правая панель `Board Items` содержит кнопку `Home` и фокус по `Anchors`/`Elements`.
- Создание групп доступно через RMB context menu на пустом canvas (`Create Group`).
- Переименование групп доступно через context menu группы (`Rename Group`).
- Group drag/resize и undo/redo для элементов/групп остаются в рабочем потоке.
- Preview pipeline для uGUI продолжает работать через `PreviewCache`.

## Известные проблемы
- Полноценный автоматизированный integration-тест preview pipeline отсутствует.
- Требуется ручной smoke-test в Unity после UI cleanup (toolbar/navigation/context menus).
- API `TryCreateTestScene*` оставлен в коде, но принудительно отключён (возвращает `false`).

## Развитие решений
- Полностью убран legacy `BoardLibrary` артефакт из проекта.
- Удалены пользовательские debug/test инструменты (`Preview Debug`, `Create Test Scene`).
- Board-level операции `Duplicate/Rename/Delete` убраны из toolbar; управление этими действиями перенесено в файловый workflow.
- Home-навигация смещена в правую панель, ближе к списку/фокусу элементов.

## Контроль изменений
- last_checked_commit: ce6d571
- last_checked_date: 2026-02-27
