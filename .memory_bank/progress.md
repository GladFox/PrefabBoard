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
- Добавлен world-space fallback при flat screen-space кадре.
- Добавлены инструменты диагностики:
  - `Tools/PrefabBoard/Preview Debug` (raw кадры `ScreenSpace/WorldSpace/Final`)
  - экспорт debug PNG в `Temp/PrefabBoardPreviewDebug`
  - сборка реальной test-сцены `Assets/Scenes/Test.unity`:
    - `Tools/PrefabBoard/Create Test Scene/From Last Preview Capture`
    - `Tools/PrefabBoard/Create Test Scene/From Selected Prefab`
- Для `Test.unity` отключена подмена fallback sprite (чтобы сохранённая сцена не показывала искусственные правки UI).
- Временные инстансы preview рендерера создаются как обычные копии (`Object.Instantiate`) без prefab connection.
- Появилась конфигурация preview rig через settings asset:
  - `Tools/PrefabBoard/Preview Rig Settings`
  - источник рига: `BuiltIn` или `PrefabTemplate`
- В `PreviewCache` добавлен выбор источника рига и унифицированная настройка camera/canvas/content.
- Режимы карточки `Resolution/Control Size` теперь управляют не только размером холста, но и fit-режимом контента:
  - fullscreen
  - single-control
- Исправлен кейс `PrefabTemplate`, где `Canvas` из шаблона мог иметь `localScale=0`:
  теперь camera/canvas/content трансформы нормализуются при сборке preview rig.
- Временный рендер UI больше не использует `PreviewScene`; используется временная additive-сцена, ближе к обычному сценарию рендера uGUI.
- Подмена `Image.sprite` при `null` полностью отключена в preview pipeline.
- Кнопка ручного переключения preview mode убрана; в UI используется auto-режим.
- Размер карточки теперь рассчитывается автоматически по prefab:
  - fixed-size root `RectTransform` -> размер контрола;
  - stretch root `RectTransform` -> размер `GameView` resolution.
- Для старых карточек с `220x120` добавлен авто-пересчёт размера по правилам выше.
- Визуальный контейнер карточки переведён в 1:1 отображение (без постоянных padding/border), чтобы размер элемента на доске соответствовал размеру рендера.
- Добавлена авто-инвалидация preview при изменении prefab-ассета (`AssetPostprocessor` -> `PreviewCache.InvalidateByAssetPath`).
- Canvas обновляет только затронутые карточки и только в видимом состоянии панели.
- Исправлено обновление размера fullscreen-карточек: размер элемента теперь синхронизируется с актуальным resolution, а не только при первом создании.

## Известные проблемы
- В проблемном кейсе пользователя preview всё ещё может показывать пустой/серый результат; причина пока не подтверждена.
- Нет автоматизированного integration-теста preview в Unity Editor.
- Поведение auto-size основано на root `RectTransform`; для сложных префабов, где целевой UI-контрол не является root, может потребоваться дополнительное правило выбора таргет-контрола.
- Внешний drag в Scene/Hierarchy для MVP остаётся на `Ctrl+LMB`.
- Режим `PrefabTemplate` требует корректно размеченный rig prefab (camera/canvas/content), иначе используется built-in fallback.

## Развитие решений
- Preview система стала параметризованной (режим + canvas size) и воспроизводимой через test scene builder.
- Конфиг камеры/canvas для debug-сцены теперь берётся из того же `PreviewCache`, что уменьшает расхождение между runtime preview и ручной диагностикой.
- Проверка изменений после `last_checked_commit` выполнена:
  - `git log 4aed78d...HEAD`
  - найдено: `298847a`.

## Контроль изменений
- last_checked_commit: 298847a
- last_checked_date: 2026-02-26
