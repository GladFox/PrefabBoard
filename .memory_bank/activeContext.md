# Active Context

## Текущие задачи
1. Сделать preview rig конфигурируемым: не только hardcoded-кодом, но и через prefab template + settings asset.
2. Сохранить два сценария рендера UI:
   - fullscreen
   - single-control
3. Обеспечить совместимость drag-out fallback с `Input System` и `Legacy Input`.
4. Расширить zoom-out диапазон доски.
5. Убрать дедупликацию элементов при drag-out в Scene/Hierarchy.
6. Добавить resize групп и найти/исправить причину, почему drag групп не срабатывает.
7. Добавить горячие клавиши undo/redo (`Ctrl+Z`, `Ctrl+Y`) для отката изменений позиций.
8. Добавить правую панель списка (`Anchors` + `Elements`) с фокусом по клику.
9. Перевести multi-board хранение на полностью отдельные board asset-файлы без shared списка в одном asset.
10. Подготовить релиз: добавить версию, release notes, commit/push.

## Последние изменения (текущая сессия)
- Подготовлен релизный baseline:
  - добавлен файл версии `VERSION` со значением `0.1.0`;
  - добавлен `RELEASE_NOTES.md` для `v0.1.0`;
  - в `local/README.md` статус дополнен текущей версией `v0.1.0`.
- Multi-board хранение переведено на board-per-file workflow:
  - `BoardRepository` больше не использует `BoardLibraryAsset.boards` как источник списка;
  - список досок строится через `AssetDatabase.FindAssets("t:PrefabBoardAsset", BoardsFolder)`;
  - создание/дублирование/удаление доски работают напрямую с отдельными `*.asset` файлами в `Assets/Editor/PrefabBoards/Boards`.
- `last opened board` переведён из shared asset в `EditorPrefs` (`PrefabBoard.LastOpenedBoardId`), чтобы переключение досок не писало общий файл.
- `PrefabBoardWindow` переведён на новую модель:
  - хранит runtime-список `_boards`;
  - использует `BoardRepository.GetAllBoards()` и `GetLastOrFirstBoard(IReadOnlyList<...>)`;
  - операции `New/Duplicate/Delete/Switch` работают без модификации общего списка в `BoardLibrary.asset`.
- Группы переведены в режим «anchors»:
  - убран `Add To Group` из контекстного меню карточки;
  - создание группы больше не перепривязывает selected карточки;
  - drag группы больше не двигает карточки внутри.
- Для drag/resize группы добавлен hit-test в `BoardCanvasElement.OnPointerDown`:
  - группа и handle определяются по world hit-test (`TryHitGroup`);
  - старт interaction больше не зависит от событий, доходящих до `GroupFrameElement`.
- Для undo/redo убраны записи операций камеры (`Zoom`, `ResetView`, `Frame/Focus`), чтобы `Ctrl+Z/Y` откатывали только изменения элементов/групп.
- Добавлена правая панель `BoardOutlineElement`:
  - отдельные секции `Anchors` и `Elements`;
  - клик по карточке вызывает фокус на item;
  - клик по группе центрирует и фреймит группу по её `rect`.
- В `PrefabBoardWindow` добавлен новый layout `toolbar + content row (canvas + right panel)`.
- В `BoardCanvasElement` добавлены методы фокуса:
  - `FocusItem(string itemId)`
  - `FocusGroup(string groupId)`
- В `GroupFrameElement` добавлены resize handles (8 сторон/углов) и событие `ResizePointerDown`.
- В `BoardCanvasElement` добавлен режим `ResizeGroup` с undoable изменением `group.rect`.
- В `BoardCanvasElement` улучшен старт drag/resize группы через `evt.currentTarget`-координаты, чтобы устранить кейс, где группа не начинала перетаскиваться.
- В `BoardCanvasElement` добавлены горячие клавиши:
  - `Ctrl/Cmd + Z` -> `Undo.PerformUndo()`
  - `Ctrl/Cmd + Y` и `Ctrl/Cmd + Shift + Z` -> `Undo.PerformRedo()`
- Canvas подписывается на `Undo.undoRedoPerformed` и полностью перестраивает визуал после undo/redo.
- В drag-out в Scene/Hierarchy убрана проверка уникальности элементов при сборке payload:
  - удалён фильтр `dragItems.All(...)` в `TryStartExternalDragFromCurrentDrag`;
  - payload формируется напрямую из текущего набора перетаскиваемых карточек, поэтому повторяющиеся карточки одного prefab не отбрасываются.
- Расширен zoom-out диапазон доски:
  - новый дефолт `BoardViewSettings.minZoom = 0.02` для новых досок;
  - в `BoardCanvasElement` минимум зума ограничен сверху значением `0.02`, чтобы старые доски с legacy `minZoom=0.2` тоже могли отъезжать дальше без ручной миграции.
