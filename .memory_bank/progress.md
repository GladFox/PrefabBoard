# Progress

## What Works
- UPM package metadata now targets Unity display requirements for release `0.2.1`.
- Package root now includes `CHANGELOG.md` and `LICENSE.md` files.
- Package metadata includes explicit UPM links:
  - `documentationUrl`
  - `changelogUrl`
  - `licensesUrl`
- Unity `.meta` files exist for newly added package docs to avoid immutable-folder import warnings.

## Known Issues
- Demo project local files (`manifest`, `packages-lock`, `ProjectSettings`) may still be dirty from editor sessions and are intentionally excluded from package-only release commits.

## Solution Evolution
- Synced package version from stale `0.2.0` to `0.2.1`.
- Added package-scoped changelog/license artifacts for UPM consumption.
- Wired GitHub URLs in `package.json` to ensure Package Manager links resolve from git dependency installs.

## Change Control
- last_checked_commit: 37bb3fd
- last_checked_date: 2026-03-06
