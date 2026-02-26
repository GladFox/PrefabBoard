# Active Context

## Текущие задачи
1. Сделать preview rig конфигурируемым: не только hardcoded-кодом, но и через prefab template + settings asset.
2. Сохранить два сценария рендера UI:
   - fullscreen
   - single-control
3. Обеспечить совместимость drag-out fallback с `Input System` и `Legacy Input`.
4. Обновить документацию и зафиксировать изменения в git.

## Последние изменения (текущая сессия)
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
1. В Unity проверить drag-out карточки в `Scene/Hierarchy` в трёх режимах `Active Input Handling`: `Input System`, `Legacy`, `Both`.
2. Подтвердить отсутствие `InvalidOperationException` из `UnityEngine.Input`.
3. При необходимости добавить editor-тест/diagnostic toggle для контроля backend-ветки ввода.

## План (REQUIREMENTS_OWNER)
1. Локализовать место вызова Legacy Input API в drag-out fallback.
2. Заменить проверку нажатия ЛКМ на backend-agnostic helper с preprocessor guards.
3. Проверить код на прямые вызовы `UnityEngine.Input` без guard и зафиксировать изменения.

## Стратегия (ARCHITECT)
- Не вносить runtime-зависимость от `Input System` пакета, использовать только условную компиляцию.
- Сохранить существующий UX drag-out без изменения контрактов `BoardCanvasElement`.

## REVIEWER checklist
- Нет прямых вызовов `Input.GetMouseButton` в коде, исполняемом при `Input System only`.
- Код компилируется при отсутствии `Input System` package (ветка обёрнута в `#if ENABLE_INPUT_SYSTEM`).
- Документация и Memory Bank синхронизированы.

## QA_TESTER заметки
- Автоматический Unity compile/smoke в этой среде не запускался.
- Нужна ручная проверка в Unity Editor (`Input System`/`Legacy`/`Both` + drag-out smoke).