- Исправлено падение при `Active Input Handling = Input System Package`:
  - в `BoardCanvasElement` убран прямой вызов `Input.GetMouseButton(0)` из scheduler fallback;
  - добавлен общий helper `IsPrimaryMouseButtonPressed()` с compile-guards:
    - `ENABLE_INPUT_SYSTEM` -> `UnityEngine.InputSystem.Mouse.current.leftButton.isPressed`
    - `ENABLE_LEGACY_INPUT_MANAGER` -> `UnityEngine.Input.GetMouseButton(0)`
  - fallback работает в режимах `Input System`, `Legacy`, `Both`.
- Усилен механизм drag-out в Scene/Hierarchy:
  - добавлен fallback-триггер через `EditorWindow.mouseOverWindow` в scheduler;
  - внешний drag стартует даже если после выхода курсора из Canvas перестали приходить `PointerMove` события.
- В drag-out поддержан multi-item payload (если во внутреннем drag выбрано несколько карточек).
- Добавлен drag-out workflow из доски в Scene/Hierarchy:
  - при `LMB drag` карточки за пределы Canvas запускается `DragAndDrop` prefab-asset;
  - если префаб отсутствует/не резолвится, внешний drag не стартует;
  - существующий `Ctrl+LMB` shortcut внешнего drag сохранён.
- Исправлен источник resolution для stretch-элементов:
  - если `PreviewRigSettings.rigSource = PrefabTemplate` и в rig есть `CanvasScaler`, используется `CanvasScaler.referenceResolution`;
  - это же resolution теперь используется и для размера карточки, и для preview canvas size.
- Исправлен рассинхрон размеров fullscreen-элементов:
  - размер карточки теперь пересчитывается по prefab-правилу на каждом refresh (не только для `220x120`);
  - для stretch root `RectTransform` размер всегда равен актуальному canvas resolution (`GameView`);
  - при изменении resolution выполняется автоматический refresh размеров.
- Добавлен авто-механизм обновления preview при изменении prefab:
  - `PreviewAssetChangeWatcher` (AssetPostprocessor) отслеживает `import/move` `.prefab`;
  - вызывает `PreviewCache.InvalidateByAssetPath(...)` только для изменённых prefab.
- `PreviewCache` теперь публикует событие `PreviewInvalidated(prefabGuid)`.
- `BoardCanvasElement` подписывается на invalidation-события и:
  - ставит в очередь только затронутые `prefabGuid`;
  - обновляет только изменённые карточки;
  - выполняет перерисовку только когда Canvas реально видим.
- Убрана кнопка переключения preview режима с карточки (`PrefabCardElement`), режим в UI зафиксирован в `Auto`.
- Добавлен автоматический расчёт размера карточки по prefab при добавлении (`Project -> Canvas`):
  - fixed-size `RectTransform` -> размер карточки = размер контрола;
  - stretch `RectTransform` -> размер карточки = текущий canvas resolution (`GameView`).
- Для legacy карточек с placeholder размером `220x120` добавлен авто-пересчёт размера при рендере.
- Убраны принудительные минимумы размеров карточки в пикселях (`80x44`) и внутренние отступы/рамки карточки, чтобы размер превью соответствовал размеру элемента 1:1.
- Отключена подмена `Image.sprite` при `null` в preview pipeline:
  - удалены вызовы `EnsureImagesHaveSprite(instance)` в screen/world рендер-ветках.
  - удалена fallback-логика генерации/назначения sprite в `PreviewCache`.
- Теперь preview рендерит UI как есть, без модификации `Image` компонентов.
- Для UI preview рендера сменён тип временной сцены:
  - вместо `EditorSceneManager.NewPreviewScene()` теперь создаётся временная обычная additive-сцена `NewSceneSetup.EmptyScene + NewSceneMode.Additive`;
  - после рендера сцена удаляется через `EditorSceneManager.CloseScene(..., true)` и восстанавливается предыдущая active scene.
- Это сделано для обхода кейса, где `ScreenSpaceCamera + uGUI` не выдаёт геометрию в `PreviewScene`.
- Найдена причина «камера рендерит только фон» в `PrefabTemplate` rig:
  - у template `Canvas` был `localScale = (0,0,0)`, из-за чего UI геометрия схлопывалась.
- В `PreviewCache` добавлена принудительная нормализация трансформов rig-компонентов:
  - `Camera`: `localPosition=(0,0,-10)`, `localRotation=identity`, `localScale=1`
  - `Canvas RectTransform`: `localPosition=0`, `localRotation=identity`, `localScale=1`
  - `Content RectTransform`: `localPosition=0`, `localRotation=identity`, `localScale=1`
- Добавлен `PreviewRigSettingsAsset` (`Data`) с параметрами preview rig:
  - `rigSource` (`BuiltIn` / `PrefabTemplate`)
  - `rigPrefab`
  - пути `cameraPath` / `canvasPath` / `contentPath`
  - параметры камеры/canvas (`background`, `near/far`, `planeDistance`, `forceUiLayer`)
