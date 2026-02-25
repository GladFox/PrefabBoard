# Active Context

## Текущие задачи
1. Завершить проверку MVP Prefab Board в Unity Editor (ручной smoke).
2. Убедиться, что все элементы DoD из ТЗ закрыты или явно зафиксированы как ограничения.
3. Подготовить итоговый commit и push ветки `feature/prefab-board-mvp`.

## Последние изменения
- Реализованы Data сущности: `PrefabBoardAsset`, `BoardItemData`, `BoardGroupData`, `BoardLibraryAsset`, `BoardViewSettings`.
- Реализованы Services: `BoardRepository`, `AssetGuidUtils`, `PreviewCache`, `BoardUndo`.
- Реализованы UI элементы: `PrefabBoardWindow`, `BoardCanvasElement`, `PrefabCardElement`, `GroupFrameElement`, `SelectionOverlayElement`, `BoardToolbarElement`.
- Добавлены стили `PrefabBoard.uss`.
- Обновлен `local/README.md` с архитектурными контрактами и текущими ограничениями MVP.

## Следующие шаги
1. Проверить поведение окна и интеракций внутри Unity Editor.
2. Обновить `progress.md` с результатами QA и known issues.
3. Выполнить git commit + push.

## План (REQUIREMENTS_OWNER)
1. Завершить реализацию MVP по вертикальному срезу (toolbar/canvas/cards/groups/dnd).
2. Проверить соответствие DoD.
3. Зафиксировать состояние в документации и git.

## Стратегия (ARCHITECT)
- Модель world/screen с хранением world-координат и `pan` в px сохранена.
- Данные карточек/групп хранятся только в ScriptableObject.
- Доски хранятся отдельными ассетами, библиотека хранит индекс и last-opened board.

## REVIEWER checklist
- Архитектурные слои Data/Services/UI разделены.
- Изменения синхронизированы с `local/README.md`.
- Memory Bank обновлён.
- Новых внешних зависимостей не добавлено.

## QA_TESTER заметки
- Автоматические тесты отсутствуют.
- Требуется ручная проверка в Unity Editor: pan/zoom/dnd/selection/groups/undo.
