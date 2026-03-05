# Active Context

## Current Tasks
1. Validate `Ctrl+LMB` external drag from board cards into Scene/Hierarchy/Prefab Mode.
2. Validate green ghost behavior while drag stays inside board.
3. Ensure board/group context menus remain unaffected.

## Recent Changes
- External drag fallback by window-exit now supports both `Mode.DragItems` and `Mode.DragExternal`.
- For `Mode.DragExternal` fallback uses `TryStartExternalDragFromPreview()`.
- This restores drag start when pointer leaves board window before canvas receives an outside move event.
- Build passed: `dotnet build PrefabBoard/PrefabBoard.sln`.

## Next Steps
1. Unity smoke test for Ctrl+LMB card drag to Scene and Prefab Mode.
2. If still fails, add temporary debug logs around `StartExternalDrag` and payload presence.
3. Commit after confirmation.
