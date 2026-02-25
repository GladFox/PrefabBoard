# Progress

## Что работает
- Реализован EditorWindow `Prefab Board` на UI Toolkit.
- Реализованы доски и библиотека досок (`BoardLibraryAsset` + отдельные `PrefabBoardAsset`).
- Реализован Canvas с:
  - grid рендерингом
  - pan (`MMB` / `Space+LMB`)
  - zoom-to-cursor (mouse wheel)
  - box selection
- Реализованы карточки префабов:
  - preview + title + short note
  - выделение, перемещение, удаление, дублирование
  - контекстное меню
- Реализованы группы:
  - создание
  - перемещение группы с вложенными карточками
  - добавление выбранных карточек в группу
- Реализован drag&drop:
  - `Project -> Canvas` (создание карточек из prefab assets)
  - `Ctrl+LMB` по карточке -> внешний drag в `Scene/Hierarchy`
- Включены undo/dirty потоки для операций изменения данных.

## Известные проблемы
- Ручной smoke в Unity Editor ещё не выполнен в этой среде.
- Внешний drag в Scene/Hierarchy для MVP завязан на `Ctrl+LMB` (осознанный компромисс, чтобы не конфликтовать с внутренним drag).
- Resize handles для групп не реализованы (вне текущего MVP).

## Развитие решений
- Архитектура разделена на `Data / Services / UI`.
- Контракт координат world/screen зафиксирован в `local/README.md`.
- Модель хранения данных соответствует подходу “данные + визуализация” без GameObject-карточек.

## Контроль изменений
- last_checked_commit: a76a6cb0d8673cbd95d9224f67e3cc3e6c445a2c
- last_checked_date: 2026-02-25
