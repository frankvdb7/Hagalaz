## 1. Regression tests first

- [x] 1.1 Add real-pipe context tests for disabled immediate termination, physical detach waking reads without cancelling the terminal token, detached writes, state preservation, endpoint rebinding, and fresh replacement readers; verify the focused Raido test project initially exposes the intended failures.
- [x] 1.2 Add reconnect race tests for concurrent candidates, captured waiter identity, already-closed candidates, timeout versus callback registration, losing-candidate registration cleanup and caller ownership, stale close/heartbeat/read/write failures, and physical detach cancellation ordering; verify each test has a deterministic completion or bounded timeout.
- [x] 1.3 Add handler/lifetime tests for callback movement, W1 timeout terminality, W2 creation only after a successful rebind and later disconnect, late W1 candidate rejection, stable store/lifetime-manager membership, and exactly one terminal disconnect; verify existing non-reconnect handler behavior remains covered.

## 2. Physical transport ownership

- [x] 2.1 Replace permanent transport access in `RaidoConnectionContext` with one published current physical connection while keeping `_connection` authoritative for stable state; verify context identity, ID, features, items, caller state, protocol, and endpoints across rebind tests.
- [x] 2.2 Implement one-lock reconnect-window waiter lifecycle, physical detach, timeout closure, and terminal abort; verify per-disconnect waiter tests and terminal-token assertions.
- [x] 2.3 Implement captured-physical registration, operation, and failure handling, including close-request features, heartbeat callbacks, stale-failure filtering, pending-read/flush wake-up, and registration disposal outside the lock; verify race tests and callback-movement tests.
- [x] 2.4 Implement pre-publication candidate registration in `TryReconnect`, captured waiter validation, atomic current-transport publication, candidate close checks, success ownership, and false-return caller ownership; verify concurrent-winner and timeout-race tests.

## 3. Handler and configuration integration

- [x] 3.1 Update the handler to obtain a published transport before creating a reader, dispatch with a fresh reader per transport, await the reconnect window outside the lock, and disconnect the stable lifetime once; verify handler reconnect-cycle tests.
- [x] 3.2 Add `WithStatefulReconnect` and bounded `StatefulReconnectTimeout` options/defaults; keep the capability opt-in with no production caller enabled yet and verify GameUpdate remains unchanged.

## 4. Validation

- [x] 4.1 Run the focused Raido tests and GameWorld tests serially, then build the affected projects and solution as appropriate; verify all required suites pass or distinguish environment failures.
- [x] 4.2 Validate `add-raido-stateful-reconnect` with strict OpenSpec validation and review the cumulative diff for fake pipes, parallel state, stale callbacks, lock-held awaits/disposals, unintended GameUpdate/store/lifetime changes, and uncompleted task mappings.

## 5. Invariant remediation

- [x] 5.1 Centralize terminal transitions, make current `ConnectionClosedRequested` terminal, anchor the single reconnect deadline at physical detach, and clear transient failure state after successful publication.
- [x] 5.2 Make the handler wait when it observes a detached active reconnect window, remove the premature GameWorld opt-in, and update the behavior artifacts.
- [x] 5.3 Add regressions for detached-start dispatch, terminal close requests, and detach-anchored timeout; fix CodeQL findings introduced by this PR, but do not let them drive reconnect architecture changes; run focused tests and validation.

## 6. Correctness remediation

- [x] 6.1 Restrict handler read reconnectability to deliberately recognized physical cancellation/I/O failures; let protocol, parser, application, and other unrecognized exceptions follow the terminal path.
- [x] 6.2 Reset per-read client-timeout state during identity-safe physical detach without introducing reconnect-lock-to-timeout-lock nesting; retain replacement heartbeat registration.
- [x] 6.3 Simplify the handler to one detached-window lifecycle path and add deterministic parser, transport-exception, timeout-reset, and replacement-heartbeat regressions.
- [x] 6.4 Update OpenSpec artifacts and PR metadata, run focused/full tests, builds, strict validation, and the cumulative diff review.

## 7. Output and timeout remediation

- [x] 7.1 Separate protocol serialization, output metadata, and captured physical flush handling in `WriteCore`, `CompleteWriteAsync`, and `WriteSlowAsync`; preserve caller-cancellation semantics, retain stale identity checks, and add terminal protocol plus physical output regressions.
- [x] 7.2 Separate keep-alive generation from captured physical ping writing in `TryWritePingSlowAsyncForConnection`; verify generation failures are terminal and current/stale physical failures have the approved behavior.
- [x] 7.3 Release `_receiveMessageTimeoutLock` before `TryAbortForConnection` in `CheckClientTimeoutForConnection`; add deterministic timeout/`ConnectionClosed` contention coverage and retain timeout-state reset behavior.
- [x] 7.4 Register initial callbacks into locals and publish only after identity/non-terminal validation; cover pre-signalled `ConnectionClosed` and `ConnectionClosedRequested` constructor behavior and preserve explicit registration ownership.
- [x] 7.5 Verify real `RaidoConnectionStore` membership through detach, replacement, and terminal disconnect without changing store or lifetime ownership.
- [x] 7.6 Update the design/spec wording, keep CodeQL cleanup scoped to introduced registration/resource findings, and run the complete serial validation and cumulative diff review.

## 8. Detached close-request publication remediation

- [x] 8.1 Preserve the detached physical connection identity and close-request registration for the active reconnect window; make the final waiter/window/token validation and replacement publication atomic under the existing reconnect lock.
- [x] 8.2 Add deterministic coverage for detached close-request dominance, stale post-publication requests, pre-claim token races, and replace flaky terminal wait-handle assertions with the existing terminal completion path.
- [x] 8.3 Update OpenSpec and remove tracked `.testagent` artifacts while adding the ignored-folder rule; run the complete serial validation and cumulative diff review.

## 9. Initial detached registration ownership remediation

- [x] 9.1 Preserve the initial physical connection's `ConnectionClosedRequested` registration when synchronous `ConnectionClosed` handling creates an active detached W1 window; reset transferred local registration markers and keep all disposal outside `_reconnectLock`.
- [x] 9.2 Add regressions for the pre-signalled physical-close/close-request sequence and both pre-signalled tokens, update the behavior documentation, and validate the cumulative change without adding reconnect architecture.
