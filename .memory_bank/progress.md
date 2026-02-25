# Progress

## Что работает
- MVP Prefab Board функционирует (канвас, pan/zoom, карточки, группы, dnd, multi-board).
- Исправлена совместимость `BoardCanvasElement` с API UI Toolkit в Unity 2021.3+.
- Добавлен custom preview pipeline для uGUI prefab:
  - рендер через временный Canvas/Camera в preview scene;
  - fallback к `AssetPreview` для остальных prefab;
  - кэш custom preview с очисткой ресурсов.
- Исправлен race в превью-кэше: mini thumbnail не фиксируется в кэше, пока `AssetPreview` в состоянии loading.

## Известные проблемы
- Нет автоматизированного integration-теста preview в Unity Editor.
- Для сложных UI prefab с нестандартной иерархией может потребоваться дополнительная подстройка framing.
- Внешний drag в Scene/Hierarchy для MVP остаётся на `Ctrl+LMB`.

## Развитие решений
- Preview система эволюционировала от чистого `AssetPreview` к гибридной схеме:
  1. UI fallback (Canvas+Camera),
  2. стандартный `AssetPreview`,
  3. mini/icon fallback.
- Улучшена визуальная предсказуемость карточек для UI-контента.

## Контроль изменений
- last_checked_commit: 25eec60c50d58f99e269b28336a7eb5179ac811d
- last_checked_date: 2026-02-25
