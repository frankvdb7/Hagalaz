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
