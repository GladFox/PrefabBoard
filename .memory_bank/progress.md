# Progress

## What Works
- UPM metadata for `0.2.1` is already in package and visible through package links.
- Package now includes local `CHANGELOG.md` and `LICENSE.md` with `.meta` files.
- Drag payload resolution now supports prefab assets and prefab instances as board input sources.

## Known Issues
- Unity-side visual smoke test still required after latest preview pipeline patch (cannot be executed from CLI here).
- Demo project files can become dirty during editor sessions and are intentionally excluded from targeted package commits.

## Solution Evolution
- Replaced additive temporary scene creation in preview renderer with preview scenes, removing dependency on current unsaved scene state.
- Hardened BuiltIn preview canvas behavior by forcing `ScreenSpaceCamera` for texture rendering pass.
- Expanded drag-to-board resolver to map hierarchy/prefab-stage objects back to source prefab asset GUID.

## Change Control
- last_checked_commit: 017ade2
- last_checked_date: 2026-03-06
