## Purpose

Provides separate stable TCP and logical Hub connection contexts so physical transport replacement does not create a second logical Hub lifetime.

## ADDED Requirements

### Requirement: Stable TCP and logical Hub contexts are separate

The system SHALL represent one logical Raido Hub connection with one stable lower TCP context and one Hub context. Replacing the physical transport SHALL preserve the logical connection identity and Hub lifecycle.

#### Scenario: Physical replacement preserves the logical connection

- **WHEN** the current physical connection closes during an enabled reconnect window and a replacement is accepted
- **THEN** the same logical connection ID, stable feature collection, item collection, caller context, and Hub context continue after replacement
- **AND** Hub connected and disconnected lifecycle callbacks are not repeated

#### Scenario: Physical disconnect remains recoverable

- **WHEN** the current physical transport disappears while the reconnect window remains valid
- **THEN** the stable TCP and Hub contexts remain available to the existing reconnect waiter
- **AND** the logical connection is not terminal solely because that physical transport closed

### Requirement: Stateful reconnect remains the existing authority

The system SHALL preserve the existing stateful reconnect behavior for reconnect windows, timeout, terminal transitions, waiter completion, physical transport failure handling, stale physical transport protection, and single-winner replacement publication.

#### Scenario: Replacement publication is serialized

- **WHEN** multiple physical replacements race for one detached connection
- **THEN** at most one replacement is published
- **AND** the existing reconnect waiter completes only after the winning replacement is ready

#### Scenario: Reconnect expiry remains terminal

- **WHEN** the reconnect window expires before a replacement is published
- **THEN** the stable connection follows the existing terminal behavior
- **AND** later replacements are rejected

#### Scenario: Stable connection capabilities follow physical lifecycle

- **WHEN** the stable TCP context is constructed
- **THEN** it has not been activated, is not terminal, and has no current physical connection
- **WHEN** a physical transport is activated
- **THEN** it is active while that physical connection is current
- **WHEN** the physical transport detaches during the reconnect window
- **THEN** it is no longer active while the reconnect waiter remains available
- **AND** terminal abort, cleanup, or reconnect expiry makes the context terminal

### Requirement: The lower transport boundary is stable

The system SHALL expose one stable `Transport` pipe pair from the TCP context for the entire logical lifetime. Physical transport execution SHALL relay bytes through the TCP context's internal `Application` pipe pair, and replacing the physical transport SHALL NOT replace the stable `Transport` instance.

#### Scenario: Data flow continues across physical replacement

- **WHEN** the initial physical transport supplies input and the Hub writes output
- **THEN** input reaches the stable transport reader and output reaches the initial physical transport
- **WHEN** the initial physical transport is replaced
- **THEN** input reaches the same stable transport reader and output reaches the replacement physical transport

#### Scenario: Incomplete input does not cross a physical replacement boundary

- **WHEN** the initial physical transport supplies only part of a protocol message and then detaches
- **AND** a replacement physical transport is accepted
- **THEN** the incomplete bytes from the initial transport are discarded before replacement input is parsed
- **AND** a protocol message is never assembled from bytes belonging to both physical transports
- **AND** a complete message supplied entirely by the replacement transport remains dispatchable

#### Scenario: Detached output is not replayed

- **WHEN** the stable logical connection has no attached physical transport and application output is produced
- **THEN** the lower transport execution consumes that output without retaining it for a later physical replacement
- **AND** no replay or acknowledgement mechanism is introduced

#### Scenario: Detached output behind an in-flight physical flush is dropped

- **WHEN** output is being flushed to a physical transport and that transport detaches
- **AND** additional output is produced before a replacement is published
- **THEN** the lower transport consumes and drops the additional output
- **AND** it is not written to the replacement transport
- **WHEN** output is produced after the replacement is published
- **THEN** it is written to the replacement transport

#### Scenario: Stable output admission commits before a physical boundary changes

- **WHEN** a Hub write is admitted while a physical transport is current
- **THEN** the active check, serialization, and stable-pipe flush invocation occur as one lower admission operation
- **AND** caller cancellation before admission does not advance bytes
- **AND** caller cancellation after admission cannot leave advanced bytes for a later write to commit
- **AND** an admitted write cannot first become visible through a replacement physical transport

#### Scenario: Physical input commit is not canceled by socket closure

- **WHEN** bytes have been copied and advanced into the stable input pipe from a physical transport
- **AND** that physical transport closes before its next read
- **THEN** the stable input commit is completed without the physical close token canceling it
- **AND** the bytes cannot remain unflushed until replacement input is copied

