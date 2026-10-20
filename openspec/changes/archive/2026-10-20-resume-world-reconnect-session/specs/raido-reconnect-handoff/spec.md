# Raido reconnect integration boundary

## ADDED Requirements

### Requirement: Dispatcher owns physical lifecycle and attachment
Raido MUST own accepted transports via a nonpublic dispatcher, one application scope, and an internal context exposing only logical new/existing operations. Reconnect preflights under lock, prepares GameWorld, then attaches internally. The API hides physical/reconnect state; preparation failure aborts candidate, attach failure terminalizes target and candidate. No public reconnect query, DTO, marker, Items coordination, reservation, lease, or second state machine; infrastructure stays neutral.

#### Scenario: Concurrent reconnects have one existing-lifecycle winner

- GIVEN two valid raw reconnect requests for one detached logical connection
- WHEN both pass GameWorld validation
- THEN the existing session claim and Raido preflight allow at most one raw
  connection to prepare and attach
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

### Requirement: GameWorld preparation completes before attach

GameWorld MUST install the fresh client protocol and flush response 15 while
the replacement remains raw. Raido MUST perform the internal single attach only
after that preparation completes, and the enclosing GameSession claim MUST
remain held until the attach returns.

#### Scenario: Immediate game input is buffered before attach

- GIVEN response 15 has been flushed on the raw replacement transport
- WHEN the client writes its first fresh-ISAAC packet before physical attach
- THEN the packet remains in the raw input pipe
- AND the existing Raido reader decodes it once after attach using the fresh
  protocol
