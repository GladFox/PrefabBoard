# Progress

## Что работает
- Реализован EditorWindow `Prefab Board` на UI Toolkit.
- Реализованы доски, карточки, группы, drag&drop, поиск и базовые операции MVP.
- Закрыты ключевые несоответствия ТЗ:
  - duplicate board сохраняет связи карточек с группами
  - `Home` в toolbar выполняет reset view
  - `Add To Group` из контекстного меню карточки таргетирует корректные карточки
  - drag-over для `Project -> Canvas` показывает ghost preview
- Сохранён hotfix совместимости UI Toolkit API для контекстного меню (`RegisterCallback<ContextualMenuPopulateEvent>`).

## Известные проблемы
- Ручной smoke в Unity Editor ещё не выполнен в этой среде.
- Внешний drag в Scene/Hierarchy для MVP завязан на `Ctrl+LMB` (осознанный компромисс).
- Resize handles для групп не реализованы (вне текущего MVP).

## Развитие решений
- Исправления внесены точечно без изменения архитектурного деления `Data/Services/UI`.
- Архитектурные контракты и UX-решения синхронизированы в `local/README.md`.

## Контроль изменений
- last_checked_commit: 7e7a4381a31738335a6108021946b8cd8d410270
- last_checked_date: 2026-02-25
