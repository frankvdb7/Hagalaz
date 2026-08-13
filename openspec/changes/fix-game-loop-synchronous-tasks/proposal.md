## Why

Mining, Fishing, and Woodcutting currently assign `async void` callbacks to recurring `RsTickTask` instances. Once a callback reaches asynchronous I/O, the scheduler considers the tick finished and can start another reward attempt, allowing rewards and world mutations to overlap outside the serialized game loop. Issue #393 requires preserving the synchronous scheduler contract while isolating that I/O before recurring tasks run.

## What Changes

- Keep `ITaskItem.Tick()`, `RsTickTask`, and `RsTaskService.Tick()` synchronous.
- Replace the three recurring skill tasks' `async void` callbacks with synchronous callbacks.
- Preload loot definitions and respawn inputs during Mining, Fishing, and Woodcutting task setup, before the recurring task is queued.
- Keep inventory, experience, animation, movement, and world mutations inside the synchronous recurring callback.
- Add deterministic tests covering callback completion, task ordering, interruption/cancellation, and delayed respawn scheduling.

## Capabilities

### New Capabilities

- `synchronous-recurring-game-tasks`: Recurring gameplay tasks execute synchronously and consume all required asynchronous data before they enter the game-loop path.

### Modified Capabilities

None.

## Impact

The change affects `Hagalaz.Game.Scripts` Mining, Fishing, and Woodcutting task/setup code plus focused MSTest coverage. It does not add dependencies, change the scheduler API, or require a data migration. Setup operations remain asynchronous; recurring task execution remains synchronous.

## Acceptance Criteria

- No scheduled Mining, Fishing, or Woodcutting recurring task uses `async void`.
- Required loot/count data is loaded before the recurring task is queued.
- A recurring tick cannot return before its reward callback and mutations complete.
- Existing task ordering, cancellation/interruption, and delayed respawn behavior remain covered by deterministic tests.

## Stop Conditions

Do not redesign `RsTaskService`, introduce blocking async wrappers, add a generic async scheduler, or migrate unrelated asynchronous gameplay tasks as part of this change.
