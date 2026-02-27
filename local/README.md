# PrefabBoard Architecture

## Status
MVP implementation in progress (`feat/tz-alignment`). Current version: `v0.1.0`.

## Scope
Editor-only инструмент `Prefab Board` на Unity UI Toolkit:
- бесконечная 2D-доска с сеткой, pan и zoom
- карточки префабов (данные в ScriptableObject)
- группы (frames)
- drag&drop `Project -> Canvas`
- внешний drag карточки в `Scene/Hierarchy` для инстанса префаба
- поддержка нескольких досок через отдельные board-файлы (`*.asset`)

## Repository Structure
- `PrefabBoard/Assets/Editor/PrefabBoard/Data`
  - `PrefabBoardAsset`
  - `BoardItemData`
  - `BoardGroupData`
  - `BoardViewSettings`
  - `PreviewRigSettingsAsset`
- `PrefabBoard/Assets/Editor/PrefabBoard/Services`
  - `BoardRepository`
  - `AssetGuidUtils`
  - `PreviewCache`
  - `PreviewAssetChangeWatcher`
  - `PreviewRigSettingsProvider`
  - `PreviewDebugCapture`
  - `GameViewResolutionUtils`
  - `BoardUndo`
- `PrefabBoard/Assets/Editor/PrefabBoard/UI`
  - `PrefabBoardWindow`
  - `BoardCanvasElement`
  - `BoardOutlineElement`
  - `PrefabCardElement`
  - `PrefabBoardAssetEditor`
  - `TextPromptWindow`
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

### Board Discovery
- Каждая доска хранится отдельным файлом (`PrefabBoardAsset`), по умолчанию создаётся в `Assets/Editor/PrefabBoards/Boards/*.asset`.
- Список досок в UI строится через поиск `t:PrefabBoardAsset` по всему проекту (не только по одной папке).
- Последняя открытая доска хранится локально в `EditorPrefs` (`PrefabBoard.LastOpenedBoardId`), а не в shared asset.

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
- `LMB drag` за границы окна PrefabBoard: внешний drag в Scene/Hierarchy (создание instance prefab)
- `MMB` или `Space + LMB`: pan
- `Mouse wheel`: zoom to cursor
- `Home` в правой панели навигации: reset view (`pan=0`, `zoom=1`)
- `Delete/Backspace`: удалить выделение
- `Ctrl/Cmd + D`: дублировать выбранные карточки
- `Ctrl/Cmd + Z`: undo
- `Ctrl/Cmd + Y` и `Ctrl/Cmd + Shift + Z`: redo
- `F`: frame selection
- Box selection: drag по пустому месту
- `Project -> Canvas` drag-over: ghost preview позиции добавления
- RMB по пустому месту Canvas: контекстное меню `Create Group`
- Группы: drag по рамке/заголовку + resize через handles по краям/углам + rename через context menu
- Правая панель (`Board Items`): отдельные секции `Anchors` и `Elements`, клик по item фокусирует карточку, клик по anchor центрирует и фреймит группу

## Preview Rendering and Sizing
Preview режим в UI зафиксирован в `Auto` (кнопка переключения режима на карточке удалена).

Правила авто-размера карточки и холста:
- если у root `RectTransform` prefab задан фиксированный размер, карточка создаётся и рендерится ровно этого размера;
- если root `RectTransform` растянут по родителю (`anchorMin=0`, `anchorMax=1`, offsets=0), карточка создаётся размером текущего canvas resolution (`GameView`).
- если включён `PreviewRigSettings` в режиме `PrefabTemplate` и у template есть `CanvasScaler`, для stretch используется `CanvasScaler.referenceResolution` template rig.

Для legacy карточек с placeholder размером `220x120` при первом рендере выполняется авто-пересчёт по тем же правилам.

Preview кэшируется по ключу `prefabGuid + mode + canvasSize`.
UI preview рендерится через временный rig в additive-сцене:
- `Camera`
- `ScreenSpaceCamera Canvas`
- `Content` контейнер, в который инстанцируется prefab
В screen-space pipeline используется явная маршрутизация по `UI` layer (`camera.cullingMask = UI`).
Если screen-space кадр пустой, выполняется fallback world-space рендер.
При изменении `.prefab` ассета кэш соответствующего preview автоматически инвалидируется через `AssetPostprocessor`.
Перерисовка в Canvas делается для изменённых карточек и только когда панель видима.
Отладочные UI (`Preview Debug` окно и создание `Test.unity`) удалены из пользовательского workflow.

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

## Board Assets
- `BoardLibrary` удалён из проекта.
- Каждая доска живёт отдельным `PrefabBoardAsset` файлом.
- В инспекторе `PrefabBoardAsset` есть кнопка `Open`, открывающая нужную доску в `PrefabBoardWindow`.

## Data Consistency
- При duplicate board ID групп пересоздаются, а `item.groupId` ремапится на новые group IDs.
- Если исходная группа не может быть сопоставлена, у дубликата `item.groupId` очищается.

## Known MVP Limits
- Внешний drag для инстанса префаба сделан через `Ctrl+LMB` (чтобы не конфликтовать с внутренним перемещением).
- Группы в текущем UX используются как визуальные anchors (без автоматической привязки/перетаскивания карточек вместе с группой).
- Preview: для uGUI prefab используется Canvas+Camera рендер во временной additive-сцене (без подмены `Image.sprite` при `null`); для остальных asset'ов сохраняется `AssetPreview` (асинхронная догрузка).
