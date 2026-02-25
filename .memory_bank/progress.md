# Progress

## Что работает
- Реализован MVP Prefab Board (EditorWindow + UI Toolkit).
- Закрыты ключевые функциональные несоответствия ТЗ (duplicate-group links, Home reset, card add-to-group targeting, drag ghost).
- Добавлен compatibility hotfix для Unity API в `BoardCanvasElement`:
  - совместимые локальные координаты событий
  - совместимый захват/освобождение мыши

## Известные проблемы
- Ручной smoke в Unity Editor ещё не выполнен в этой среде.
- Внешний drag в Scene/Hierarchy для MVP завязан на `Ctrl+LMB`.
- Resize handles для групп не реализованы (вне текущего MVP).

## Развитие решений
- Совместимость UI Toolkit усилена без изменения модели данных.
- Исправления точечные и не затрагивают архитектурное деление проекта.

## Контроль изменений
- last_checked_commit: 6f08a450e1e276a106acd1fd5f31669410164881
- last_checked_date: 2026-02-25
