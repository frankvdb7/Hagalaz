## Purpose

Allow an opted-in Raido connection to survive replacement of its physical transport while retaining the same connection-scoped state and lifetime.

## ADDED Requirements

### Requirement: Opt-in transport rebinding

Raido SHALL preserve one stable connection context while allowing only an opted-in connection to replace its physical transport during a bounded reconnect window.

#### Scenario: Reconnect disabled

- **WHEN** the physical transport closes for a connection that has not enabled stateful reconnect
- **THEN** the Raido connection terminates immediately and no replacement transport can be published

#### Scenario: Reconnect enabled

- **WHEN** the current physical transport closes for an opted-in connection
- **THEN** the stable connection remains alive while it waits for a replacement until the configured timeout expires

### Requirement: Stable connection state

Replacing a physical transport SHALL preserve the connection ID, features, items, caller state, protocol, lifetime-manager membership, and store membership of the stable Raido connection.

#### Scenario: Successful replacement

- **WHEN** a replacement transport is published within the reconnect window
- **THEN** all existing connection-scoped state and the stable lifetime continue to be observed through the same Raido connection

#### Scenario: Physical endpoints

- **WHEN** a connection is detached or rebound
- **THEN** local and remote endpoints reflect the currently published physical transport, and are unavailable while detached

### Requirement: Reconnect publication and ownership

A replacement SHALL become the current usable transport only after its physical registrations have been installed and it has won the active reconnect window under synchronized publication. Exactly one candidate may win a reconnect window. A rejected candidate SHALL remain the caller's responsibility to close.

#### Scenario: Concurrent candidates

- **WHEN** multiple candidates race for one reconnect window
- **THEN** exactly one candidate is published and all losing candidates return failure without being owned by Raido

#### Scenario: Candidate closes before publication

- **WHEN** a candidate's close or close-request signal is already active before publication
- **THEN** the candidate is rejected and cannot become the current transport

#### Scenario: Timeout wins publication race

- **WHEN** the reconnect timeout closes the window before a candidate finishes publication
- **THEN** the connection becomes terminal, the candidate cannot publish later, and the waiter is completed exactly once

### Requirement: Physical operation isolation

Transport operations and callbacks SHALL be associated with the physical transport they captured. Failures and callbacks from a stale transport SHALL not terminate or alter a newer published transport. Detaching a physical transport SHALL wake operations belonging to that transport without cancelling the terminal connection-aborted token.

#### Scenario: Stale operation failure

- **WHEN** a read, write, heartbeat, close, or close-request operation from an old transport completes after a replacement is published
- **THEN** the stale completion is ignored and the replacement remains active

#### Scenario: Current transport failure

- **WHEN** an operation on the current physical transport fails while reconnect is enabled
- **THEN** that transport is detached and the existing reconnect window is used

#### Scenario: Detached write

- **WHEN** a write is requested while no physical transport is published
- **THEN** the write completes without touching a pipe and without reporting a transport write to a discarded pipe

### Requirement: Fresh protocol readers

Raido SHALL create a new protocol reader for each published physical transport and SHALL never request transport input while no physical transport is published.

#### Scenario: Replacement reader

- **WHEN** a replacement transport is published after the previous transport ends
- **THEN** dispatch resumes with a fresh reader over the replacement transport

#### Scenario: Physical detach wakes dispatch

- **WHEN** the current physical transport detaches while dispatch is waiting for input
- **THEN** the physical read is woken, the reconnect waiter can be awaited, and the terminal connection-aborted token remains uncancelled

### Requirement: Physical callback rebinding

Keep-alive and lifetime-notification callbacks SHALL be registered against each captured physical transport's features, including close-request notification when available. A successful replacement SHALL receive the same applicable physical registrations.

#### Scenario: Replacement callback registration

- **WHEN** a replacement transport wins publication
- **THEN** its close, close-request, keep-alive, and active client-timeout callbacks use the replacement's physical features

#### Scenario: Reconnect cycles

- **WHEN** a successfully rebound transport later disconnects
- **THEN** a new reconnect waiter is created for that later disconnect; a timed-out reconnect window does not create another window
