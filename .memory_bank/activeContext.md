# Active Context

## Current Tasks
1. Fix ScreenSpace UI preview failure when an untitled unsaved scene is open.
2. Restore reliable uGUI rendering in BuiltIn preview mode.
3. Support Project/Prefab Mode/Hierarchy drag sources when dropping prefabs onto the board.
4. Keep Demo-local scene/settings changes out of package bugfix commit.

## Recent Changes
- `PreviewCache` preview scene lifecycle switched to `EditorSceneManager.NewPreviewScene()`/`ClosePreviewScene()` for both ScreenSpace and WorldSpace custom preview passes.
- BuiltIn rig profile application now keeps preview canvas in `RenderMode.ScreenSpaceCamera` to ensure camera-based UI rendering to RenderTexture.
- Added robust prefab source resolution in `AssetGuidUtils`:
  - `TryResolvePrefabAsset(Object, out GameObject)`
  - `TryResolvePrefabGuid(Object, out string)`
- `BoardCanvasElement` drag-in path now accepts prefab instances (from Prefab Mode/Hierarchy), not only prefab assets from Project.

## Next Steps
1. Verify in Unity:
   - no ScreenSpace preview exception with unsaved scene,
   - Dialog/Button previews render in BuiltIn mode,
   - drag from opened prefab hierarchy to board creates cards.
2. Commit package-only bugfix changes.
3. Push to `main` and prepare release note entry if requested.
