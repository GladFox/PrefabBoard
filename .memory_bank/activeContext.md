# Active Context

## Текущие задачи
1. Провести smoke в Unity после правок верхнего toolbar.
2. Проверить, что dropdown `Board` показывает все `PrefabBoardAsset` из проекта.
3. Проверить автo-refresh списка досок при возврате фокуса в окно.

## Последние изменения
- Исправлена верхняя панель: убран конфликт label/control в toolbar.
- Удалено поле `Name` из toolbar.
- Toolbar переведен в `nowrap`.
- Поиск досок переведен на `AssetDatabase.FindAssets("t:PrefabBoardAsset")` по всему проекту.
- В `PrefabBoardWindow` добавлен `OnFocus()` для refresh bindings.

## Следующие шаги
1. Если UX toolbar ок — оставить текущий compact layout.
2. При необходимости добавить явную сортировку/группировку досок по папкам.

## План (REQUIREMENTS_OWNER)
1. Починить визуальное выравнивание контролов в верхней панели.
2. Обеспечить полный список досок в dropdown `Board`.

## Стратегия (ARCHITECT)
- Минимизировать элементы в toolbar, чтобы избежать переносов и конфликтов label-контрол.
- Источник данных списка досок — глобальный поиск `PrefabBoardAsset`.

## REVIEWER checklist
- Toolbar без рассинхрона caption/control.
- `Board` dropdown видит все доски проекта.
- Refresh списка досок срабатывает при `OnFocus`.

## QA_TESTER заметки
- Локальная `dotnet build` успешна.
- Нужна ручная проверка в Unity Editor.