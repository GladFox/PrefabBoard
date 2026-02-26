# Active Context

## Текущие задачи
1. Добавить reproducible debug-сцену `Assets/Scenes/Test.unity`, собранную из того же preview rig, что используется в `PreviewCache`.
2. Дать быстрый entry point из меню Unity для сборки test-сцены:
   - из последнего preview capture
   - из выделенного prefab
3. Зафиксировать изменения и обновить документацию/Memory Bank.

## Последние изменения (текущая сессия)
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
1. В Unity выполнить `Tools/PrefabBoard/Create Test Scene/From Last Preview Capture` и открыть `Assets/Scenes/Test.unity`.
2. Проверить, виден ли UI prefab в `GameView` и корректны ли `Camera/Canvas/CanvasScaler/layer` настройки.
3. Если UI всё ещё пустой, сравнить кадры из `Tools/PrefabBoard/Preview Debug` со сценой `Test.unity`.

## План (REQUIREMENTS_OWNER)
1. Снять параметры из текущего preview pipeline.
2. Построить test scene теми же настройками.
3. Задокументировать workflow и закоммитить.

## Стратегия (ARCHITECT)
- Не дублировать второй риг: повторно использовать существующие методы настройки preview в `PreviewCache`.
- Вынести запуск в отдельный menu entry без вмешательства в логику `BoardCanvasElement`.

## REVIEWER checklist
- Меню создаёт сцену без ручной настройки объектов.
- Конфигурация camera/canvas совпадает с preview pipeline.
- Документация и Memory Bank синхронизированы.

## QA_TESTER заметки
- Автоматический Unity compile/smoke в этой среде не запускался.
- Нужна ручная проверка в Unity Editor (создание `Test.unity` + визуальная валидация).
