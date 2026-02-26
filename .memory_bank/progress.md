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
- Для `PrefabTemplate` stretch-размер теперь может браться из `CanvasScaler.referenceResolution` шаблонного rig, чтобы совпадать с фактическим canvas шаблона.
- Добавлен drag-out из карточки: перетаскивание за пределы окна PrefabBoard запускает внешний drag prefab в Scene/Hierarchy.
- Drag-out усилен fallback-проверкой выхода курсора из окна (`EditorWindow.mouseOverWindow`), чтобы не зависеть от потери `PointerMove` после выхода из Canvas.
- Drag-out fallback сделан совместимым с обеими системами ввода:
  - `Input System` (`Mouse.current.leftButton.isPressed`)
  - `Legacy Input` (`Input.GetMouseButton(0)`)
  - выбор ветки через compile-guards `ENABLE_INPUT_SYSTEM` / `ENABLE_LEGACY_INPUT_MANAGER`.
- Расширен zoom-out диапазон доски:
  - дефолт `BoardViewSettings.minZoom` снижен с `0.2` до `0.02`;
  - runtime `MinZoom` в `BoardCanvasElement` ограничивается значением не выше `0.02`,
    поэтому старые доски с сохранённым `0.2` тоже могут отъезжать дальше.

## Известные проблемы
- В проблемном кейсе пользователя preview всё ещё может показывать пустой/серый результат; причина пока не подтверждена.
- Нет автоматизированного integration-теста preview в Unity Editor.
- Поведение auto-size основано на root `RectTransform`; для сложных префабов, где целевой UI-контрол не является root, может потребоваться дополнительное правило выбора таргет-контрола.
- Для drag-out fallback нужна ручная проверка в конфигурациях `Input System`/`Legacy`/`Both` (автотестов нет).
- Новые пределы зума требуют ручной UX-проверки на очень больших канвасах (читабельность сетки/карточек на экстремальном отдалении).
- Режим `PrefabTemplate` требует корректно размеченный rig prefab (camera/canvas/content), иначе используется built-in fallback.

## Развитие решений
- Preview система стала параметризованной (режим + canvas size) и воспроизводимой через test scene builder.
- Конфиг камеры/canvas для debug-сцены теперь берётся из того же `PreviewCache`, что уменьшает расхождение между runtime preview и ручной диагностикой.
- Проверка изменений после `last_checked_commit` выполнена:
  - `git log 00b2557...HEAD`
  - найдено: `78b2282`.

## Контроль изменений
- last_checked_commit: 78b2282
- last_checked_date: 2026-02-26
