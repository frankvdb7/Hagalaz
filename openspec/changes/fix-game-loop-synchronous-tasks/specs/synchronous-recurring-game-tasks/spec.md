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

### Requirement: Asynchronous setup data is available before recurring execution

The Mining, Fishing, and Woodcutting setup flows MUST load the definitions required by recurring reward callbacks before queuing those callbacks as recurring gameplay tasks. They MUST keep asynchronous setup I/O outside the blocking scheduler tick path.

#### Scenario: Mining starts with a valid rock definition

- **GIVEN** Mining setup resolves the rock, ore, pickaxe, and loot definitions
- **WHEN** the Mining task is queued
- **THEN** its recurring callback uses the resolved in-memory data
- **AND** it does not perform asynchronous definition or store access during a tick

#### Scenario: Skill startup I/O completes outside the scheduler tick

- **GIVEN** Mining, Fishing, or Woodcutting startup requires asynchronous definition or cache access
- **WHEN** the startup operation is initiated
- **THEN** the scheduler tick does not synchronously wait for that operation
- **AND** completion enqueues only a synchronous continuation for the next game-loop tick

#### Scenario: Fishing starts with a valid spot table

- **GIVEN** Fishing setup resolves the spot table and required item definitions
- **WHEN** the Fishing task is queued
- **THEN** its recurring callback uses those resolved values
- **AND** it does not perform asynchronous store access during a tick

#### Scenario: Woodcutting starts with a valid tree definition

- **GIVEN** Woodcutting setup resolves the tree, log, hatchet, and loot definitions
- **WHEN** the Woodcutting task is queued
- **THEN** its recurring callback uses those resolved values
- **AND** it does not perform asynchronous definition or store access during a tick

### Requirement: Respawn timing uses the setup-time online-character count

The Mining, Fishing, and Woodcutting setup flows MUST resolve the online-character count through `ICharacterStore.CountAsync()` before queuing the recurring task. The resulting count MUST be passed into the synchronous continuation and used by the recurring reward callback without asynchronous store access.

#### Scenario: Online population is resolved during setup

- **GIVEN** asynchronous setup resolves the online-character count as `100`
- **WHEN** the recurring skill task is queued
- **THEN** the synchronous continuation captures `100`
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
