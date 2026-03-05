# Progress

## What Works
- PrefabBoard implementation is now a standalone UPM package (`com.gladfox.prefabboard`).
- Unity project tracked structure is converted to `Demo/`.
- User-facing install flow now uses Git URL package dependency.
- Demo project dependency also uses the same Git URL package reference.

## Known Issues
- Manual Unity validation is required after migration (package resolution + runtime editor interactions).
- The old local folder `PrefabBoard/` may still exist in workspace with non-tracked generated files (`Library`, `Logs`, `obj`, `.sln`) and is no longer the tracked project root.

## Solution Evolution
- Replaced monolithic project-embedded tool layout with package + demo split.
- Added package-level metadata (`package.json`, package README).
- Updated docs + demo manifest from local file package to Git URL package install.

## Change Control
- last_checked_commit: e264865
- last_checked_date: 2026-03-05
