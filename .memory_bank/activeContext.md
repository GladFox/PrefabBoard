# Active Context

## Текущие задачи
1. Реализовать корректный preview для uGUI prefab (не дефолтная заглушка).
2. Сохранить совместимость MVP и не менять контракты Canvas/Data.
3. Обновить документацию и зафиксировать изменения в git.

## Последние изменения
- Добавлен fallback рендер для UI prefab в `PreviewCache`:
  - prefab инстанцируется во временной preview scene;
  - Canvas переводится в `WorldSpace` для рендера камерой;
  - bounds считаются по `Renderer` + `RectTransform`;
  - изображение рендерится в `RenderTexture` и кэшируется.
- Исправлено поведение кэша preview:
  - mini thumbnail больше не кэшируется, пока `AssetPreview` ещё грузится;
  - это позволяет карточке обновиться до полноценного preview после загрузки.

## Следующие шаги
1. Проверить в Unity Editor несколько uGUI prefab (включая nested Canvas).
2. Оценить, нужен ли фон/подсветка для светлых UI элементов.
3. После smoke-test при необходимости добавить тонкую настройку framing/padding.

## План (REQUIREMENTS_OWNER)
1. Обеспечить, чтобы uGUI prefab отображался содержимым, а не дефолтной иконкой.
2. Не ломать существующий pipeline preview для не-UI префабов.
3. Сохранить undo/data контракты без изменений.

## Стратегия (ARCHITECT)
- Изменение локализовано в `Services/PreviewCache.cs`.
- Data-модель и UI события не изменяются.
- Для устойчивости добавлен явный lifecycle cleanup временных ресурсов.

## REVIEWER checklist
- Временная scene закрывается в `finally`.
- `RenderTexture` освобождается, custom `Texture2D` уничтожаются на `Invalidate/Clear`.
- UI fallback применяется до `AssetPreview` для uGUI prefab.
- Mini thumbnail не кэшируется в режиме `loading=true`.

## QA_TESTER заметки
- Автоматический Unity compile/smoke в этой среде не запускался.
- Нужна ручная проверка в Editor: загрузка доски, добавление uGUI prefab, визуал карточки.
