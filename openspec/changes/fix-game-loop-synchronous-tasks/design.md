## Context

`RsTaskService.Tick()` walks scheduled `ITaskItem` instances synchronously. `RsTickTask` invokes its `Action` synchronously, but MiningTask, FishingTask, and WoodcuttingTask currently assign `async void` methods to that action. The first incomplete await returns control to the scheduler while the callback continues later, so the next game tick can enter the same task before the previous reward operation has finished.

The three skill setup flows already have asynchronous entry points for definition and store access. Those flows are the appropriate boundary for loading data, but they must not block the game-loop tick. A stateful result-producing `RsAsyncTask<T>` starts the operation once, observes it on later ticks, and applies the preparation result synchronously when it is complete. The recurring tasks only need deterministic gameplay decisions and in-memory mutations after setup.

## Goals / Non-Goals

**Goals:**

- Preserve the synchronous `ITaskItem.Tick()` and `RsTaskService.Tick()` contracts.
- Make Mining, Fishing, and Woodcutting recurring reward callbacks synchronous and non-overlapping.
- Load loot tables before the recurring task is scheduled.
- Resolve the online-character count through the existing asynchronous store API during setup and pass it into the synchronous continuation.
- Keep async preparation and its synchronous completion callback under one scheduled task with one cancellation and fault owner.
- Preserve existing task cancellation, interruption, reward, exhaustion, and respawn ordering.

**Non-Goals:**

- Do not make the scheduler asynchronous.
- Do not replace the scheduler with a generic continuation, queue, lock, or state-machine framework. The pending queue is only a boundary for cross-thread scheduling.
- Do not change unrelated `RsAsyncTask` call sites or other gameplay skills.
- Do not change loot selection, reward amounts, chance calculations, or persistence behavior.

## Decisions

1. **Use a synchronous `Func<bool>` callback for the three recurring tasks.** The callback's boolean result already represents whether the task should stop. Making that callback synchronous removes the `async void` escape without changing the task's ownership or cancellation model. A new generic task abstraction is unnecessary.

2. **Preload definitions in the existing skill setup methods.** Mining and Woodcutting load their loot tables before queueing the recurring task; Fishing resolves its fishing table before the synchronous fishing completion callback. This reuses the existing async service boundary and leaves inventory, XP, animations, object visibility, and region changes on the game-loop callback.

3. **Make `RsAsyncTask<T>` stateful and result-producing.** The task starts the preparation operation once, retains its pending `Task<T>`, and returns from `Tick()` while that operation is incomplete. A later tick checks completion, obtains the result only after completion is known, and invokes the synchronous completion callback. The same task owns cancellation and fault state, so no fire-and-forget operation or synthetic continuation task is needed.

4. **Use the existing asynchronous character count API during setup.** Mining, Fishing, and Woodcutting resolve `ICharacterStore.CountAsync()` before their synchronous completion callback starts the recurring task, then pass the resulting setup-time snapshot into the recurring callback. No synchronous count property or second count state is added to `CharacterStore`, and the callback performs no asynchronous store operation.

5. **Keep the scheduler tick synchronous.** `ITaskItem.Tick()`, `RsTickTask.Tick()`, and `RsTaskService.Tick()` remain synchronous. `RsAsyncTask<T>` may use `GetAwaiter().GetResult()` only after the pending task reports `IsCompleted`, so result extraction cannot block the scheduler. A second async scheduler would violate the issue requirements.

## Risks / Trade-offs

- [Setup latency] Definition and count reads now complete before the recurring task starts → the existing setup methods are already asynchronous, and no recurring gameplay state is started until required data is available. The count is intentionally a setup-time snapshot until a synchronous store API is needed.
- [Pending operation] Async setup can take multiple ticks → the scheduled result task retains the pending operation and does not start it again.
- [Cancellation and faults] The owner can cancel setup or preparation can fail → `RsAsyncTask<T>` owns the cancellation token and fault state, and no detached continuation remains to outlive the handle.
- [Missing loot definition] Setup can determine that no loot table exists before queueing → the task is not started without the data required to award a reward, avoiding an indefinitely running no-op task.

## Migration Plan

No deployment or data migration is required. Deploy the code change normally. Rollback is a code rollback; no persisted state is changed by this design.

## Open Questions

None.
