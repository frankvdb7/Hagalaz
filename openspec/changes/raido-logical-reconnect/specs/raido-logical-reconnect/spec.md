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
- **THEN** the physical session is detached, its pumps stop, the logical connection remains registered, and terminal hub disconnect is not invoked yet

### Requirement: A replacement transport rebinds atomically

Raido MUST provide one application-callable operation that can rebind a known reconnecting logical connection to exactly one replacement physical transport.

#### Scenario: Successful rebind

- **WHEN** a valid replacement transport is presented for a known logical connection during its grace window
- **THEN** one replacement physical session becomes active, the logical connection id/caller context/features/items/protocol association/client destination survive, and the grace timer is cancelled

#### Scenario: Concurrent rebinds

- **WHEN** multiple replacement transports attempt to rebind the same logical connection concurrently
- **THEN** exactly one attempt succeeds and all losing attempts are rejected without replacing the winner

### Requirement: Physical pumps have one owner

Raido MUST stop the previous physical pumps and release their stable application-pipe reader/writer ownership before starting replacement pumps.

#### Scenario: Replacement pump ownership

- **WHEN** a replacement wins during the reconnect grace window
- **THEN** the old physical session cannot receive later logical writes, the replacement is the only active pump pair, and the logical handler continues to read the same application pipe

### Requirement: The physical session owns its lifetime

Raido MUST keep the physical-session handler alive independently of the temporary replacement application handler until the physical connection itself terminates.

#### Scenario: Successful logical transfer

- **WHEN** a replacement handshake commits to an existing logical connection
- **THEN** the replacement application handler may finish, while the physical session task remains active and the original logical handler continues on its stable application pipe

### Requirement: Reader ownership is explicit

Raido MUST leave completion of the stable application reader to its handler owner and MUST transfer unread input exactly once at a successful replacement boundary.

#### Scenario: Same-buffer replacement

- **WHEN** the replacement handshake has an opcode-18 message followed by the first encrypted game packet in one read buffer
- **THEN** the handshake reader advances through the opcode once, the unread suffix is written once to the target application pipe after the fresh protocol is installed, and the target handler dispatches the game packet once

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
