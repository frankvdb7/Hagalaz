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

#### Scenario: Stable connection status follows physical lifecycle

- **WHEN** the stable TCP context is constructed
- **THEN** its status is `Inactive`
- **WHEN** a physical transport is activated
- **THEN** its status is `Active`
- **WHEN** the physical transport detaches during the reconnect window
- **THEN** its status is `Inactive` until a replacement is published
- **AND** terminal abort, cleanup, or reconnect expiry changes its status to `Disposed`

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