#### Scenario: Input and output relay faults are observed during cleanup

- **WHEN** terminal cleanup cancels the stable transport relays
- **THEN** cleanup awaits both relay tasks after cancellation
- **AND** relay task completion and faults are observed
- **AND** relay faults do not remain unobserved

#### Scenario: Stable pipe producers are quiesced before completion

- **WHEN** terminal cleanup begins while a Hub writer or physical-input relay is active
- **THEN** terminal signalling prevents new admissions and wakes pending operations without completing a producer-owned writer concurrently
- **AND** cleanup awaits the active relays and the Hub write owner
- **AND** each producer-owned stable writer is completed exactly once after its owner has quiesced

### Requirement: Stable physical heartbeat integration is identity-safe

The system SHALL keep heartbeat handlers on the stable TCP context. A physical heartbeat SHALL tick those handlers only while its physical connection remains the active published transport.

#### Scenario: Stale or losing physical heartbeat is ignored

- **WHEN** a physical connection is replaced or loses the activation race
- **AND** its registered heartbeat callback fires later
- **THEN** it does not invoke stable heartbeat handlers
- **WHEN** the active replacement heartbeat fires
- **THEN** it invokes the stable heartbeat handlers

#### Scenario: Replacement heartbeat waits for the previous input boundary

- **WHEN** a replacement is published while the previous physical input boundary is awaiting acknowledgement
- **AND** the replacement heartbeat fires
- **THEN** no stable Hub heartbeat handler is invoked for that tick
- **AND** the replacement cannot inherit the detached transport's client-timeout state through that heartbeat
- **WHEN** the previous input boundary is acknowledged
- **THEN** later replacement heartbeats invoke the stable handlers normally

### Requirement: Stable infrastructure features remain stable

The system SHALL provide connection-owned infrastructure features from the stable TCP context and SHALL preserve legitimate custom/application features from the initial physical context.

#### Scenario: Stable features do not alias physical infrastructure

- **WHEN** a physical transport is activated and later replaced
- **THEN** stable ID, items, transport, lifetime, and heartbeat features remain owned by the TCP context
- **AND** the stable items feature exposes the same collection as the stable `Items` property
- **AND** initial custom/application features remain available
- **AND** replacement physical infrastructure features do not become authoritative

### Requirement: Hub state remains above the TCP transport

The system SHALL keep the Hub protocol, message-writing coordination, Hub timeout policy, caller state, and logical lifecycle state on the Hub context. Physical transport replacement SHALL NOT implicitly change the Hub protocol or expose raw pipes through the public Hub-facing API.

#### Scenario: TCP replacement does not change protocol

- **WHEN** the lower TCP context publishes a replacement physical transport
- **THEN** the Hub context continues using its existing protocol unless Hub-level code explicitly assigns another protocol

#### Scenario: Public caller state remains logical

- **WHEN** application code accesses the caller context
- **THEN** it can access logical connection state and coordinated Hub operations
- **AND** it cannot access a TCP context, raw physical connection, or transport ownership operation

### Requirement: Logical lifecycle occurs once

The system SHALL register and remove the logical Hub context once for its entire lifetime, including physical reconnect.

#### Scenario: Reconnect does not duplicate lifecycle callbacks

- **WHEN** a logical connection loses and regains its physical transport
- **THEN** the lifetime manager and Hub dispatcher observe one connected lifecycle and one eventual disconnected lifecycle
- **AND** the connection store does not remove and re-add the logical context during replacement

### Requirement: Stable keepalive follows connection activity

The system SHALL send and account for keepalive pings only while the stable TCP context reports `IsActive`.

#### Scenario: Detached and terminal keepalive is not accounted as sent

- **WHEN** the stable TCP context is detached or terminal
- **AND** a keepalive tick occurs
- **THEN** no ping is recorded as sent
- **AND** the stable last-send timestamp is not advanced

### Requirement: Physical failure is retained through reconnect expiry

The system SHALL preserve the original physical transport exception across a reconnect window until a replacement succeeds or the window expires.

#### Scenario: Replacement clears a pending physical failure

- **WHEN** a physical transport fails and a replacement is successfully published
- **THEN** the pending physical failure is cleared

#### Scenario: Reconnect expiry promotes the original physical failure

- **WHEN** a physical transport fails and no replacement is published before reconnect expiry
- **THEN** the stable terminal exception is the original physical transport failure
