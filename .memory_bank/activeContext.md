# Active Context

## Current Tasks
1. Validate package import in `Demo` project (`com.gladfox.prefabboard` from Git URL).
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
- Switched user-facing install docs to Git UPM URL:
  - `https://github.com/GladFox/PrefabBoard.git?path=/Packages/com.gladfox.prefabboard#main`
- Updated Demo dependency to the same Git URL in `Demo/Packages/manifest.json`.

## REVIEWER Checklist
- Git URL package dependency is valid in docs and Demo manifest.
- Demo still contains required board/settings assets.
- No stale hardcoded paths to old `Assets/Editor/PrefabBoard/Styles` only.
- Docs and Memory Bank reflect new layout and installation flow.

## Next Steps
1. Open Demo in Unity and let Package Manager resolve Git package.
2. Run interaction smoke-test.
3. Commit and push.
