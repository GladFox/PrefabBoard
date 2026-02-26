# Progress

## Что работает
- MVP Prefab Board функционирует (canvas, pan/zoom, карточки, группы, dnd, multi-board).
- Для UI prefab работает custom preview через `Canvas+Camera` fallback.
- Добавлены режимы рендера preview на уровне элемента доски:
  - `Auto`
  - `Resolution`
  - `Control Size`
- Добавлена кнопка-переключатель режима на карточке (`A/R/C`) с сохранением в `BoardItemData`.
- Кнопка режима перенесена в правый нижний `action`-блок карточки (блок расширяем под будущие кнопки).
- Режим preview копируется при дублировании карточек.
- Кэш preview теперь учитывает режим и размер холста (`prefabGuid + mode + canvasSize`).
- UI preview pipeline переведён на preview rig (`Camera + ScreenSpaceCanvas + Content`) для более стабильного рендера UI prefab.
- Добавлен fallback рендер world-space и warning-диагностика в Console для случаев пустого preview.
- Отключено отбрасывание preview по эвристике «пустого кадра»; приоритет отдаётся реальному рендер-результату пайплайна.
- Для режима `Control Size` canvas size теперь совпадает с размером элемента (`item.size`) без дополнительного viewport clamp.

## Известные проблемы
- Нет автоматизированного integration-теста preview в Unity Editor.
- Поведение `Auto` основано на эвристике stretch rect и может требовать тонкой подстройки для нестандартных иерархий UI.
- Требуется ручная проверка, что проблема пустого серого рендера закрыта на целевых префабах.
- Внешний drag в Scene/Hierarchy для MVP остаётся на `Ctrl+LMB`.

## Развитие решений
- Preview система расширена до параметризованной схемы рендера с персистентным per-item режимом.
- Инвалидация кэша расширена: удаляются все варианты preview одного prefab при изменении режима.
- Проверка изменений после `last_checked_commit` выполнена:
  - `git log 25eec60...HEAD` -> `aaa128f feat(editor): add canvas fallback preview for UI prefabs`.

## Контроль изменений
- last_checked_commit: d700552
- last_checked_date: 2026-02-26
