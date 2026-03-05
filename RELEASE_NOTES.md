# Release Notes

## v0.2.0 - March 5, 2026

### Highlights
- Migrated PrefabBoard implementation into a UPM package:
  - `Packages/com.gladfox.prefabboard`
  - editor code moved under package `Editor/` (`Data`, `Services`, `UI`, `Styles`)
- Converted tracked Unity project layout to Demo project:
  - project root moved from `PrefabBoard/` to `Demo/`
  - demo now references package via local file dependency:
    - `com.gladfox.prefabboard`: `file:../../Packages/com.gladfox.prefabboard`
- Updated stylesheet loading in `PrefabBoardWindow`:
  - primary path from package
  - legacy `Assets/...` fallback retained

### Notes
- Demo-specific assets remain in `Demo/Assets` (boards, prefabs, preview rig settings).
- Tool behavior remains editor-only and functionally aligned with previous MVP scope.

## v0.1.1 - March 5, 2026

### Highlights
- Improved external prefab drag pipeline from board cards:
  - stabilized `Ctrl/Cmd + LMB` drag-out flow
  - better handoff to Scene/Hierarchy/Prefab Mode targets
  - refined drag ghost/preview behavior during external drag candidate mode
- Prefab stage drop handling updates:
  - safer payload handling for board-origin drags
  - improved compatibility for dropping into open prefab editing context
- Canvas interaction cleanup:
  - adjusted right-click/context-menu behavior around drag gestures
  - reduced accidental menu conflicts during drag scenarios
- UI and style cleanup:
  - updated board/card/group interaction code paths
  - style cleanups in `PrefabBoard.uss`

### Notes
- Current recommended external drag gesture remains `Ctrl/Cmd + LMB` for maximum stability in Unity Editor.

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
