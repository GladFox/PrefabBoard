# Active Context

## Current Tasks
1. Publish release metadata for `v0.2.2` (root `VERSION`, package version, release notes).
2. Keep non-PreviewScene rendering path for uGUI previews and preserve restored BuiltIn sizing behavior.
3. Keep support for Project/Prefab Mode/Hierarchy drag sources when dropping prefabs onto the board.
4. Track Demo-local scene/settings changes separately from package-only release commits.

## Recent Changes
- Prepared release bump to `0.2.2` in root and package metadata.
- Added `v0.2.2` entries to root `RELEASE_NOTES.md` and package `CHANGELOG.md`.
- `PreviewCache` preview render path was rolled back from `PreviewScene API` to additive temporary scene flow (`NewScene(...Additive)` + `CloseScene`).
- Root-cause analysis against pre-UPM code: UPM migration itself was not the functional regression source for sizing.
- Found regression source: post-migration `ApplyBuiltInTemplateRigProfile` override (camera/canvas/scaler transforms copied from `Rig.prefab`) broke size behavior.
- Removed `ApplyBuiltInTemplateRigProfile` usage so BuiltIn path returns to pre-UPM sizing logic while keeping BuiltIn settings (`builtInBaseResolution`, `builtInCameraBackground`).
- Added robust prefab source resolution in `AssetGuidUtils`:
  - `TryResolvePrefabAsset(Object, out GameObject)`
  - `TryResolvePrefabGuid(Object, out string)`
- `BoardCanvasElement` drag-in path now accepts prefab instances (from Prefab Mode/Hierarchy), not only prefab assets from Project.
- Confirmed limitation: for our target prefabs, `Preview Scene API` path is not suitable for stable uGUI rendering and must be treated as a dead-end experiment.

## Next Steps
1. Verify in Unity:
   - no unsaved-scene exception during preview refresh,
   - Dialog/Button previews render in BuiltIn mode with correct element-to-canvas size mapping,
   - drag from opened prefab hierarchy to board creates cards.
2. Push release metadata commit and tag/release if requested.
