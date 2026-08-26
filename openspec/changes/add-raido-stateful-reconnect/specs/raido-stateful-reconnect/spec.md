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
- **THEN** the stable connection remains alive while it waits for a replacement until the single deadline that began at physical detach expires

#### Scenario: Current close request

- **WHEN** `ConnectionClosedRequested` is signalled by the current physical transport
- **THEN** the stable Raido connection terminates immediately and no replacement can be published

#### Scenario: Detached transport close request

- **WHEN** `ConnectionClosedRequested` is signalled by the physical transport that owns the active detached reconnect window before replacement publication is claimed
- **THEN** the stable Raido connection terminates and the pending replacement cannot be published

#### Scenario: Protocol or application failure

- **WHEN** protocol parsing, malformed or incomplete data, a message-size violation, or application dispatch throws an exception that is not identified as coming from the captured physical `PipeReader.ReadAsync` operation
- **THEN** the existing terminal error/disconnect path runs and no reconnect window is opened

### Requirement: Stable connection state

Replacing a physical transport SHALL preserve the connection ID, features, items, caller state, protocol, lifetime-manager membership, and store membership of the stable Raido connection.

#### Scenario: Successful replacement

- **WHEN** a replacement transport is published within the reconnect window
- **THEN** all existing connection-scoped state and the stable lifetime continue to be observed through the same Raido connection and transient physical failure state is cleared

#### Scenario: Physical endpoints

- **WHEN** a connection is detached or rebound
- **THEN** local and remote endpoints reflect the currently published physical transport, and are unavailable while detached

### Requirement: Reconnect publication and ownership

A replacement SHALL become the current usable transport only after its physical registrations have been installed and it has won the active reconnect window under synchronized publication. The final successful validation and claim under `_reconnectLock` is the replacement publication linearization point; state is published and the reconnect waiter is completed before that lock is released. Exactly one candidate may win a reconnect window. A rejected candidate SHALL remain the caller's responsibility to close.

#### Scenario: Concurrent candidates

- **WHEN** multiple candidates race for one reconnect window
- **THEN** exactly one candidate is published and all losing candidates return failure without being owned by Raido

#### Scenario: Candidate closes before publication

- **WHEN** a candidate's close or close-request signal is already active before publication
- **THEN** the candidate is rejected and cannot become the current transport

#### Scenario: Timeout wins publication race

- **WHEN** the reconnect timeout closes the window before a candidate finishes publication
- **THEN** the connection becomes terminal, the candidate cannot publish later, and the waiter is completed exactly once

#### Scenario: Close request wins publication race

- **WHEN** the detached transport's `ConnectionClosedRequested` token is already signalled before a candidate reaches the final publication claim
- **THEN** the connection becomes terminal and the candidate cannot publish

### Requirement: Physical operation isolation

Transport operations and callbacks SHALL be associated with the physical transport they captured. Failures and callbacks from a stale transport SHALL not terminate or alter a newer published transport. Detaching a physical transport SHALL wake operations belonging to that transport without cancelling the terminal connection-aborted token.

#### Scenario: Stale operation failure

- **WHEN** a read, write, heartbeat, close, or close-request operation from an old transport completes after a replacement is published
- **THEN** the stale completion is ignored and the replacement remains active

#### Scenario: Stale detached close request

- **WHEN** a close-request callback from the replaced transport executes after a replacement has published
- **THEN** the callback is ignored and the replacement remains active

#### Scenario: Current transport failure

- **WHEN** a deliberately recognized physical cancellation or I/O failure captured from the current physical transport's read operation occurs while reconnect is enabled
- **THEN** that transport is detached and the existing reconnect window is used

#### Scenario: Unrecognized operation failure

- **WHEN** a parser, protocol, application, or otherwise unrecognized operation exception occurs, including an exception whose type happens to be `IOException` or `OperationCanceledException` but which was thrown by the parser rather than the physical read
- **THEN** the exception follows the terminal error/disconnect path instead of being treated as a reconnectable transport failure

#### Scenario: Detached write

- **WHEN** a write is requested while no physical transport is published
- **THEN** the write completes without touching a pipe and without reporting a transport write to a discarded pipe

#### Scenario: Protocol serialization failure

- **WHEN** `Protocol.WriteMessage` or output metadata access throws while writing, including an `IOException` or `OperationCanceledException`
- **THEN** the failure follows the terminal error/disconnect path and does not open a reconnect window

#### Scenario: Physical output failure

- **WHEN** the captured physical output's `FlushAsync` fails with a recognized physical I/O or cancellation exception, or with an `ObjectDisposedException` thrown directly by that operation
- **THEN** the current captured transport follows the existing detach/reconnect path, while a stale captured transport is ignored

#### Scenario: Caller-cancelled write

- **WHEN** the caller cancellation token cancels a write or its captured physical flush
- **THEN** the write preserves normal caller-cancellation semantics without changing Raido connection state

#### Scenario: Keep-alive failure provenance

- **WHEN** ping message generation fails, or a captured physical ping write fails
- **THEN** generation follows the terminal path, while only the captured physical write can detach/reconnect and stale physical failures are ignored

### Requirement: Fresh protocol readers

Raido SHALL create a new protocol reader for each published physical transport and SHALL never request transport input while no physical transport is published.

#### Scenario: Replacement reader

- **WHEN** a replacement transport is published after the previous transport ends
- **THEN** dispatch resumes with a fresh reader over the replacement transport

#### Scenario: Physical detach wakes dispatch

- **WHEN** the current physical transport detaches while dispatch is waiting for input
- **THEN** the physical read is woken, the per-read client-timeout state is cleared, the reconnect waiter can be awaited, and the terminal connection-aborted token remains uncancelled

#### Scenario: Handler observes a detached window

- **WHEN** dispatch begins while the physical transport is detached and an active reconnect window exists
- **THEN** the handler waits for that window and does not request transport input until a replacement is published

#### Scenario: Timeout terminalization releases the timeout lock

- **WHEN** client-timeout detection observes an expired read timeout while a physical close callback is running concurrently
- **THEN** timeout state is inspected under the timeout lock, that lock is released before terminalization, and registration disposal and physical cancellation complete without a timeout-lock/reconnect-lock deadlock

### Requirement: Physical callback rebinding

Keep-alive and lifetime-notification callbacks SHALL be registered against each captured physical transport's features, including close-request notification when available. A successful replacement SHALL receive the same applicable physical registrations.

#### Scenario: Replacement callback registration

- **WHEN** a replacement transport wins publication
- **THEN** its close, close-request, keep-alive, and active client-timeout callbacks use the replacement's physical features

#### Scenario: Initial callback registration preserves synchronous transitions

- **WHEN** the initial physical connection has a pre-signalled `ConnectionClosed` or `ConnectionClosedRequested` token while callbacks are being registered
- **THEN** a pre-signalled close leaves an opted-in connection detached with its reconnect window active, while a pre-signalled close request terminalizes it; local registrations are published only if the same connection remains current and non-terminal

#### Scenario: Reconnect cycles

- **WHEN** a successfully rebound transport later disconnects
- **THEN** a new reconnect waiter is created for that later disconnect; a timed-out reconnect window does not create another window
