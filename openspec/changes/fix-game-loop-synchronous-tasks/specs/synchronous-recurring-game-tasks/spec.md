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

The Mining, Fishing, and Woodcutting setup flows MUST load the definitions and respawn inputs required by recurring reward callbacks before queuing those callbacks as recurring gameplay tasks.

#### Scenario: Mining starts with a valid rock definition

- **GIVEN** Mining setup resolves the rock loot table and respawn inputs
- **WHEN** the Mining task is queued
- **THEN** its recurring callback uses the resolved in-memory data
- **AND** it does not perform asynchronous definition or store access during a tick

#### Scenario: Fishing starts with a valid spot table

- **GIVEN** Fishing setup resolves the spot table and the respawn-count input
- **WHEN** the Fishing task is queued
- **THEN** its recurring callback uses those resolved values
- **AND** it does not perform asynchronous store access during a tick

#### Scenario: Woodcutting starts with a valid tree definition

- **GIVEN** Woodcutting setup resolves the tree loot table and respawn-count input
- **WHEN** the Woodcutting task is queued
- **THEN** its recurring callback uses those resolved values
- **AND** it does not perform asynchronous definition or store access during a tick

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
