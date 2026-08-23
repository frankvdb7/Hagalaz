## ADDED Requirements

### Requirement: Non-opted-in connections remain terminal on transport loss

Raido MUST keep the current immediate terminal-disconnect behavior when stateful reconnect is not enabled for a logical connection.

#### Scenario: Default transport loss

- **WHEN** a non-opted-in physical connection closes
- **THEN** the logical connection is removed through the existing terminal cleanup path and hub disconnect is invoked without a grace window

### Requirement: Opted-in transport loss retains one logical connection

Raido MUST retain an opted-in logical connection in a reconnecting state for one bounded grace period after its active physical transport is lost.

#### Scenario: Loss enters grace

- **WHEN** an opted-in active physical transport closes
- **THEN** the physical generation is detached, input dispatch and writes from that generation stop, the logical connection remains registered, and terminal hub disconnect is not invoked yet

### Requirement: A replacement transport rebinds atomically

Raido MUST provide one application-callable operation that can rebind a known reconnecting logical connection to exactly one replacement physical transport.

#### Scenario: Successful rebind

- **WHEN** a valid replacement transport is presented for a known logical connection during its grace window
- **THEN** one new physical generation becomes active, the logical connection id/caller context/features/items/protocol association/client destination survive, and the grace timer is cancelled

#### Scenario: Concurrent rebinds

- **WHEN** multiple replacement transports attempt to rebind the same logical connection concurrently
- **THEN** exactly one attempt succeeds and all losing attempts are rejected without replacing the winner

### Requirement: Stale generations are fenced

Raido MUST prevent a detached or losing physical generation from dispatching new input or receiving writes after a successful rebind.

#### Scenario: Stale input

- **WHEN** work read from an old generation reaches the dispatch boundary after a replacement has won
- **THEN** the work is rejected and no hub handler is invoked for that stale generation

#### Scenario: Stale write

- **WHEN** a write targets an old generation after a replacement has won
- **THEN** the write fails or is rejected and no bytes are delivered to the old transport

### Requirement: Detached sends are explicit and not replayed

Raido MUST make sends during the reconnecting state fail with one explicit unavailable/reconnecting outcome and MUST NOT buffer or replay them.

#### Scenario: Send during grace

- **WHEN** the lifetime manager sends to a logical connection with no active physical transport
- **THEN** the send fails explicitly and no later rebind delivers that send

### Requirement: Terminal transitions are idempotent

Raido MUST remove a retained logical connection and invoke terminal cleanup exactly once when grace expires, an application explicitly closes it, or server shutdown occurs.

#### Scenario: Grace expiry

- **WHEN** the reconnect grace period expires without a winning rebind
- **THEN** the logical connection transitions to closed, is removed, and terminal disconnect occurs once

#### Scenario: Explicit close during grace

- **WHEN** an application explicitly closes a reconnecting logical connection
- **THEN** the grace timer is cancelled, the logical connection closes once, and a later replacement is rejected

#### Scenario: Shutdown

- **WHEN** the Raido server shuts down while logical connections are retained
- **THEN** retained connections are closed and are not left in the store after shutdown

### Requirement: Application code can veto stateful reconnect

Raido MUST expose a one-way connection feature for application or protocol code to disable stateful reconnect when the logical connection is no longer eligible for retention.

#### Scenario: Veto before transport loss

- **WHEN** application code disables reconnect on an opted-in connected logical connection and its physical transport later closes
- **THEN** the physical loss follows the immediate terminal cleanup path instead of entering the reconnect grace window

#### Scenario: Veto during grace

- **WHEN** application code disables reconnect while an opted-in logical connection is in its reconnect grace window
- **THEN** the logical connection closes immediately, terminal cleanup is invoked once, and later replacement transports are rejected
