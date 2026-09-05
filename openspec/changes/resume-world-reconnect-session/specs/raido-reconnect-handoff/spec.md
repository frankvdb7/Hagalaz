# Raido reconnect integration boundary

## ADDED Requirements

### Requirement: Dispatcher owns physical lifecycle and attachment

`RaidoConnectionDispatcher` MUST own each accepted physical connection, create
one application scope, and invoke a scoped application delegate with a
per-connection dispatch context. The context MUST expose only high-level logical
operations for new and existing connections; its constructor MUST be internal
and it MUST NOT publicly expose the physical `ConnectionContext`, physical
attachment, reconnect state, or transport state.

The existing-dispatch operation MUST first derive an awaiting-reconnect
preflight from the existing Raido TCP state under its existing state lock, then
invoke GameWorld preparation, and finally delegate to the existing internal
`TryAttachPhysicalConnection` operation. GameWorld MUST NOT call physical
attachment on `RaidoHubConnectionContext` and MUST NOT receive a public
reconnect-state query.

There MUST be no connection-selection result DTO, GameWorld reconnect marker,
`Items`-based coordination, reservation, lease, or second reconnect state
machine. The infrastructure operation MUST remain application-neutral.

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
