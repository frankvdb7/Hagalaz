# World reconnect session

## ADDED Requirements

### Requirement: Reconnect is classified before logical connection creation

The GameWorld connection handler MUST parse the first raw handshake before
creating a logical Raido context. Opcode 16 with reconnect flag 1 MUST go to
the reconnect handler with the raw `ConnectionContext`; it MUST NOT create a
temporary candidate context or invoke fresh world sign-in.

#### Scenario: Reconnect flag selects raw reconnect handling

- GIVEN a complete opcode-16 handshake with reconnect flag 1
- WHEN the connection is accepted
- THEN the decoder produces `WorldReconnectRequest`
- AND no logical candidate context is created
- AND the raw connection is validated against the existing target

### Requirement: Reconnect authentication proves the existing identity

Reconnect MUST use the dedicated existing-authentication request/response,
return only the validated subject and failure flags, and never mint a token or
install candidate GameWorld features. The normal sign-in contract MUST remain
token issuing without a reconnect mode flag.

#### Scenario: Valid and invalid credentials

- GIVEN credentials for an already-authenticated account
- WHEN reconnect authentication runs
- THEN only the matching existing master ID can be considered
- AND invalid credentials are rejected before session adoption

### Requirement: Reconnect reuses exact GameWorld state

The handler MUST resolve the existing world session by authenticated master ID,
verify its stable connection ID, session claim, character reference, and
authentication subject, and attach directly to that existing logical target.
It MUST NOT hydrate, register, publish fresh-login messages, or remove the
existing session on rejected raw-connection cleanup.

#### Scenario: Matching session resumes

- GIVEN an exact authenticated world session with a detached logical target in
  the existing Raido reconnect window
- WHEN reconnect succeeds
- THEN the same logical connection, GameSession, claim, character, and
  handlers resume
- AND only the physical transport, reconnect protocol, and client metadata are
  replaced

### Requirement: Fresh login and lobby remain unchanged

Opcode 16 with reconnect flag 0 MUST use the normal fresh world sign-in path,
including response 2 and stateful reconnect enabled at logical context
creation. Lobby authentication MUST remain non-reconnectable.

#### Scenario: Fresh and lobby classification

- GIVEN a flag-0 world request or a lobby request
- WHEN the raw message is classified
- THEN the handshake bytes are retained for the normal logical handler
- AND fresh world uses the existing stateful factory option while lobby does
  not

### Requirement: Reconnect response and protocol ordering are preserved

Successful reconnect MUST send response 15 as plain handshake framing with a
two-byte payload length and exactly the 4,608-byte player-entry payload. The
existing target MUST have the fresh revision-specific protocol seeded from the
reconnect request before the client can send its first game packet.

#### Scenario: Response precedes resumed input

- GIVEN a valid revision-742 reconnect
- WHEN the target accepts the raw connection
- THEN response 15 is flushed on the raw handshake transport
- AND subsequent game input is read by the existing target using the fresh
  reconnect protocol
