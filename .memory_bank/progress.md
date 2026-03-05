# Progress

## What Works
- PrefabBoard is packaged as UPM module `com.gladfox.prefabboard`.
- Git URL install flow is documented and wired in Demo.
- Package now includes required `.meta` files for immutable package assets.
- BuiltIn preview mode now applies camera/canvas/scaler profile from rig prefab settings.

## Known Issues
- Manual Unity validation is still required after package + BuiltIn changes.
- Local Demo project may contain unrelated user-side settings changes not part of this fix.

## Solution Evolution
- Added missing package root metadata to remove immutable-folder import warnings.
- Extended BuiltIn preview setup with rig-profile override for parity with `Rig.prefab` behavior.

## Change Control
- last_checked_commit: 1887d56
- last_checked_date: 2026-03-05
