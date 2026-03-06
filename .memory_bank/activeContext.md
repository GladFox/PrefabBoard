# Active Context

## Current Tasks
1. Finalize UPM metadata so Unity Package Manager shows package version `0.2.1`.
2. Add package-local `CHANGELOG.md` and `LICENSE.md` and wire UPM links in `package.json`.
3. Keep Demo project local changes out of package-only release commit.

## Recent Changes
- Updated `Packages/com.gladfox.prefabboard/package.json`:
  - bumped package version to `0.2.1`
  - added `documentationUrl`, `changelogUrl`, `licensesUrl`
- Added package files:
  - `Packages/com.gladfox.prefabboard/CHANGELOG.md`
  - `Packages/com.gladfox.prefabboard/LICENSE.md`
- Added Unity meta files for new package docs:
  - `CHANGELOG.md.meta`
  - `LICENSE.md.meta`

## Next Steps
1. Commit only package metadata/doc updates (exclude Demo local edits).
2. Push release patch to `main`.
3. In Unity, reopen Package Manager and validate version + changelog/license links in package details.
