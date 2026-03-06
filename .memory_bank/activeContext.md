# Active Context

## Current Tasks
1. Rework preview pipeline: `Preview Scene API` cannot be used as the main path for uGUI previews (does not reliably render required UI content).
2. Restore reliable uGUI rendering in BuiltIn preview mode using non-PreviewScene flow.
3. Keep support for Project/Prefab Mode/Hierarchy drag sources when dropping prefabs onto the board.
4. Keep Demo-local scene/settings changes out of package bugfix commit.

## Recent Changes
- `PreviewCache` preview render path was rolled back from `PreviewScene API` to additive temporary scene flow (`NewScene(...Additive)` + `CloseScene`).
- BuiltIn rig profile application now keeps preview canvas in `RenderMode.ScreenSpaceCamera` to ensure camera-based UI rendering to RenderTexture.
- Added robust prefab source resolution in `AssetGuidUtils`:
  - `TryResolvePrefabAsset(Object, out GameObject)`
  - `TryResolvePrefabGuid(Object, out string)`
- `BoardCanvasElement` drag-in path now accepts prefab instances (from Prefab Mode/Hierarchy), not only prefab assets from Project.
- Confirmed limitation: for our target prefabs, `Preview Scene API` path is not suitable for stable uGUI rendering and must be treated as a dead-end experiment.

## Next Steps
1. Verify in Unity:
   - no unsaved-scene exception during preview refresh,
   - Dialog/Button previews render in BuiltIn mode with additive-scene path,
   - drag from opened prefab hierarchy to board creates cards.
2. Commit package-only bugfix changes and push to `main` (if validation passes).
