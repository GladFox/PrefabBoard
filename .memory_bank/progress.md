# Progress

## Что работает
- Реализован MVP Prefab Board (EditorWindow + UI Toolkit).
- Закрыты ключевые функциональные несоответствия ТЗ.
- Выполнен двухэтапный hotfix совместимости UI Toolkit для `BoardCanvasElement`:
  - событие-координаты нормализованы в `Vector2`
  - удалены несовместимые вызовы pointer-capture API
  - сохранена логика drag/zoom/pan с корректной конвертацией координат

## Известные проблемы
- Ручной smoke в Unity Editor ещё не выполнен в этой среде.
- Внешний drag в Scene/Hierarchy для MVP завязан на `Ctrl+LMB`.
- Resize handles для групп не реализованы (вне текущего MVP).

## Развитие решений
- Совместимость UI Toolkit усилена без изменения Data/Services слоя.
- Исправления точечные и ориентированы на широкий диапазон Unity API.

## Контроль изменений
- last_checked_commit: dd96764183c92b03c72e432b7e0526df25edc000
- last_checked_date: 2026-02-25
