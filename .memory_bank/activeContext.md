# Active Context

## Текущие задачи
1. Проверить в Unity UX после cleanup toolbar/navigation (создание/переключение досок, Home в правой панели).
2. Проверить workflow групп: RMB Create Group, Rename Group, drag/resize, undo/redo.
3. Проверить открытие доски из инспектора `PrefabBoardAsset` кнопкой `Open`.
4. Подтвердить, что debug/test scene инструменты больше недоступны в меню.

## Последние изменения (текущая сессия)
- Удалены legacy/debug артефакты:
  - удалён `BoardLibraryAsset` (скрипт и `BoardLibrary.asset`);
  - удалены меню/окна `Preview Debug` и `Create Test Scene`.
- В `PreviewCache` публичный API создания test-сцены отключён (`Test scene rendering is disabled`).
- В `PreviewDebugCapture` отключён runtime capture по умолчанию (`CaptureEnabled = false`).
- В toolbar убраны операции board-level `Duplicate`, `Rename`, `Delete` и кнопка `Home`.
- Кнопка `Home` перенесена в правую панель навигации (`BoardOutlineElement`).
- В Canvas добавлено контекстное меню по правому клику на пустом месте: `Create Group`.
- Для групп добавлено переименование через context menu (`Rename Group`).
- Добавлено окно ввода текста `TextPromptWindow` для rename операций.
- Добавлен custom inspector `PrefabBoardAssetEditor` с кнопкой `Open` для открытия конкретной доски.
- `PrefabBoardWindow` получил `OpenBoard(PrefabBoardAsset)` и поддержку отложенного выбора доски при открытии окна.
- Обновлены стили правой панели под кнопку `Home`.

## Следующие шаги
1. Ручной smoke в Unity Editor по сценариям из DoD/текущих задач.
2. Если понадобятся board-level операции (rename/delete), выполнять через файловый менеджмент ассетов.
3. При следующем изменении обновить `VERSION` и `RELEASE_NOTES.md` (если планируется релизный срез).

## План (REQUIREMENTS_OWNER)
1. Удалить неактуальные инструменты debug/test scene.
2. Перенастроить UX панели управления доской (toolbar + navigation).
3. Добавить rename групп и создание групп через RMB-меню.
4. Убрать зависимость от BoardLibrary и открыть доску из инспектора ассета.

## Стратегия (ARCHITECT)
- Хранение multi-board остаётся board-per-file, без shared library asset.
- Board-level lifecycle операции не предоставляются кнопками в toolbar.
- Контекстные действия для групп/канваса реализуются через `ContextualMenuPopulateEvent`.

## REVIEWER checklist
- Нет ссылок на удалённые `BoardLibraryAsset`, `PreviewDebugWindow`, `PreviewTestSceneMenu`.
- Home доступен только в right outline.
- В контекстном меню группы есть `Rename Group`.
- В контекстном меню пустого canvas есть `Create Group`.
- Инспектор `PrefabBoardAsset` содержит кнопку `Open`.

## QA_TESTER заметки
- Локальная `dotnet build PrefabBoard.sln` прошла успешно.
- Нужна ручная проверка внутри Unity Editor.
