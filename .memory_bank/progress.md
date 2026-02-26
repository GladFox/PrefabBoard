# Progress

## Что работает
- MVP Prefab Board функционирует (canvas, pan/zoom, карточки, группы, dnd, multi-board).
- Для UI prefab работает custom preview через `Canvas+Camera` fallback.
- Добавлены режимы рендера preview на уровне элемента доски:
  - `Auto`
  - `Resolution`
  - `Control Size`
- Добавлена кнопка-переключатель режима на карточке (`A/R/C`) с сохранением в `BoardItemData`.
- Режим preview копируется при дублировании карточек.
- Кэш preview теперь учитывает режим и размер холста (`prefabGuid + mode + canvasSize`).

## Известные проблемы
- Нет автоматизированного integration-теста preview в Unity Editor.
- Поведение `Auto` основано на эвристике stretch rect и может требовать тонкой подстройки для нестандартных иерархий UI.
- Внешний drag в Scene/Hierarchy для MVP остаётся на `Ctrl+LMB`.

## Развитие решений
- Preview система расширена до параметризованной схемы рендера с персистентным per-item режимом.
- Инвалидация кэша расширена: удаляются все варианты preview одного prefab при изменении режима.
- Проверка изменений после `last_checked_commit` выполнена:
  - `git log 25eec60...HEAD` -> `aaa128f feat(editor): add canvas fallback preview for UI prefabs`.

## Контроль изменений
- last_checked_commit: aaa128f
- last_checked_date: 2026-02-26
