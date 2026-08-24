## ADDED Requirements

### Requirement: Valid world reconnect resumes the existing session

GameWorld MUST accept a valid revision-742 opcode-18 reconnect during the Raido grace window only when the handshake authenticates the owner of an existing local world session whose logical connection is reconnecting.

#### Scenario: Successful reconnect

- **WHEN** a valid opcode-18 request authenticates the owner of a retained reconnecting world session
- **THEN** GameWorld rebinds the replacement transport through Raido, preserves the existing `GameSession` and exact `ICharacter` instance, installs fresh protocol/encryption state, and returns the normal world sign-in success response as the first replacement-transport packet

#### Scenario: Invalid ownership

- **WHEN** the request credentials do not identify the owner of the retained world session, or no matching reconnecting session exists
- **THEN** the replacement is rejected and cannot attach to another logical connection

### Requirement: Reconnect does not repeat world creation

GameWorld MUST NOT hydrate or register a new character/session for a successful reconnect.

#### Scenario: Existing runtime state survives

- **WHEN** a reconnect succeeds
- **THEN** the existing character store entry, region membership, contacts/world-session ownership, and persistence revision remain the authoritative instances

### Requirement: Reconnect restores authoritative current state

GameWorld MUST send only focused authoritative resynchronization using existing update APIs after successful rebind.

#### Scenario: Current view rebuild

- **WHEN** a reconnect succeeds
- **THEN** the success response is flushed first, the character receives a forced map/viewport rebuild and appearance refresh in the post-commit phase, and no bytes encoded with the old protocol/encryption state are replayed

#### Scenario: Invalidated reconnect

- **WHEN** the retained session expires, is aborted, or loses the prepare/commit race after the replacement handshake begins
- **THEN** the temporary replacement connection terminates and does not continue through ordinary handshake processing

### Requirement: Terminal cleanup remains the existing boundary

GameWorld MUST defer logout/persistence/session removal during transient physical loss and execute the existing cleanup pipeline once after terminal logical closure.

#### Scenario: Grace expiry

- **WHEN** no valid reconnect wins before grace expiry
- **THEN** the existing disconnect path performs character detach/removal, persistence/dehydration, Contacts/world-session cleanup, and applicable token/session revocation once
