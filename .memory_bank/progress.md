# Progress

## What Works
- Core board editor is stable.
- `Ctrl+LMB` external drag path has visual ghost and move handling.
- External drag fallback now covers both drag modes (`DragItems`, `DragExternal`).

## Known Issues
- Requires Unity manual verification for full drag/drop UX.
- No automated interaction tests.

## Solution Evolution
- Restored window-exit fallback for `Mode.DragExternal` to prevent missed drag starts when cursor leaves board window.

## Change Control
- last_checked_commit: c25b38b
- last_checked_date: 2026-03-02
