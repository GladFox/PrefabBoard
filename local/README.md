# PrefabBoard Architecture

## Status
MVP implementation in progress (`feat/tz-alignment`).

## Scope
Editor-only инструмент `Prefab Board` на Unity UI Toolkit:
- бесконечная 2D-доска с сеткой, pan и zoom
- карточки префабов (данные в ScriptableObject)
- группы (frames)
- drag&drop `Project -> Canvas`
- внешний drag карточки в `Scene/Hierarchy` для инстанса префаба
- поддержка нескольких досок через библиотеку

## Repository Structure
- `PrefabBoard/Assets/Editor/PrefabBoard/Data`
  - `PrefabBoardAsset`
  - `BoardItemData`
  - `BoardGroupData`
  - `BoardLibraryAsset`
  - `BoardViewSettings`
  - `PreviewRigSettingsAsset`
- `PrefabBoard/Assets/Editor/PrefabBoard/Services`
  - `BoardRepository`
  - `AssetGuidUtils`
  - `PreviewCache`
  - `PreviewRigSettingsProvider`
  - `PreviewDebugCapture`
  - `GameViewResolutionUtils`
  - `BoardUndo`
- `PrefabBoard/Assets/Editor/PrefabBoard/UI`
  - `PrefabBoardWindow`
  - `BoardCanvasElement`
  - `PrefabCardElement`
  - `PreviewDebugWindow`
  - `GroupFrameElement`
  - `SelectionOverlayElement`
  - `BoardToolbarElement`
- `PrefabBoard/Assets/Editor/PrefabBoard/Styles`
  - `PrefabBoard.uss`

## Data Model
### PrefabBoardAsset
Хранит состояние одной доски:
- `boardId`, `boardName`
- `pan` (пиксели Canvas)
- `zoom` (масштаб)
- `items`, `groups`
- `viewSettings` (grid/snap/zoom limits)

### BoardItemData
Карточка префаба:
- `id`, `prefabGuid`
- `position`, `size` (world units)
- `titleOverride`, `note`
- `tagColor`, `tags`
- `groupId`

### BoardGroupData
Группа/фрейм:
- `id`, `name`
- `rect` (world)
- `color`, `zOrder`

### BoardLibraryAsset
Менеджер досок:
- `boards`
- `lastOpenedBoardId`

## Coordinate Contract
Сохранение позиций в world, навигация через `pan`/`zoom`:
- `screen = world * zoom + panPx`
- `world = (screen - panPx) / zoom`

`pan` хранится в пикселях Canvas.
`zoom` ограничен `viewSettings.minZoom .. viewSettings.maxZoom`.

Zoom-to-cursor реализован через фиксацию точки под курсором при смене масштаба.

## Interaction Contract
- `LMB drag` на карточке: перемещение по доске
- `Ctrl+LMB` на карточке: внешний drag в Scene/Hierarchy
- Кнопка режима preview на карточке (в правом нижнем action-блоке): цикл `A -> R -> C`
- `MMB` или `Space + LMB`: pan
- `Mouse wheel`: zoom to cursor
- `Home` в toolbar: reset view (`pan=0`, `zoom=1`)
- `Delete/Backspace`: удалить выделение
- `Ctrl/Cmd + D`: дублировать выбранные карточки
- `F`: frame selection
- Box selection: drag по пустому месту
- `Project -> Canvas` drag-over: ghost preview позиции добавления
- `Add To Group` в контекстном меню карточки:
  если карточка в selection — применяется к selection, иначе к карточке под курсором

## Preview Rendering Modes
Для каждой карточки хранится `previewRenderMode`:
- `Auto`:
  - если основной `RectTransform` растянут на экран (`anchorMin=0`, `anchorMax=1`, offset=0), используется размер `Resolution`
  - иначе используется `Control Size`
- `Resolution`: размер холста для рендера берётся из выбранного разрешения `GameView` (квантование до 32px)
- `Control Size`: размер холста берётся из `item.size` (с fallback на размер `RectTransform`)

Дополнительно `previewRenderMode` управляет компоновкой контента в preview rig:
- `Resolution` -> fullscreen fit (контент растягивается на весь preview canvas)
- `Control Size` -> single-control fit (контент фиксируется по размеру контрола)
- `Auto` -> авто-режим (сохраняется прежняя логика: stretch остаётся stretch, фиксированный control центрируется)

Preview кэшируется по ключу `prefabGuid + mode + canvasSize`.
UI preview рендерится через временный rig в preview scene:
- `Camera`
- `ScreenSpaceCamera Canvas`
- `Content` контейнер, в который инстанцируется prefab
В screen-space pipeline используется явная маршрутизация по `UI` layer (`camera.cullingMask = UI`).
Если screen-space кадр пустой, выполняется fallback world-space рендер.
Для диагностики доступно окно `Tools/PrefabBoard/Preview Debug`:
- показывает raw кадры `ScreenSpace`, `WorldSpace`, `Final`
- показывает метаданные (prefab path, mode, canvas size, время)
- умеет сохранять PNG в `Temp/PrefabBoardPreviewDebug`
- можно собрать реальную сцену `Assets/Scenes/Test.unity` с тем же preview rig через:
  - `Tools/PrefabBoard/Create Test Scene/From Last Preview Capture`
  - `Tools/PrefabBoard/Create Test Scene/From Selected Prefab`
  Это создаёт `PrefabBoardPreviewCamera + PrefabBoardPreviewCanvas + Content + instance prefab` с теми же настройками, что в preview pipeline.

Canvas и карточки клиппируются по своим границам, чтобы контент не выходил поверх toolbar и за пределы элемента.

## Preview Rig Configuration
Источник preview rig настраивается через asset `PreviewRigSettings`:
- меню: `Tools/PrefabBoard/Preview Rig Settings`
- путь по умолчанию: `Assets/Editor/PrefabBoard/Settings/PreviewRigSettings.asset`

Поддерживаемые источники:
- `BuiltIn`: камера/canvas/content создаются кодом (fallback по умолчанию)
- `PrefabTemplate`: риг берётся из prefab (камера/canvas/content резолвятся по путям в settings)

## Persistence and Undo
Изменения данных выполняются через:
- `Undo.RecordObject(...)`
- `EditorUtility.SetDirty(...)`

Состояние хранится в ScriptableObject-ассетах, без создания runtime GameObject для карточек/групп.

## Data Consistency
- При duplicate board ID групп пересоздаются, а `item.groupId` ремапится на новые group IDs.
- Если исходная группа не может быть сопоставлена, у дубликата `item.groupId` очищается.

## Known MVP Limits
- Внешний drag для инстанса префаба сделан через `Ctrl+LMB` (чтобы не конфликтовать с внутренним перемещением).
- Ресайз групп (handles) не включен в текущий MVP.
- Preview: для uGUI prefab используется Canvas+Camera fallback в preview scene; для остальных asset'ов сохраняется `AssetPreview` (асинхронная догрузка).
