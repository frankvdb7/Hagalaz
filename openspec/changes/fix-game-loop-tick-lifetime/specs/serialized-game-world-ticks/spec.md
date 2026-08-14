## ADDED Requirements

### Requirement: Major world ticks are serialized

The GameWorld worker MUST await the complete major update, client-prepare, client-update, and client-reset phases before it can start another major tick.

#### Scenario: A tick exceeds its budget

- **WHEN** a region's major update remains incomplete beyond the configured tick duration
- **THEN** no subsequent major update or client phase from another tick begins until the original four-phase pipeline completes

#### Scenario: Adjacent ticks complete normally

- **WHEN** one major tick completes and the worker starts the next iteration
- **THEN** the next tick's phases begin only after every phase of the previous tick has completed

### Requirement: Region phase ordering is preserved

The worker MUST execute all region major updates, then all client-prepare updates, then all client updates, then all client-reset updates, without overlapping phases.

#### Scenario: Multiple active regions are present

- **WHEN** a major tick snapshots multiple active regions
- **THEN** each of the four phases runs in the existing phase order over that snapshot

### Requirement: Worker shutdown owns in-flight work

The hosted GameWorld worker MUST retain ownership of its execution task and MUST NOT complete normal shutdown while a worker-owned region phase is still running.

#### Scenario: Shutdown begins during a region phase

- **WHEN** `StopAsync` is called while a major region phase is blocked
- **THEN** shutdown remains incomplete until that phase and the worker task finish, and no region phase continues after successful shutdown

#### Scenario: Host cancellation is requested

- **WHEN** the worker stopping token is cancelled while the worker is delaying or between ticks
- **THEN** the worker exits cleanly without reporting routine host cancellation as an unexpected major tick failure

### Requirement: Tick failures and overruns remain observable

The worker MUST log genuine major tick exceptions and MUST report a completed major tick that exceeds its configured duration, without treating an overrun as permission to overlap work.

#### Scenario: A major tick overruns

- **WHEN** the completed major region pipeline takes longer than `TickTimeSpan`
- **THEN** the worker emits an overrun warning and still waits for the pipeline before starting another tick

#### Scenario: A major tick faults

- **WHEN** a region phase throws an unexpected exception
- **THEN** the worker logs the exception as a major tick failure and does not start another tick until the faulted pipeline has ended
