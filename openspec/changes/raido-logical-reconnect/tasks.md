## 1. Logical and physical lifecycle

- [x] 1.1 Add endpoint support plus explicit per-logical-connection activation, lifecycle state, stable logical/physical identity, logical cancellation, and injectable grace timing to `RaidoConnectionContext`.
- [x] 1.2 Separate physical transport loss from terminal abort; retain the logical owner during the grace window and close it exactly once on expiry, explicit close, protocol failure, or shutdown.
- [x] 1.3 Add explicit prepare/commit reservation and invalidation with one winning replacement, stable application pipes, physical pump replacement, deferred committed work, and explicit detached-send failure.
- [x] 1.4 Expose explicit enable/veto and post-rebind notification features; close retained connections when the veto is applied during grace.
- [x] 1.5 Make prepare invalidation terminate the temporary replacement and make the committed response/post-commit output barrier explicit.
- [x] 1.6 Discard uncertain pre-loss output, consume large post-commit output live, and install the barrier atomically with normal write admission.
- [x] 1.7 Quiesce a pending retained-application output flush before asynchronously acquiring the target write lock, and keep that intentional cancellation recoverable.

## 2. Handler and dispatch integration

- [x] 2.1 Keep the original logical handler on stable application pipes, complete only its owned protocol reader, and transfer unread input only after the replacement reader has released ownership.
- [x] 2.2 Keep physical pump ownership single-reader/single-writer across replacement with generation-scoped stop completion, exact suffix-before-new-input ordering, and preserved caller context, client destination, protocol, features, and items.
- [x] 2.3 Keep the Kestrel physical handler lifetime separate from the temporary replacement application handler, and keep physical plumbing internal to Raido.

## 3. Regression coverage

- [x] 3.1 Add tests for non-opt-in immediate terminal loss, opt-in retention, no early terminal callback, successful rebind, logical identity/context survival, and old-session input/write fencing.
- [x] 3.2 Add tests for concurrent rebind winner, grace expiry, explicit close during grace, detached send failure, racing writes/rebind, and shutdown cleanup.
- [x] 3.3 Test reconnect veto before transport loss and during the grace window.
- [x] 3.4 Test explicit endpoint support without activation, composed persistent post-rebind callback delivery, reservation invalidation, and reader completion ownership.
- [x] 3.5 Test real application/physical pipes, same-buffer opcode-plus-payload transfer with exact suffix ordering, and original physical handler lifetime after rebind.
- [x] 3.6 Test deterministic first-packet response ordering, post-commit output ordering, and invalidated replacement termination.
- [x] 3.7 Test stale-output discard, large post-commit backpressure, normal-write admission, and keep-alive suppression during the barrier.
- [x] 3.8 Test a blocked pre-loss application flush, replacement commit completion, intentional cancellation safety, stale-byte discard, and normal post-reconnect output.

## 4. Validation

- [x] 4.1 Run the focused `Raido.Server.Tests` project with the repository's positional `dotnet test ... --no-restore` command and verify the existing suite remains green.
- [x] 4.2 Build the affected Raido projects, run strict OpenSpec validation for `raido-logical-reconnect`, inspect the cumulative diff, and confirm no unrelated transport or replay scope leaked into the change.
