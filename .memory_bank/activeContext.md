# Active Context

## Текущие задачи
1. Починить отсутствие UI-рендера (серый квадрат) в preview карточек.
2. Стабилизировать pipeline рендера через выделенный preview rig.
3. Зафиксировать update в git.

## Последние изменения
- В `BoardItemData` добавлен enum `BoardItemPreviewRenderMode` и поле `previewRenderMode` (сериализуется в ассете).
- В `PrefabCardElement` добавлена кнопка режима preview (`A/R/C`) и callback для Canvas.
- Action-кнопки карточки вынесены в отдельный контейнер `pb-card-actions` (правый нижний угол, с расчётом на дальнейшее расширение).
- В `BoardCanvasElement`:
  - добавлена обработка переключения режима с `Undo`;
  - сохранение и dirty-mark в данных;
  - прокидывание режима и размеров в `PreviewCache`;
  - копирование режима при дублировании карточек.
- В `PreviewCache`:
  - ключ кэша теперь учитывает `prefabGuid + mode + canvasSize`;
  - реализованы стратегии размеров холста `Resolution`, `Control Size`, `Auto`;
  - `Auto` выбирает `Resolution` для stretch-to-screen rect, иначе `Control Size`;
  - инвалидация удаляет все кэш-варианты по `prefabGuid`.
  - UI preview переведён на `ScreenSpaceCamera` rig:
    - отдельная preview camera
    - отдельный canvas
    - `Content` контейнер с инстансом prefab
  - все canvases инстанса принудительно переводятся в `ScreenSpaceCamera`, с нормализацией `CanvasScaler`;
  - удалён world-space bounds framing для UI preview.
  - добавлен двойной pipeline рендера:
    1. `ScreenSpace` rig
    2. fallback `WorldSpace` camera framing
  - добавлен runtime-диагностический `Debug.LogWarning` при пустом кадре или exception в preview pipeline.
  - убрана эвристика отбрасывания «пустых» кадров, чтобы не падать в icon fallback при спорных кейсах.
  - в `Control Size` размер preview canvas теперь равен размеру элемента без viewport-clamp.
  - источник `Resolution` переключён с размера окна инструмента на выбранное разрешение `GameView`.
  - добавлен клиппинг `pb-canvas` и `pb-card`, чтобы карточки/контент не вылезали на toolbar и за границы элемента.

## Следующие шаги
1. Ручной smoke-test в Unity Editor на UI prefab с фиксированным размером и full-stretch.
2. Проверить визуально новый action-блок в правом нижнем углу на карточках разных размеров.
3. При необходимости добавить выбор режима через контекстное меню карточки.

## План (REQUIREMENTS_OWNER)
1. Реализовать выбор режима рендера прямо на карточке.
2. Обеспечить персистентность режима в данных доски.
3. Не ломать существующие сценарии drag/selection и pipeline preview.

## Стратегия (ARCHITECT)
- Изменения локализованы в `Data/BoardItemData`, `UI/PrefabCardElement`, `UI/BoardCanvasElement`, `Services/PreviewCache`.
- Слои данных и UI остаются разделёнными; PreviewCache остаётся сервисом, который получает параметры рендера извне.

## REVIEWER checklist
- Переключение режима делает один undo-step.
- Значение режима дублируется вместе с карточкой.
- Кнопка режима не конфликтует с drag/select по карточке.
- Кэш не возвращает preview не того режима/размера.

## QA_TESTER заметки
- Автоматический Unity compile/smoke не запускался в этой среде.
- Требуется ручная проверка в Unity Editor на нескольких типах UI prefab.
