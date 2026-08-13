## 1. Synchronous recurring task implementation

- [x] 1.1 Change MiningTask, FishingTask, and WoodcuttingTask reward callbacks to synchronous `Func<bool>` callbacks with no `async void` tick methods.
- [x] 1.2 Preload Mining, Fishing, and Woodcutting loot/definition inputs in their existing asynchronous setup flows and keep recurring callbacks limited to in-memory game-loop mutations.

## 2. Regression coverage

- [x] 2.1 Update existing Woodcutting service coverage for the synchronous callback contract and setup-time data loading.
- [x] 2.2 Add deterministic recurring-task tests covering synchronous callback completion and non-overlapping consecutive ticks for Mining, Fishing, and Woodcutting.
- [x] 2.3 Preserve scheduler lifecycle coverage for completed-task removal and delayed respawn ordering, then run focused tests and a project build.

## 3. Async startup handoff and setup-time population count

- [x] 3.1 Implement non-blocking `RsAsyncTask` and the scheduler-owned `GameLoopSynchronizationContext`; drain posted continuations on the game-loop tick before scheduled tasks run.
- [x] 3.2 Keep online-character counting on the existing asynchronous `ICharacterStore.CountAsync()` API; do not add a synchronous count property or duplicate counter state.
- [x] 3.3 Refactor Mining, Fishing, and Woodcutting startup I/O to ordinary `QueueTask(() => Start...Async(...))` methods; remove preparation DTOs, result-task call sites, and the `QueueAsyncTask(Func<Task<Action?>>)` continuation helper.
- [x] 3.4 Resolve the online-character count during asynchronous setup and pass it into the synchronous recurring callbacks.

## 4. Follow-up regression coverage

- [x] 4.1 Test non-blocking async task progress, game-loop continuation resumption, cooperative and non-cooperative cancellation, fault ownership, and existing scheduler task ordering.
- [x] 4.2 Preserve `CharacterStore.CountAsync()` coverage for the authoritative collection count.
- [x] 4.3 Test the async-to-synchronous handoff and verify Woodcutting reads `CountAsync()` during setup but not from the recurring callback.
- [x] 4.4 Run focused tests, strict OpenSpec validation, solution build, and final diff review.

## 5. Scheduler tick-boundary semantics

- [x] 5.1 Drain tasks scheduled before the tick before resuming continuations, and defer tasks scheduled during continuation or ordinary task processing until the next tick.
- [x] 5.2 Process a snapshot continuation batch so continuations posted during `RunPending()` wait for the next batch.
- [x] 5.3 Add deterministic ordering and continuation-batch regression tests, then rerun focused tests, strict OpenSpec validation, solution build, and final diff review.

## 6. Queue handoff and stale interaction hardening

- [x] 6.1 Protect continuation enqueue and batch handoff with a short lock so concurrent posts cannot be lost.
- [x] 6.2 Add temporary interruption guards to Mining, Fishing, and Woodcutting asynchronous startup flows.
- [x] 6.3 Add concurrent continuation and interrupted-startup regression coverage, then rerun focused tests, strict OpenSpec validation, solution build, and final diff review.

## 7. Generic externally owned cancellation

- [x] 7.1 Add optional externally owned cancellation tokens to `RsAsyncTask` and both asynchronous `QueueTask` overloads without adopting them in Mining, Fishing, or Woodcutting.
- [x] 7.2 Keep skill startup methods tokenless with one temporary interruption check at the synchronous gameplay boundary, and cover the generic external-cancellation behavior with focused tests.
