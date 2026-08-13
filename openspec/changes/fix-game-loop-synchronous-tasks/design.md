## Context

`RsTaskService.Tick()` walks scheduled `ITaskItem` instances synchronously. `RsTickTask` invokes its `Action` synchronously, but MiningTask, FishingTask, and WoodcuttingTask previously assigned `async void` methods to that action. The first incomplete await returned control to the scheduler while the callback continued later, so the next game tick could enter the same task before the previous reward operation had finished.

The skill startup flows also need asynchronous definition and store access. They should remain ordinary async methods, but their post-await gameplay code must return to the game-loop owner instead of running on an arbitrary completion thread. The scheduler therefore owns a `SynchronizationContext` with a thread-safe continuation queue.

## Goals / Non-Goals

**Goals:**

- Preserve the synchronous `ITaskItem.Tick()` and `RsTaskService.Tick()` contracts.
- Make Mining, Fishing, and Woodcutting recurring reward callbacks synchronous and non-overlapping.
- Keep ordinary async skill startup methods readable and free of preparation DTOs or result callbacks.
- Resume async gameplay continuations on the owning game loop.
- Load loot tables, definitions, and the existing asynchronous online-character count before the recurring task is queued.
- Preserve existing task cancellation, interruption, reward, exhaustion, and respawn ordering.

**Non-Goals:**

- Do not make the scheduler asynchronous.
- Do not replace the scheduler with a second worker, generic continuation scheduler, or state-machine framework.
- Do not change unrelated `RsAsyncTask` call sites or other gameplay skills.
- Do not change loot selection, reward amounts, chance calculations, or persistence behavior.
- Do not add a synchronous character-count API or duplicate count state to `CharacterStore`.

## Decisions

1. **Use a synchronous `Func<bool>` callback for the three recurring tasks.** The callback's boolean result already represents whether the task should stop. Making that callback synchronous removes the `async void` escape without changing the task's ownership or cancellation model. Inventory, XP, and world mutations remain inside this callback.

2. **Keep startup methods ordinary async methods.** Mining, Fishing, and Woodcutting use the direct shape `character.QueueTask(() => Start...Async(character, target))`. Each method awaits its required service calls and then invokes the normal synchronous gameplay setup. No preparation record, `Func<Task<Action?>>` helper, or two-lambda result API is introduced.

3. **Use a scheduler-owned `GameLoopSynchronizationContext`.** `RsTaskService.Tick()` installs one context for the duration of a tick and drains its concurrent continuation queue before ticking scheduled tasks. `RsAsyncTask` starts its regular `Task` once while that context is current. Normal `await` captures the context, so completion posts the continuation to the queue; the next game tick executes that continuation synchronously on the game-loop thread.

4. **Keep `RsAsyncTask` non-blocking.** Its first `Tick()` starts the operation and returns while it is incomplete. Later ticks only inspect `Task.IsCompleted`; `GetAwaiter().GetResult()` is used only after completion is known, so the scheduler never waits for I/O. A fault is owned by the task and reported through the existing scheduler logging path.

5. **Use ordinary cooperative task cancellation.** `GameLoopSynchronizationContext` knows nothing about cancellation and always runs queued continuations. `RsAsyncTask.Cancel()` requests cancellation through its internal `CancellationTokenSource`; the token-aware constructor passes that token to the operation. `IsCancelled` reflects the underlying task becoming canceled rather than merely receiving a cancellation request. Operations created through the parameterless-token constructor continue normally unless they cancel themselves.

6. **Use the existing asynchronous character count API during setup.** Mining, Fishing, and Woodcutting resolve `ICharacterStore.CountAsync()` before their synchronous gameplay setup, then capture that setup-time value for respawn calculation. No synchronous count property or second count state is added to `CharacterStore`.

## Risks / Trade-offs

- [Continuation queue] Async completion can happen on a worker thread → `GameLoopSynchronizationContext.Post` uses a concurrent queue, and only `RsTaskService.Tick()` executes queued continuations.
- [Setup latency] Definition and count reads now complete before the recurring task starts → no recurring gameplay state is started until required data is available, and the scheduler tick is not blocked.
- [Cancellation race] Cancellation after a continuation starts cannot undo already-running synchronous gameplay → the game loop remains the single owner of continuation execution, while normal .NET `finally` blocks and cooperative cancellation semantics remain intact.
- [Count freshness] `CountAsync()` is intentionally a setup-time snapshot until a synchronous store API is needed → this preserves the current store contract and avoids duplicate count state.

## Migration Plan

No deployment or data migration is required. Deploy the code change normally. Rollback is a code rollback; no persisted state is changed by this design.

## Open Questions

None.
