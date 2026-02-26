# Progress

## Что работает
- MVP Prefab Board функционирует (canvas, pan/zoom, карточки, группы, dnd, multi-board).
- Для UI prefab работает custom preview pipeline с режимами рендера:
  - `Auto`
  - `Resolution`
  - `Control Size`
- Режим preview хранится на уровне карточки (`BoardItemData.previewRenderMode`) и дублируется вместе с карточкой.
- Кэш preview учитывает режим и размер холста (`prefabGuid + mode + canvasSize`).
- UI preview рендерится через rig `Camera + ScreenSpaceCanvas + Content`, с layer routing на `UI` слой.
- Добавлены fallback'и (`Image` sprite fallback + world-space fallback при flat screen-space кадре).
- Добавлены инструменты диагностики:
  - `Tools/PrefabBoard/Preview Debug` (raw кадры `ScreenSpace/WorldSpace/Final`)
  - экспорт debug PNG в `Temp/PrefabBoardPreviewDebug`
  - сборка реальной test-сцены `Assets/Scenes/Test.unity`:
    - `Tools/PrefabBoard/Create Test Scene/From Last Preview Capture`
    - `Tools/PrefabBoard/Create Test Scene/From Selected Prefab`

## Известные проблемы
- В проблемном кейсе пользователя preview всё ещё может показывать пустой/серый результат; причина пока не подтверждена.
- Нет автоматизированного integration-теста preview в Unity Editor.
- Поведение `Auto` основано на эвристике stretch rect и может требовать подстройки для нестандартных UI-иерархий.
- Внешний drag в Scene/Hierarchy для MVP остаётся на `Ctrl+LMB`.

## Развитие решений
- Preview система стала параметризованной (режим + canvas size) и воспроизводимой через test scene builder.
- Конфиг камеры/canvas для debug-сцены теперь берётся из того же `PreviewCache`, что уменьшает расхождение между runtime preview и ручной диагностикой.
- Проверка изменений после `last_checked_commit` выполнена:
  - `git log 25bd6fc...HEAD`
  - найдено: `fefe755`, `f4160da`, `3199bed`.

## Контроль изменений
- last_checked_commit: fefe755
- last_checked_date: 2026-02-26
