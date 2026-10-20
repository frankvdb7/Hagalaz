## ADDED Requirements

### Requirement: Recurring gameplay ticks are synchronous

The Mining, Fishing, and Woodcutting recurring gameplay tasks MUST execute their complete tick callback synchronously through the existing `ITaskItem.Tick()` and `RsTickTask` path.

#### Scenario: A reward callback is eligible during a tick

- **GIVEN** a Mining, Fishing, or Woodcutting task is scheduled and its reward chance succeeds
- **WHEN** the scheduler invokes `Tick()`
- **THEN** the reward callback and all resulting gameplay mutations complete before `Tick()` returns
- **AND** no asynchronous callback remains active for that tick

#### Scenario: Consecutive ticks are processed

- **GIVEN** a recurring skill task remains active after a reward callback
- **WHEN** the scheduler invokes two consecutive ticks
- **THEN** the second tick starts only after the first tick has completed
- **AND** the reward callbacks cannot overlap

### Requirement: Asynchronous startup returns through the game loop

The Mining, Fishing, and Woodcutting startup flows MUST use ordinary async methods for definition and store access without blocking `RsTaskService.Tick()`. Their post-await gameplay code MUST execute on the owning game loop.

#### Scenario: Startup begins without blocking a tick

- **GIVEN** a skill interaction queues `() => Start...Async(...)`
- **WHEN** the scheduler starts the `RsAsyncTask`
- **THEN** the async method runs until its first incomplete await and the tick returns without waiting for I/O

#### Scenario: Startup continuation is resumed by the scheduler

- **GIVEN** asynchronous startup I/O completes after the initial tick
- **WHEN** the async await continuation is posted
- **THEN** it is held in the scheduler-owned synchronization-context queue
- **WHEN** the next scheduler tick runs
- **THEN** the continuation and its synchronous gameplay setup execute on that game-loop tick
- **AND** no blocking wait or separate continuation task is used

### Requirement: Scheduler phases have a stable tick boundary

`RsTaskService.Tick()` MUST first accept tasks scheduled before the tick, then resume the continuation batch pending at that boundary, and then tick the already-owned task set. Tasks scheduled during continuation or task processing MUST wait until the next tick.

#### Scenario: Task scheduled by an async continuation waits for the next tick

- **GIVEN** an async continuation resumes during a scheduler tick
- **WHEN** that continuation schedules a gameplay task
- **THEN** the gameplay task is not ticked during the current tick
- **AND** it is accepted and ticked on the following tick

#### Scenario: Task scheduled during ordinary task processing waits for the next tick

- **GIVEN** an owned task schedules another task from its synchronous `Tick()` callback
- **WHEN** the scheduler is processing the owned task set
- **THEN** the newly scheduled task is not ticked during the current tick
- **AND** it is accepted and ticked on the following tick

### Requirement: Continuation batches are bounded by the game-loop tick

`GameLoopSynchronizationContext.RunPending()` MUST process only the continuation batch captured when it starts. Continuations posted while that batch runs MUST remain queued for the next invocation.

#### Scenario: Continuation posted by a continuation waits for the next batch

- **GIVEN** a pending continuation posts another continuation while it runs
- **WHEN** `RunPending()` processes the current batch
- **THEN** the posted continuation is not executed by that invocation
- **AND** it executes during the next `RunPending()` invocation

#### Scenario: Concurrent continuation posting is not lost at the batch boundary

- **GIVEN** a worker thread posts a continuation while the game loop begins a `RunPending()` batch
- **WHEN** the game loop swaps the pending queue and drains the captured batch
- **THEN** the posted continuation remains in the active pending queue or the captured batch
- **AND** it executes during the current or a subsequent `RunPending()` invocation

#### Scenario: Startup cancellation does not suppress a pending continuation

- **GIVEN** an async startup task has a continuation pending in the game-loop context
- **WHEN** its task handle is canceled before the continuation runs
- **THEN** the pending continuation still runs on the game loop
- **AND** normal async cleanup, including `finally` blocks, is executed

#### Scenario: Cooperative startup cancellation follows the underlying task

- **GIVEN** an async startup operation accepts the provided `CancellationToken`
- **WHEN** its task handle is canceled and the operation observes the token
- **THEN** the underlying task becomes canceled
- **AND** `RsAsyncTask.IsCancelled` becomes true
- **AND** the scheduler removes the task on a subsequent tick

#### Scenario: Non-cooperative startup ignores a cancellation request

- **GIVEN** an async startup operation does not accept or observe a cancellation token
- **WHEN** its task handle is canceled
- **THEN** the operation continues through its queued continuation
- **AND** it may complete successfully
- **AND** `RsAsyncTask.IsCancelled` remains false

