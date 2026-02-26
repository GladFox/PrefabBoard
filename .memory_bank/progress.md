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
- Для режима `Resolution` размер canvas берётся из выбранного resolution в `GameView`.
- Добавлен клиппинг canvas/card, чтобы визуал не выходил за границы окна и карточек.
- Добавлено debug-окно `Tools/PrefabBoard/Preview Debug` с raw кадрами `ScreenSpace/WorldSpace/Final`.
- Добавлен экспорт debug PNG в `Temp/PrefabBoardPreviewDebug` для разборов проблемных prefab.
- Для preview-инстанса добавлена подстановка built-in `UISprite` в `UI.Image` без source sprite, чтобы избежать пустой прозрачной отрисовки.
- Добавлен runtime fallback-sprite (white 2x2), если built-in `UISprite` недоступен.
- Если screen-space рендер даёт плоский фон, автоматически пробуется world-space и выбирается неплоский кадр.
- Исправлен wrapper canvas для screen-space preview (`RenderMode.ScreenSpaceCamera`).
- Добавлен явный layer routing на UI слой + `camera.cullingMask = UI`.

## Известные проблемы
- Нет автоматизированного integration-теста preview в Unity Editor.
- Поведение `Auto` основано на эвристике stretch rect и может требовать тонкой подстройки для нестандартных иерархий UI.
- Требуется ручная проверка в Unity после трёх bugfix'ов (особенно `Dialog.prefab` и prefab'ы с собственным Canvas).
- Внешний drag в Scene/Hierarchy для MVP остаётся на `Ctrl+LMB`.

## Развитие решений
- Preview система расширена до параметризованной схемы рендера с персистентным per-item режимом.
- Инвалидация кэша расширена: удаляются все варианты preview одного prefab при изменении режима.
- Проверка изменений после `last_checked_commit` выполнена:
  - `git log 25eec60...HEAD` -> `aaa128f feat(editor): add canvas fallback preview for UI prefabs`.

## Контроль изменений
- last_checked_commit: 25bd6fc
- last_checked_date: 2026-02-26
