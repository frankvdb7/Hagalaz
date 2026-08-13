## Why

Mining, Fishing, and Woodcutting currently assign `async void` callbacks to recurring `RsTickTask` instances. Once a callback reaches asynchronous I/O, the scheduler considers the tick finished and can start another reward attempt, allowing rewards and world mutations to overlap outside the serialized game loop. Issue #393 requires preserving the synchronous scheduler contract while isolating that I/O before recurring tasks run.

## What Changes

- Keep `ITaskItem.Tick()`, `RsTickTask`, and `RsTaskService.Tick()` synchronous.
- Replace the three recurring skill tasks' `async void` callbacks with synchronous callbacks.
- Preload loot definitions and the online-character count during Mining, Fishing, and Woodcutting task setup, before the recurring task is queued.
- Keep asynchronous skill startup I/O outside the blocking scheduler path by running ordinary async startup methods through a non-blocking `RsAsyncTask` whose continuations are resumed by a scheduler-owned game-loop synchronization context.
- Allow generic `RsAsyncTask` and `QueueTask` callers to provide an optional externally owned cancellation token without migrating the Mining, Fishing, or Woodcutting interactions to that capability.
- Define an explicit scheduler phase boundary: accept tasks scheduled before the tick, resume the continuation batch pending at that boundary, then tick the owned task set. Work scheduled during continuation or task processing starts on the next tick.
- Make the continuation handoff lossless when a worker posts concurrently with the game-loop batch swap.
- Prevent an interrupted Mining, Fishing, or Woodcutting interaction from starting after its asynchronous setup completes.
- Keep inventory, experience, animation, movement, and world mutations inside the synchronous recurring callback.
- Add deterministic tests covering callback completion, task ordering, interruption/cancellation, and delayed respawn scheduling.

## Capabilities

### New Capabilities

- `synchronous-recurring-game-tasks`: Recurring gameplay tasks execute synchronously and consume all required asynchronous data before they enter the game-loop path.

### Modified Capabilities

None.

## Impact

The change affects `RsAsyncTask`, the `QueueTask` extension overloads, and `Hagalaz.Game.Scripts` Mining, Fishing, and Woodcutting task/setup code plus focused MSTest coverage. It does not add dependencies, change the scheduler tick contract, or require a data migration. Setup operations remain asynchronous; recurring task execution remains synchronous. The skills continue to use tokenless startup methods; external task lifetime cancellation is only a generic capability for future callers.

## Acceptance Criteria

- No scheduled Mining, Fishing, or Woodcutting recurring task uses `async void`.
- Required loot, definition, and online-character count data is loaded before the recurring task is queued; recurring callbacks use the captured setup-time count.
- Mining, Fishing, and Woodcutting startup database/cache work is not executed through a blocking `RsAsyncTask.Tick()`; ordinary async methods are resumed on the game loop after their awaits.
- Respawn calculations use the online-character count resolved during asynchronous setup.
- A recurring tick cannot return before its reward callback and mutations complete.
- Scheduling from either an async continuation or ordinary task processing cannot start the new task in the same tick, and continuations posted while a continuation batch is running wait for the next batch.
- Concurrent continuation posts are not lost at the batch boundary.
- An interruption during asynchronous skill setup prevents the recurring skill task from being created.
- Generic async tasks can link an externally owned cancellation token with their task-handle cancellation source.
- Existing task ordering, cancellation/interruption, and delayed respawn behavior remain covered by deterministic tests.

## Stop Conditions

Do not make `Tick()` asynchronous, migrate unrelated asynchronous gameplay tasks, introduce `QueueAsyncTask(Func<Task<Action?>>)`, preparation DTOs, or a generic result-task API. The synchronization context is only a continuation handoff owned by `RsTaskService`; it is not a second scheduler.
