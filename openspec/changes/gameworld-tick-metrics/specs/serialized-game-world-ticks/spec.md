## ADDED Requirements

### Requirement: Tick duration, outcome and overrun are measured end to end

The worker that owns the tick MUST record one `hagalaz.gameworld.tick` count tagged `outcome` (`success`, `failure` or `cancelled`) and one `hagalaz.gameworld.tick.duration` in seconds for every attempted tick, and MUST record `hagalaz.gameworld.tick.overrun` when that duration exceeds `TickTimeSpan`.

#### Scenario: A tick completes within its budget

- **WHEN** a tick completes in no more than `TickTimeSpan`
- **THEN** one `success` tick and its duration are recorded and no overrun is recorded

#### Scenario: The duration spans the whole attempt

- **WHEN** a tick runs
- **THEN** its duration begins before the task scheduler tick and ends after the pipeline completes or faults, unlike the existing major-pipeline overrun log, which starts after task scheduling

#### Scenario: A tick exceeds its budget

- **WHEN** a tick takes longer than `TickTimeSpan`
- **THEN** one overrun is recorded in addition to the tick count and duration

#### Scenario: A phase throws

- **WHEN** a tick phase throws an unexpected exception
- **THEN** one `failure` tick and its elapsed duration are recorded, and an overrun is recorded if that duration exceeded the budget

#### Scenario: The worker stopping token cancels a tick

- **WHEN** the tick ends with an `OperationCanceledException` carrying the worker stopping token
- **THEN** one `cancelled` tick is recorded, and a cancellation carrying any other token is recorded as `failure`

### Requirement: Tick phases are timed only when entered

The worker MUST record `hagalaz.gameworld.tick.phase.duration` in seconds, tagged `phase` with one of `scheduler`, `major_update`, `client_prepare`, `client_update`, `client_reset` or `housekeeping`, for each phase it enters, including the elapsed time of a phase that throws. It MUST NOT record a phase it never entered. Phase measurements MUST NOT carry region, player or other unbounded tags.

#### Scenario: A tick completes

- **WHEN** a tick completes
- **THEN** the six phases are recorded once each in pipeline order

#### Scenario: Client prepare fails

- **WHEN** client prepare throws
- **THEN** `scheduler`, `major_update`, `client_prepare` and `client_reset` are recorded, and `client_update` and `housekeeping` are not

### Requirement: Tick cadence is measured from actual starts

The worker MUST record `hagalaz.gameworld.tick.interval` in seconds as the monotonic time between the start timestamps of successive ticks, so that tick duration and any delay between ticks are both visible. The first tick records no interval.

#### Scenario: Two ticks run in sequence

- **WHEN** a second tick starts after the first has finished
- **THEN** the interval equals the time from the first tick's start to the second tick's start

### Requirement: Disabled tick metrics add no timing work

When no listener is subscribed to a tick instrument, the worker MUST NOT take timestamps or build tags for it.

#### Scenario: Nothing is listening

- **WHEN** no tick instrument is enabled
- **THEN** a tick runs without reading timestamps for tick, interval or phase measurements