#### Scenario: An externally owned cancellation token prevents a task from starting

- **GIVEN** a caller queues an async task with an externally owned `CancellationToken`
- **WHEN** that token is canceled before the task's first tick
- **THEN** the linked `RsAsyncTask` does not invoke the operation
- **AND** `RsAsyncTask.IsCancelled` is true

#### Scenario: Skill startup does not adopt the generic external token

- **GIVEN** Mining, Fishing, or Woodcutting startup is queued through the tokenless `QueueTask(() => Start...Async(...))` form
- **WHEN** its asynchronous setup is running
- **THEN** the skill method does not receive or propagate a `CancellationToken`
- **AND** its temporary interruption handler performs the single stale-interaction check before synchronous gameplay setup

#### Scenario: Terminal disposable tasks release owned resources

- **GIVEN** the scheduler owns a task that is canceled, completed, or faulted and implements `IDisposable`
- **WHEN** the scheduler removes the terminal task from its active list
- **THEN** it calls `Dispose()` exactly as part of that removal

#### Scenario: Retained handles can cancel terminal async tasks safely

- **GIVEN** an `RsAsyncTask` has completed or has been disposed by the scheduler
- **WHEN** a retained `RsTaskHandle` calls `Cancel()`
- **THEN** the call is harmless and does not access a disposed cancellation source

#### Scenario: A synchronously thrown cancellation is not treated as a fault

- **GIVEN** an async task delegate throws `OperationCanceledException` while it is being invoked
- **WHEN** the task is ticked
- **THEN** the task becomes canceled
- **AND** it is not marked faulted
- **AND** the cancellation exception does not escape as a task fault

#### Scenario: Interrupted skill startup does not begin the action

- **GIVEN** Mining, Fishing, or Woodcutting asynchronous startup is waiting for service data
- **WHEN** a `CreatureInterruptedEvent` is received before setup completes
- **THEN** the startup exits without creating its recurring skill task
- **AND** the temporary interruption handler is unregistered

#### Scenario: Mining starts with resolved definitions

- **GIVEN** Mining startup resolves the rock, ore, pickaxe, and loot definitions
- **WHEN** the startup method continues after its awaits
- **THEN** its synchronous setup uses the resolved in-memory data
- **AND** the recurring callback performs no asynchronous definition or store access

#### Scenario: Fishing starts with a resolved spot table

- **GIVEN** Fishing startup resolves the spot table and online-character count
- **WHEN** the startup method continues after its awaits
- **THEN** its synchronous fishing setup uses those resolved values
- **AND** it performs no asynchronous store access during recurring execution

#### Scenario: Woodcutting starts with resolved definitions

- **GIVEN** Woodcutting startup resolves the tree, log, hatchet, loot, and online-character data
- **WHEN** the startup method continues after its awaits
- **THEN** its synchronous setup uses the resolved in-memory data
- **AND** the recurring callback performs no asynchronous definition or store access

### Requirement: Respawn timing uses the setup-time online-character count

The Mining, Fishing, and Woodcutting setup flows MUST resolve the online-character count through `ICharacterStore.CountAsync()` before queuing the recurring task. The resulting setup-time value MUST be captured by the synchronous gameplay setup and used by the recurring reward callback without asynchronous store access.

#### Scenario: Online population is resolved during setup

- **GIVEN** asynchronous setup resolves the online-character count as `100`
- **WHEN** the recurring skill task is created
- **THEN** the synchronous gameplay setup captures `100`
- **AND** the reward callback uses `100` when calculating respawn timing
- **AND** no asynchronous character-store operation occurs in the reward callback

#### Scenario: Character-store count remains asynchronous

- **GIVEN** a caller needs the number of online characters
- **WHEN** it queries the character store
- **THEN** it awaits `CountAsync()`
- **AND** no synchronous count property or duplicate count state is required

### Requirement: Existing task lifecycle behavior is preserved

The recurring skill tasks MUST retain their existing cancellation, interruption, reward-result, and delayed respawn behavior while removing escaped asynchronous callbacks.

#### Scenario: The performer is interrupted

- **GIVEN** a Mining, Fishing, or Woodcutting task is active
- **WHEN** a `CreatureInterruptedEvent` is received
- **THEN** the task is canceled through its existing task lifecycle
- **AND** its interruption handler is unregistered

#### Scenario: A resource is exhausted

- **GIVEN** a reward callback exhausts the rock, fishing spot, or tree
- **WHEN** the synchronous callback schedules respawn
- **THEN** the resource mutation occurs before the delayed respawn task is scheduled
- **AND** the recurring task stops according to the existing reward result
