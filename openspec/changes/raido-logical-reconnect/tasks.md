## 1. Logical and physical lifecycle

- [x] 1.1 Add endpoint support plus explicit per-logical-connection activation, lifecycle state, stable logical/physical identity, logical cancellation, and injectable grace timing to `RaidoConnectionContext`.
- [x] 1.2 Separate physical transport loss from terminal abort; retain the logical owner during the grace window and close it exactly once on expiry, explicit close, protocol failure, or shutdown.
- [x] 1.3 Add atomic store/context reservation and commit with one winning replacement, stable application pipes, physical pump replacement, and explicit detached-send failure.
- [x] 1.4 Expose explicit enable/veto and post-rebind notification features; close retained connections when the veto is applied during grace.

## 2. Handler and dispatch integration

- [x] 2.1 Keep the original logical handler on stable application pipes, complete its protocol reader explicitly, and transfer unread input only after the replacement reader has released ownership.
- [x] 2.2 Keep physical pump ownership single-reader/single-writer across replacement while preserving caller context, client destination, protocol, features, and items.

## 3. Regression coverage

- [x] 3.1 Add tests for non-opt-in immediate terminal loss, opt-in retention, no early terminal callback, successful rebind, logical identity/context survival, and old-session input/write fencing.
- [x] 3.2 Add tests for concurrent rebind winner, grace expiry, explicit close during grace, detached send failure, racing writes/rebind, and shutdown cleanup.
- [x] 3.3 Test reconnect veto before transport loss and during the grace window.
- [x] 3.4 Test explicit endpoint support without activation, persistent post-rebind callback delivery, and reader completion ownership.
- [x] 3.5 Test real application/physical pipes, same-buffer opcode-plus-payload transfer, and original physical handler lifetime after rebind.

## 4. Validation

- [x] 4.1 Run the focused `Raido.Server.Tests` project with the repository's positional `dotnet test ... --no-restore` command and verify the existing suite remains green.
- [x] 4.2 Build the affected Raido projects, run strict OpenSpec validation for `raido-logical-reconnect`, inspect the cumulative diff, and confirm no GameWorld/session-resume scope leaked into the change.
