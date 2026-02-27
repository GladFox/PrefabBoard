# Release Notes

## v0.1.0 - February 27, 2026

### Highlights
- Switched multi-board storage to `board-per-file`:
  - each board is saved as a separate asset in `Assets/Editor/PrefabBoards/Boards`
  - board discovery now uses `AssetDatabase.FindAssets`
  - last opened board is stored in `EditorPrefs` (`PrefabBoard.LastOpenedBoardId`)
- Added robust group interaction model:
  - group dragging and resizing from canvas-level hit testing
  - 8 resize handles (edges + corners)
- Improved undo/redo behavior:
  - `Ctrl/Cmd + Z`, `Ctrl/Cmd + Y`, `Ctrl/Cmd + Shift + Z`
  - camera-only actions are excluded from undo stack
- Added right-side outline panel:
  - separate sections: `Anchors` and `Elements`
  - click-to-focus item and click-to-frame group
- Extended board navigation:
  - larger zoom-out range for big boards
- Improved drag-out to Scene/Hierarchy:
  - better reliability when cursor leaves board window
  - supports repeated prefab entries in payload
  - compatible with both Legacy Input and Input System

### Notes
- Current group behavior is anchor-oriented (no automatic item attachment to groups).
- `BoardLibraryAsset` remains in project as a legacy artifact, but is no longer used as board source of truth.
