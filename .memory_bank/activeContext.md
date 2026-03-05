# Active Context

## Current Tasks
1. Validate package import in `Demo` project (`com.gladfox.prefabboard` from Git URL).
2. Validate BuiltIn preview mode behavior against `Demo/Assets/Editor/PrefabBoard/Rig.prefab` profile.
3. Run Unity smoke-test for board CRUD and drag/drop after package migration.

## Recent Changes
- Added missing package meta files for immutable package assets:
  - `Packages/com.gladfox.prefabboard.meta`
  - `Packages/com.gladfox.prefabboard/Editor.meta`
  - `Packages/com.gladfox.prefabboard/package.json.meta`
  - `Packages/com.gladfox.prefabboard/README.md.meta`
- Updated BuiltIn preview rig pipeline in `PreviewCache`:
  - BuiltIn mode now applies camera/canvas/canvas-scaler profile from `settings.rigPrefab` (same source as `Rig.prefab`).
  - Keeps current BuiltIn flow, but mirrors template rig setup parameters for parity.

## Plan (REQUIREMENTS_OWNER)
1. Ensure package assets are recognized by Unity in immutable package context.
2. Align BuiltIn preview behavior with rig profile used by template mode.
3. Commit only targeted package fixes without touching unrelated local changes.

## Strategy (ARCHITECT)
- Keep package metadata complete for all package root objects.
- Apply rig profile in BuiltIn mode as post-configuration override to preserve existing pipeline.
- Avoid unrelated Demo project settings changes in this commit.

## REVIEWER Checklist
- No missing `.meta` files under package root.
- No immutable-folder warnings for package root/editor/package.json/readme.
- BuiltIn mode reads rig profile from `PreviewRigSettingsAsset.rigPrefab`.
- No regressions in template rig mode path.

## Next Steps
1. Verify in Unity that immutable-folder warnings are gone.
2. Switch PreviewRigSettings to BuiltIn and compare output with template rig behavior.
3. Commit and push targeted fixes.
