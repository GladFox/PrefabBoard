# Active Context

## Текущие задачи
1. Добавить per-item переключатель режима рендера preview на карточке.
2. Сохранять выбранный режим в `BoardItemData`.
3. Поддержать режимы размера холста для рендера: `Resolution`, `Control Size`, `Auto`.

## Последние изменения
- В `BoardItemData` добавлен enum `BoardItemPreviewRenderMode` и поле `previewRenderMode` (сериализуется в ассете).
- В `PrefabCardElement` добавлена кнопка режима preview (`A/R/C`) и callback для Canvas.
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

## Следующие шаги
1. Ручной smoke-test в Unity Editor на UI prefab с фиксированным размером и full-stretch.
2. Оценить UX подписи режимов (`A/R/C`) и при необходимости заменить на иконки.
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
