# Progress

## What Works
- UPM package import path and metadata are in place.
- BuiltIn preview mode now has dedicated configurable settings:
  - base resolution
  - camera background color
- BuiltIn preview rendering pipeline reads these settings during rig/camera setup.

## Known Issues
- Unity-side verification is still required for final visual parity and UX confirmation.
- Local Demo project files (`manifest/project settings`) may change during editor operations and are not automatically included in targeted fix commits.

## Solution Evolution
- Extended settings model with BuiltIn-only preview controls.
- Wired BuiltIn settings into resolution and camera background resolution logic in preview renderer.

## Change Control
- last_checked_commit: 0abebcb
- last_checked_date: 2026-03-05
