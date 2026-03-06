# Changelog

All notable changes to this package are documented in this file.

## [0.2.2] - 2026-03-06

### Changed
- Rolled UI preview renderer back from `PreviewScene` to additive temporary scene flow.
- Restored pre-UPM BuiltIn preview sizing behavior by removing template profile geometry overrides.
- Added drag-to-board support for prefab instances from Prefab Mode and Hierarchy.

## [0.2.1] - 2026-03-06

### Added
- BuiltIn preview rig settings: base resolution and camera background.
- Missing Unity `.meta` files for package root assets.

### Changed
- BuiltIn preview renderer now respects rig profile parity settings from `Rig.prefab` and package preview settings.
- Package metadata updated for Unity Package Manager links (`documentationUrl`, `changelogUrl`, `licensesUrl`).

## [0.2.0] - 2026-03-05

### Added
- Initial UPM package structure for `com.gladfox.prefabboard`.
- Demo project references package as a local dependency.

## [0.1.1] - 2026-03-05

### Changed
- Stabilized external drag-out flow from board cards to Scene/Hierarchy/Prefab Mode.
- Improved drag/context-menu interaction behavior on canvas.

## [0.1.0] - 2026-02-27

### Added
- Board-per-file storage model.
- Group anchors with drag/resize and right-side navigation panel.
- Undo/redo improvements and expanded board navigation behavior.
