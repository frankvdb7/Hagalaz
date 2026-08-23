## 1. Logical and physical lifecycle

- [x] 1.1 Add opt-in reconnect options, lifecycle state, stable logical/physical identity, logical cancellation, and injectable grace timing to `RaidoConnectionContext`.
- [x] 1.2 Separate physical transport loss from terminal abort; retain the logical owner during the grace window and close it exactly once on expiry, explicit close, protocol failure, or shutdown.
- [x] 1.3 Add atomic store/context rebind with one winning replacement, physical handoff cleanup, generation advancement, and explicit detached-send failure.
- [x] 1.4 Expose a one-way reconnect-veto feature and close retained connections when the veto is applied during grace.

## 2. Handler and dispatch integration

- [x] 2.1 Recreate readers across physical generations and wait through reconnecting state without invoking terminal dispatcher/lifetime callbacks at initial transport loss.
- [x] 2.2 Guard dispatch and writes against stale generations while preserving existing caller context, client destination, protocol, features, and items.

## 3. Regression coverage

- [x] 3.1 Add tests for non-opt-in immediate terminal loss, opt-in retention, no early terminal callback, successful rebind, logical identity/context survival, and stale input/write rejection.
- [x] 3.2 Add tests for concurrent rebind winner, grace expiry, explicit close during grace, detached send failure, racing writes/rebind, and shutdown cleanup.
- [x] 3.3 Test reconnect veto before transport loss and during the grace window.

## 4. Validation

- [x] 4.1 Run the focused `Raido.Server.Tests` project with the repository's positional `dotnet test ... --no-restore` command and verify the existing suite remains green.
- [x] 4.2 Build the affected Raido projects, run strict OpenSpec validation for `raido-logical-reconnect`, inspect the cumulative diff, and confirm no GameWorld/session-resume scope leaked into the change.
