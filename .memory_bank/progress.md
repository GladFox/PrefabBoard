# Progress

## Что работает
- EditorWindow с canvas, pan/zoom, карточками и группами.
- Multi-board через отдельные `PrefabBoardAsset`.
- Открытие доски из инспектора кнопкой `Open`.
- Группы: создание (RMB canvas), rename, drag, resize.
- Правая панель: Home + фокус по anchor/item.
- Dropdown `Board` показывает все доски проекта.

## Известные проблемы
- Нужен ручной Unity smoke-test после текущих UI правок.
- Автотестов UI Toolkit interaction пока нет.

## Развитие решений
- Упрощен toolbar для стабильного выравнивания label/control.
- Источник списка досок переведен на глобальный asset search.
- Добавлен refresh bindings при `OnFocus` окна.

## Контроль изменений
- last_checked_commit: 597cf48
- last_checked_date: 2026-02-27