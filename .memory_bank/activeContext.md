# Active Context

## Current Tasks
1. Validate package import in `Demo` project (`com.gladfox.prefabboard` from local file path).
2. Validate PrefabBoard window opens with stylesheet from package path.
3. Run Unity smoke-test for board CRUD and drag/drop after package migration.

## Plan (REQUIREMENTS_OWNER)
1. Move editor implementation to UPM package structure.
2. Convert tracked Unity project to Demo layout and connect package dependency.
3. Sync docs/memory and verify build-level integrity.

## Strategy (ARCHITECT)
- Keep demo data/assets in `Demo/Assets`.
- Keep tool implementation in `Packages/com.gladfox.prefabboard`.
- Preserve script GUID continuity by moving `.cs` with corresponding `.meta` files.

## Recent Changes
- Project tracked directories moved under `Demo/` (`Assets`, `Packages`, `ProjectSettings`).
- PrefabBoard editor implementation moved from `Assets/Editor/PrefabBoard/*` to package `Packages/com.gladfox.prefabboard/Editor/*`.
- Added package manifest `Packages/com.gladfox.prefabboard/package.json`.
- Added package dependency in `Demo/Packages/manifest.json`.
- Updated `PrefabBoardWindow` stylesheet loading to package path with legacy fallback.
- Updated version/docs for package+demo architecture (`v0.2.0`).

## REVIEWER Checklist
- Package path and dependency are valid.
- Demo still contains required board/settings assets.
- No stale hardcoded paths to old `Assets/Editor/PrefabBoard/Styles` only.
- Docs and Memory Bank reflect new layout.

## Next Steps
1. Open Demo in Unity and let Package Manager resolve local package.
2. Run interaction smoke-test.
3. Commit and push.
