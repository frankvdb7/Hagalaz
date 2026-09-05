# World reconnect session

## ADDED Requirements

### Requirement: Reconnect is classified before logical connection creation

The GameWorld connection delegate MUST process opcode 14 and send its existing
acknowledgement before reading the following authentication request. It MUST
then classify that request before creating a logical Raido context. Opcode 16
with reconnect flag 1 MUST go to the reconnect handler with the raw
`ConnectionContext`; it MUST NOT create a temporary candidate context or invoke
fresh world sign-in.

#### Scenario: Reconnect flag selects raw reconnect handling

- GIVEN a complete opcode-14 handshake followed by opcode 16 with reconnect
  flag 1
- WHEN the connection is accepted
- THEN opcode 14 is acknowledged
- THEN the decoder produces `WorldReconnectRequest`
- AND no logical candidate context is created
- AND the raw connection is validated against the existing target

### Requirement: Reconnect authentication proves the existing identity

Reconnect MUST use the dedicated existing-authentication request/response,
return only the validated subject and failure flags, and never mint a token or
install candidate GameWorld features. Normal sign-in MUST remain token issuing
without a reconnect mode flag.

#### Scenario: Valid and invalid credentials

- GIVEN credentials for an already-authenticated account
- WHEN reconnect authentication runs
- THEN only the matching existing master ID can be considered
- AND invalid credentials are rejected before session adoption

### Requirement: Raw reconnect authentication has no ambient Raido dependency

Reconnect authentication MUST be executable before a Raido caller context
exists. The raw physical remote address and connection ID required for
Authorization validation and rate limiting MUST be supplied explicitly by the
reconnect handler. It MUST NOT create or fake a caller context.

#### Scenario: Raw authentication uses explicit physical metadata

- GIVEN a reconnect request on a raw physical connection with a remote IP
- WHEN reconnect authentication runs before logical Raido creation
- THEN the Authorization validation request contains that remote IP
- AND rate limiting uses that IP or the explicit physical connection ID
- AND the validated subject maps to the existing master ID
- AND no fresh world session or character hydration is performed

### Requirement: Reconnect reuses exact GameWorld state

The handler MUST resolve the existing world session by authenticated master ID,
then re-resolve and verify its stable connection ID, expected session claim,
exact logical target, character reference, and authentication subject inside the
existing session-claim critical section. It MUST invoke Raido's existing
awaiting-reconnect preflight before GameWorld mutation and MUST complete the
internal physical attach before the claim action returns.

It MUST NOT hydrate, register, publish fresh-login messages, remove the existing
session on rejected raw-connection cleanup, or use `Items` for reconnect
coordination. The outer handshake cancellation token MUST remain in force
through the claim action, preparation, response flush, and final attach.

#### Scenario: Matching session resumes

- GIVEN an exact authenticated world session with a detached logical target in
  the existing Raido reconnect window
- WHEN reconnect succeeds
- THEN the same logical connection, GameSession, claim, character, and handlers
  resume
- AND only the physical transport, reconnect protocol, and client metadata are
  replaced

### Requirement: Handshake policy is injectable and request-specific

Handshake revision and system-update policy MUST be provided through
`IHandshakeValidator<TRequest>` for each request type. No static global
handshake policy class may own this decision.

#### Scenario: Reconnect validation can be substituted

- GIVEN an injected `IHandshakeValidator<WorldReconnectRequest>` returns
  `Outdated`
- WHEN a reconnect request is handled
- THEN `Outdated` is returned before reconnect authentication is called

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
candidate MUST pass authoritative session-claim revalidation and Raido's
awaiting-reconnect preflight before GameWorld preparation. The winner MUST
install the fresh protocol and metadata, flush response 15, and then perform the
existing single physical attach internally.

#### Scenario: Response precedes resumed input

- GIVEN a valid revision-742 reconnect
- WHEN the target accepts the raw connection
- THEN the fresh protocol is installed on the detached target
- AND response 15 is flushed on the raw handshake transport
- AND only then does the existing Raido single attach operation resume normal
  input
- AND subsequent game input is read by the existing target using the fresh
  reconnect protocol

#### Scenario: Active target is rejected before preparation

- GIVEN physical A is still active on the existing logical target
- AND physical B presents valid reconnect credentials and matching GameWorld
  identity
- WHEN B reaches Raido's existing-dispatch preflight
- THEN preparation is not invoked
- AND protocol, ISAAC, metadata, response 15, session, character, and physical A
  remain unchanged
- AND B is rejected

#### Scenario: Losing candidate cannot disturb the winner

- GIVEN two valid candidates target the same detached logical connection
- WHEN the first candidate enters the existing session claim and completes the
  continuation through physical attach
- THEN the other candidate is rejected by Raido preflight before preparation
- AND it receives no response 15
- AND the winner protocol, metadata, session, character, and physical transport
  remain unchanged

### Requirement: Attach failure after target mutation is terminal

If the existing single Raido attach operation fails after the target protocol,
metadata, and response 15 have been committed, the reconnect continuation MUST
abort the raw replacement and terminate the partially transitioned logical
target. It MUST NOT attempt protocol rollback.

#### Scenario: Final attach loses the narrow post-response race

- GIVEN authoritative revalidation succeeds and response 15 is flushed
- WHEN the existing single attach operation rejects the replacement
- THEN the replacement is aborted
- AND the logical target is terminated
- AND no second session, character, marker, or reconnect transition is created

### Requirement: Preparation failure follows the mutation boundary

If preparation is canceled or fails before `SetProtocolAsync` commits the fresh
protocol, the logical target MUST remain non-terminal with its old protocol and
metadata intact. The replacement physical connection MUST be aborted and the
incoming protocol scope MUST be disposed exactly once according to the existing
`SetProtocolAsync` ownership contract. If preparation fails after the protocol
transition commits, GameWorld MUST terminalize the target without attempting
protocol rollback.

#### Scenario: Cancellation before protocol mutation

- GIVEN a valid reconnect whose preparation cancellation occurs before the
  incoming protocol is accepted
- WHEN the reconnect continuation fails
- THEN the target remains eligible for its existing reconnect lifecycle
- AND no response 15 is sent
- AND the replacement is aborted

#### Scenario: Response flush fails after protocol mutation

- GIVEN `SetProtocolAsync` succeeds and GameWorld metadata is updated
- WHEN response 15 flushing fails or is canceled
- THEN the target is terminalized
- AND the replacement is aborted
- AND the fresh protocol is not rolled back onto a reconnectable target
