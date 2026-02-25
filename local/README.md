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
- `PrefabBoard/Assets/Editor/PrefabBoard/Services`
  - `BoardRepository`
  - `AssetGuidUtils`
  - `PreviewCache`
  - `BoardUndo`
- `PrefabBoard/Assets/Editor/PrefabBoard/UI`
  - `PrefabBoardWindow`
  - `BoardCanvasElement`
  - `PrefabCardElement`
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
