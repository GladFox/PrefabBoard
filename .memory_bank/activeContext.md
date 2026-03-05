# Active Context

## Current Tasks
1. Verify BuiltIn preview mode settings (`builtInBaseResolution`, `builtInCameraBackground`) in Unity.
2. Verify package import warnings are gone for immutable package folder.
3. Keep demo package/project settings changes out of targeted package fix commit unless explicitly required.

## Recent Changes
- Added BuiltIn-specific preview settings fields to `PreviewRigSettingsAsset`.
- Updated `PreviewCache` so BuiltIn mode uses configured base resolution and camera background.
- Added package metadata coverage for immutable package assets in previous patch set.
- Updated release metadata (`VERSION`, `RELEASE_NOTES`) for next patch release.

## Next Steps
1. Smoke test in Unity for BuiltIn preview output parity.
2. Commit targeted files only (package code + docs + version notes).
3. Push release patch.