- Добавлен `PreviewRigSettingsProvider` (`Services`):
  - загрузка/создание settings asset
  - меню `Tools/PrefabBoard/Preview Rig Settings`
- `PreviewCache` переработан:
  - новый этап `CreatePreviewRig(...)` с выбором источника рига (prefab template или built-in fallback)
  - общий конфиг `ConfigurePreviewCamera/Canvas/Content`
  - `TryRenderUiPrefabPreview*` теперь получает `renderMode` и мапит его в fit mode:
    - `Resolution -> Fullscreen`
    - `ControlSize -> SingleControl`
    - `Auto -> Auto`
  - `AttachInstanceToPreviewContent` поддерживает явные fit-режимы
  - `TryCreateTestScene` принимает `renderMode`, чтобы `Test.unity` повторял компоновку preview
- Убрано впечатление «изменения prefab» при debug-сборке:
  - в `TryCreateTestScene(...)` больше не вызывается `EnsureImagesHaveSprite(instance)` (fallback sprite не подставляется в сохранённой `Test.unity`).
  - инстанс для `Test`-сцены создаётся как `Object.Instantiate(prefabAsset)` (без prefab connection).
- Временные инстансы для preview pipeline (`ScreenSpace/WorldSpace`) тоже переключены на `Object.Instantiate(prefabAsset)` для полной изоляции от asset-связи.
- В `PreviewCache` добавлен API для сборки test-сцены:
  - `TryCreateTestSceneFromLastCapture(...)`
  - `TryCreateTestScene(...)`
- В `PreviewCache` добавлен путь по умолчанию `Assets/Scenes/Test.unity` и сборка сцены с тем же pipeline-набором:
  - `PrefabBoardPreviewCamera`
  - `PrefabBoardPreviewCanvas` (`ScreenSpaceCamera` + `CanvasScaler`)
  - `Content`
  - instance prefab, подготовленный через те же `AttachInstanceToPreviewContent`/`PrepareUiForPreviewScreenSpace`.
- `CreatePreviewCamera/CreatePreviewCanvas/CreatePreviewContent` расширены overload-методами с флагом `hidden`, чтобы использовать один и тот же конфиг как в preview scene, так и в обычной test scene.
- Добавлен `PreviewTestSceneMenu`:
  - `Tools/PrefabBoard/Create Test Scene/From Last Preview Capture`
  - `Tools/PrefabBoard/Create Test Scene/From Selected Prefab`
- `local/README.md` обновлён: добавлен workflow сборки `Test.unity` для ручной диагностики.

## Предыдущие изменения
- Переключение режима preview (`Auto/Resolution/ControlSize`) вынесено на карточку и сохраняется в `BoardItemData`.
- Добавлен preview debug pipeline (`PreviewDebugCapture` + `PreviewDebugWindow`).
- Preview UI переведён на `ScreenSpaceCamera` rig + layer routing на `UI` слой.
- Добавлены fallback'и для `Image` без sprite и world-space fallback pipeline.

## Следующие шаги
1. В Unity проверить collaborative сценарий: параллельные изменения разных board `.asset` без конфликтов в shared library файле.
2. Проверить `New/Duplicate/Delete/Switch` после перехода на `AssetDatabase.FindAssets`.
3. Проверить drag/resize/undo и outline smoke после рефактора репозитория.
4. Отдельно спроектировать optional-режим «приклеивания» карточки к верхней группе + правило запрета пересечений групп.

## План (REQUIREMENTS_OWNER)
1. Перевести репозиторий досок на обнаружение отдельных board asset-файлов.
2. Убрать зависимость окна от `BoardLibraryAsset.boards`.
3. Хранить `last opened board` локально в `EditorPrefs`.
4. Синхронизировать README/Memory Bank и закоммитить.

## Стратегия (ARCHITECT)
- Оставить `groupId` в data model для будущего optional-режима, но не использовать в текущем UX.
- Для undo/redo использовать нативный Unity Undo pipeline без собственного стека операций.
- Для drag/resize группы приоритизировать canvas-level hit-testing как более стабильный источник pointer interaction.
- Для правой панели использовать однонаправленный поток: `Canvas -> событие BoardDataChanged -> Outline.Rebuild()`.
- Для совместной работы не хранить динамический список досок в одном shared asset; каждая доска должна быть самостоятельным файлом.

## REVIEWER checklist
- Группы двигаются и ресайзятся; `group.rect` обновляется корректно.
- Undo/redo работает для изменения позиций/размеров после drag операций и не откатывает camera-only actions.
- Правая панель отображает `Anchors`/`Elements` и корректно фокусирует canvas.
- Multi-board операции не требуют записи в общий список досок в одном asset.
- Документация и Memory Bank синхронизированы.

## QA_TESTER заметки
- Автоматический Unity compile/smoke в этой среде не запускался.
- Нужна ручная проверка в Unity Editor (group drag/resize + undo/redo + outline focus).
