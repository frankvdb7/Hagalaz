## Purpose

GameWorld tests must exercise game ticks without retaining uncontrolled background work or test-owned records after the test that created them finishes.

## ADDED Requirements

### Requirement: Ordinary game-worker behavior tests use bounded tick execution

GameWorker behavior tests MUST execute only the number of game ticks required by the assertion. They MUST NOT depend on an uncontrolled zero-duration hosted-service loop.

#### Scenario: A behavior test executes one tick

- **WHEN** a test verifies scheduler, snapshot, region-phase, or overrun behavior for one tick
- **THEN** exactly one bounded tick is executed and no background worker remains active after the test completes

#### Scenario: A behavior test executes adjacent ticks

- **WHEN** a test verifies behavior across two ticks
- **THEN** the second tick begins only after the first tick has completed and both tick operations have a deterministic owner

### Requirement: Hosted worker shutdown owns all started work

Any test that starts the hosted game worker MUST cancel and await its execution task on both successful and exceptional paths. Test cleanup MUST NOT leave a started worker executing after the test has ended.

#### Scenario: Hosted shutdown occurs while a tick is executing

- **WHEN** a hosted worker is stopped while its current tick is blocked
- **THEN** shutdown waits for or explicitly reports the bounded host shutdown outcome, and the test releases and awaits the worker before cleanup finishes

#### Scenario: Tick cancellation is the worker cancellation

- **WHEN** the worker cancellation token cancels a pending tick
- **THEN** the worker exits without logging an unexpected tick failure and without starting another tick

#### Scenario: A tick throws an unexpected exception

- **WHEN** one tick throws an unexpected exception
- **THEN** the exception is observed by the worker lifecycle test, the loop's documented continuation behavior is preserved, and cleanup still awaits worker termination

### Requirement: Test-owned diagnostic records have bounded ownership

Test loggers and other test-owned recorders MUST be owned by the test that creates them and MUST NOT be the retention mechanism for an uncontrolled producer. A recorder used to assert a bounded event MUST retain only the events needed by that assertion.

#### Scenario: A completed test releases its diagnostic recorder

- **WHEN** a worker test completes or fails
- **THEN** its diagnostic recorder and worker callbacks are no longer reachable through a running task or substitute call router

### Requirement: Hot-path fixture retention is evidence-driven

Pathfinder tests MUST NOT use a call-recording substitute for high-frequency collision lookups when measured call retention materially increases the test host's live heap. If lookup observations are required, the fixture MUST record only the scalar data needed by the assertion.

#### Scenario: Pathfinder collision lookup retention is measured

- **WHEN** a pathfinder test performs a high-volume collision search
- **THEN** its fixture either uses a non-recording collision provider or demonstrates that retained call history is not a material contributor to the observed memory growth

### Requirement: Generated project output is excluded from MSBuild inputs

Project-local generated `artifacts` output MUST be excluded from implicit
MSBuild item evaluation, including design-time evaluation, so nested publish
or plugin output cannot recursively become source input during GameWorld
validation.

#### Scenario: GameWorld artifacts contain nested build output

- **WHEN** the GameWorld project is evaluated or compiled while its generated
  `artifacts` directory contains nested output trees
- **THEN** those files are absent from the implicit compile/evaluation inputs
- **AND** the project can compile without input-graph memory growth caused by
  recursively nested generated output
