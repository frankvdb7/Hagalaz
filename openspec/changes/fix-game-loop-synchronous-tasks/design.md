## Context

`RsTaskService.Tick()` walks scheduled `ITaskItem` instances synchronously. `RsTickTask` invokes its `Action` synchronously, but MiningTask, FishingTask, and WoodcuttingTask currently assign `async void` methods to that action. The first incomplete await returns control to the scheduler while the callback continues later, so the next game tick can enter the same task before the previous reward operation has finished.

The three skill setup flows already have asynchronous entry points for definition and store access. Those flows are the appropriate boundary for loading data. The recurring tasks only need deterministic gameplay decisions and in-memory mutations after setup.

## Goals / Non-Goals

**Goals:**

- Preserve the synchronous `ITaskItem.Tick()` and `RsTaskService.Tick()` contracts.
- Make Mining, Fishing, and Woodcutting recurring reward callbacks synchronous and non-overlapping.
- Load loot tables and the character-count input used for respawn timing before the recurring task is scheduled.
- Preserve existing task cancellation, interruption, reward, exhaustion, and respawn ordering.

**Non-Goals:**

- Do not make the scheduler asynchronous.
- Do not replace the scheduler with a generic continuation, queue, lock, or state-machine framework.
- Do not change unrelated `RsAsyncTask` call sites or other gameplay skills.
- Do not change loot selection, reward amounts, chance calculations, or persistence behavior.

## Decisions

1. **Use a synchronous `Func<bool>` callback for the three recurring tasks.** The callback's boolean result already represents whether the task should stop. Making that callback synchronous removes the `async void` escape without changing the task's ownership or cancellation model. A new generic task abstraction is unnecessary.

2. **Preload asynchronous inputs in the existing skill setup methods.** Mining and Woodcutting load their loot tables before queueing the recurring task; Fishing already receives its fishing table and additionally samples the character count before queueing. This reuses the existing async service/store boundary and leaves inventory, XP, animations, object visibility, and region changes on the game-loop callback.

3. **Capture setup data in the callback.** The callback uses the loaded loot table and character-count value rather than querying a service or store during a tick. This prevents concurrent I/O from escaping the serialized path. The character count is a setup-time snapshot, matching the existing respawn formula while trading a small amount of freshness for a synchronous tick.

4. **Keep the scheduler unchanged.** `ITaskItem.Tick()`, `RsTickTask.Tick()`, and `RsTaskService.Tick()` already provide the required synchronous ordering and exception boundary. Adding blocking `GetAwaiter().GetResult()`, locks, or an async scheduler would either violate the issue requirements or create a second ownership mechanism.

## Risks / Trade-offs

- [Setup latency] Definition/count reads now complete before the recurring task starts → the existing setup methods are already asynchronous, and no recurring gameplay state is started until required data is available.
- [Count freshness] Respawn timing uses a setup-time character-count snapshot → this is bounded to the task start and avoids database access during reward execution; the existing formula and scheduling path remain unchanged.
- [Missing loot definition] Setup can determine that no loot table exists before queueing → the task is not started without the data required to award a reward, avoiding an indefinitely running no-op task.

## Migration Plan

No deployment or data migration is required. Deploy the code change normally. Rollback is a code rollback; no persisted state is changed by this design.

## Open Questions

None.
