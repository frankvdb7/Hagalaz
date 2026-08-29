## Purpose

Provides the evidence-backed revision-742 world reconnect flow while preserving the existing authenticated GameWorld session and character.

## ADDED Requirements

### Requirement: World reconnect intent uses the characterized opcode-16 flag

The system MUST distinguish fresh world sign-in from world reconnect using the characterized reconnect flag in the opcode-16 world handshake. Flag 0 MUST produce fresh world sign-in behavior, and flag 1 MUST produce reconnect behavior. Wire opcode 18 MUST NOT be treated as the active reconnect contract.

#### Scenario: Fresh world sign-in remains fresh

- **GIVEN** a valid revision-742 world sign-in packet with opcode 16 and reconnect flag 0
- **WHEN** the handshake is dispatched
- **THEN** the server handles it as a `WorldSignInRequest`
- **AND** the existing fresh sign-in behavior remains unchanged

#### Scenario: Reconnect intent is preserved

- **GIVEN** a valid characterized revision-742 reconnect packet with opcode 16 and reconnect flag 1
- **WHEN** the handshake is dispatched
- **THEN** the server handles it as a `WorldReconnectRequest`
- **AND** it does not dispatch the packet as a fresh world sign-in

#### Scenario: Wire opcode 18 is not accepted as the active reconnect contract

- **GIVEN** an input packet beginning with wire opcode 18
- **WHEN** the GameWorld handshake dispatcher evaluates it
- **THEN** it does not route the packet to fresh sign-in or the characterized reconnect flow
- **AND** it is rejected or remains unsupported without creating a session or changing ownership

### Requirement: Reconnect response and protocol state follow characterization

The system MUST implement response code 15 and the reconnect world-entry payload using the checked-in characterization of the revision-742 client. The characterized flow includes authentication reset, protocol preservation, new client and server ISAAC instances, temporary-key clearing, the established RSA/XTEA boundaries, and the server-key `+50` transform. The system MUST NOT emit an uncharacterized field or cipher transition.

#### Scenario: Characterized reconnect response is emitted

- **GIVEN** an authenticated reconnect request using opcode 16 with reconnect flag 1
- **WHEN** reconnect succeeds
- **THEN** the server emits response code 15
- **AND** it emits the characterized `readEnterWorldPacket(true)` world-entry payload of 4,608 bytes
- **AND** it applies only the characterized protocol/cipher state transition

#### Scenario: Uncharacterized wire behavior is not introduced

- **GIVEN** a reconnect field, cipher transition, or payload detail is not represented by the characterization fixture
- **WHEN** reconnect implementation is evaluated
- **THEN** that behavior remains unimplemented
- **AND** no guessed field, key rule, or payload is emitted

### Requirement: Reconnect authentication has no fresh-sign-in side effects

Reconnect authentication MUST reuse the existing credential and identity validation rules without creating a new GameSession, hydrating or registering a character, replacing ownership, or publishing fresh-login commands. Temporary authentication tokens and features MUST have an explicit owner and cleanup path.

#### Scenario: Valid owner authenticates without creating a session

- **GIVEN** valid reconnect credentials identify the owner of a retained world session
- **WHEN** reconnect authentication succeeds
- **THEN** the existing session remains the authoritative session
- **AND** no new session or character is created
- **AND** no character hydration, registration, or fresh-login command runs

#### Scenario: Temporary authentication state is released

- **GIVEN** reconnect authentication creates temporary token or feature state
- **WHEN** authentication, handoff, or validation ends
- **THEN** the state is released, revoked, or transferred to its existing owner according to the established authentication contract
- **AND** no temporary token or feature remains attached to the temporary context without an owner

### Requirement: Successful reconnect reuses the existing world state

The system MUST resume the exact existing GameSession and ICharacter instance after validating authenticated ownership. It MUST not replace the distributed or local session owner.

#### Scenario: Existing session and character are resumed

- **GIVEN** the authenticated master ID resolves to an existing retained world session
- **AND** that session owns an existing character instance
- **WHEN** reconnect completes
- **THEN** the same GameSession instance remains active
- **AND** the same ICharacter instance remains registered
- **AND** the old logical connection remains the owner

#### Scenario: Reconnect identity does not match the retained owner

- **GIVEN** reconnect credentials do not identify the master ID of the retained world session
- **WHEN** reconnect validation runs
- **THEN** the reconnect is rejected
- **AND** the retained session and character remain unchanged

### Requirement: Reconnect ordering follows the characterized sequence

The system MUST preserve the characterized ordering between opcode-16 reconnect-flag handling, response 15, protocol/cipher transition, physical handoff, reconnect world-entry resynchronization, and resumed reads and writes. The implementation MUST NOT assume that response 15 occurs before or after physical adoption; the checked-in fixture and transport tests MUST define that boundary.

#### Scenario: Reconnect follows the captured ordering

- **GIVEN** the characterization fixture records the complete reconnect event sequence
- **WHEN** the server accepts the reconnect
- **THEN** each event occurs in the same order as the fixture
- **AND** normal logical reads and writes resume only at the characterized boundary

### Requirement: Reconnect resynchronization is limited to characterized authoritative state

After successful handoff, the system MUST send only the authoritative world-entry and resynchronization payloads proven necessary by the revision-742 characterization. It MUST NOT assume map, appearance, widget, container, or other state is required without evidence and MUST NOT introduce a generic snapshot or replay mechanism.

#### Scenario: Only characterized resynchronization is sent

- **GIVEN** the reconnect world-entry payload is characterized as the required resynchronization
- **WHEN** the existing session resumes
- **THEN** the server sends that characterized payload through existing GameWorld update or rebuild operations
- **AND** no additional unproven state is resent
