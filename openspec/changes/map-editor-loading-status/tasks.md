# Tasks

## 1. Status presentation

- [x] 1.1 Add a map load status component deriving first-load, progress, failed, empty and issue states, verifying by component tests each state, forward-only progress, restart on a new load, dismissal and stable announcements.
- [x] 1.2 Project it into the viewport and remove the page-level bar and counter text, verifying by page tests the stage shown during an incremental load.

## 2. Writes and retry

- [x] 2.1 Show pending saves and world map refreshes, verifying by a page test the text during a save.
- [x] 2.2 Add retry through the pending-changes guard, verifying by page tests that retry reloads and that declining the discard prompt keeps the draft.

## 3. Verification

- [x] 3.1 Run admin tests, `lint:biome`, the admin build and strict OpenSpec validation.
- [ ] 3.2 Check first load, incremental load and failure states in the browser at desktop and phone widths.
