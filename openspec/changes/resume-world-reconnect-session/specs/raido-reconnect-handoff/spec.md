# Raido reconnect integration boundary

## ADDED Requirements

### Requirement: GameWorld uses the existing Raido reconnect lifecycle

GameWorld MUST resolve the existing logical Raido connection and return it to
Raido connection infrastructure. The connection dispatcher MUST delegate to
Raido's existing internal `TryAttachPhysicalConnection` operation. GameWorld
MUST NOT call physical attachment on `RaidoHubConnectionContext`. It MUST NOT create a temporary
candidate logical context, transfer a physical connection between logical
contexts, add a second reconnect registry or state machine, or add
response-aware transport writes. Existing #477/#488 Raido reconnect timing,
attach locking, and single-winner behavior remain authoritative. No new
high-level reconnect API is added to Raido. `RaidoHubConnectionContext` MUST
NOT expose public physical transport attachment, and
`RaidoTcpConnectionContext` attachment MUST remain internal. The infrastructure
operation MUST remain application-neutral.

#### Scenario: Concurrent reconnects have one existing-lifecycle winner

- GIVEN two valid raw reconnect requests for one detached logical connection
- WHEN both pass GameWorld validation
- THEN the existing session claim and Raido reconnect window allow at most one
  raw connection to attach
- AND a rejected request cannot alter the target logical connection or session

### Requirement: Existing logical identity is preserved

The target MUST retain its stable logical connection ID, features, items,
handlers, and GameWorld state. The replacement physical connection ID MUST NOT
be rewritten as the logical ID.

#### Scenario: Raw reconnect attaches to the target

- GIVEN an exact existing GameWorld session and detached Raido logical target
- WHEN the raw replacement connection is accepted
- THEN the target logical connection resumes with the same identity
- AND no candidate logical connection exists

### Requirement: GameWorld completes the reconnect handshake before attach

GameWorld MUST install the fresh client protocol and flush response 15 before
returning the existing logical connection. Raido MUST receive the replacement
transport only after GameWorld has completed that response.

#### Scenario: Immediate game input is buffered before attach

- GIVEN response 15 has been flushed on the raw replacement transport
- WHEN the client writes its first fresh-ISAAC packet before physical attach
- THEN the packet remains in the raw input pipe
- AND the existing Raido reader decodes it once after attach using the fresh protocol
