# Progress

## Что работает
- Реализован EditorWindow `Prefab Board` на UI Toolkit.
- Реализованы доски, карточки, группы, drag&drop, поиск и базовые операции MVP.
- Исправлена совместимость UI Toolkit API для контекстного меню:
  - `PrefabCardElement`
  - `GroupFrameElement`

## Известные проблемы
- Ручной smoke в Unity Editor ещё не выполнен в этой среде.
- Внешний drag в Scene/Hierarchy для MVP завязан на `Ctrl+LMB` (осознанный компромисс).
- Resize handles для групп не реализованы (вне текущего MVP).

## Развитие решений
- Hotfix выполнен точечно в UI-слое без изменений модели данных.
- Стабилизирована компиляция на окружениях, где `AddManipulator` недоступен в текущем API.

## Контроль изменений
- last_checked_commit: 97f5a2d5b962de396b3896f9ea110f6ed95e9f35
- last_checked_date: 2026-02-25
