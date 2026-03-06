# Progress

## What Works
- UPM metadata for `0.2.1` is already in package and visible through package links.
- Package now includes local `CHANGELOG.md` and `LICENSE.md` with `.meta` files.
- Drag payload resolution now supports prefab assets and prefab instances as board input sources.

## Known Issues
- `Preview Scene API` is not reliable for PrefabBoard uGUI preview rendering (confirmed on target prefabs like Dialog/Button); keep this path disabled/temporary and avoid treating it as production-ready.
- Unity-side visual smoke test still required after latest preview pipeline patch (cannot be executed from CLI here).
- Demo project files can become dirty during editor sessions and are intentionally excluded from targeted package commits.

## Solution Evolution
- Tried preview-scene-based renderer, but it proved unsuitable for stable uGUI preview output in target prefabs.
- Rolled renderer back to additive temporary scene flow for ScreenSpace/WorldSpace preview passes.
- Hardened BuiltIn preview canvas behavior by forcing `ScreenSpaceCamera` for texture rendering pass.
- Expanded drag-to-board resolver to map hierarchy/prefab-stage objects back to source prefab asset GUID.
- New constraint captured: PreviewScene-based render path did not solve uGUI preview correctness, so renderer strategy must revert to non-PreviewScene approach for stable results.

## Change Control
- last_checked_commit: b4cc7f5
- last_checked_date: 2026-03-06
