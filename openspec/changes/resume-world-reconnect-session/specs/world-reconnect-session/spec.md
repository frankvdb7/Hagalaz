# World reconnect session

## ADDED Requirements

### Requirement: Reconnect is classified before logical connection creation

The GameWorld connection handler MUST process opcode 14 and send its existing
acknowledgement before reading the following authentication request. It MUST
then classify that request before creating a logical Raido context. Opcode 16
with reconnect flag 1 MUST go to the reconnect handler with the raw
`ConnectionContext`; it MUST NOT create a temporary candidate context or
invoke fresh world sign-in.

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
install candidate GameWorld features. The normal sign-in contract MUST remain
token issuing without a reconnect mode flag.

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
exact logical target, character reference, authentication subject, and detached
reconnectable state inside the existing session-claim critical section before
mutating the target or asking Raido connection infrastructure to activate the
raw connection on that existing logical target.
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

### Requirement: Handshake policy is injectable and request-specific

Handshake revision and system-update policy MUST be provided through
`IHandshakeValidator<TRequest>` for each request type. The default validator
MAY be registered as an open generic, and a closed request-specific
registration MUST be able to replace it without changing the callers. No
static global handshake policy class may own this decision.

#### Scenario: Reconnect validation can be substituted

- GIVEN an injected `IHandshakeValidator<WorldReconnectRequest>` returns
  `Outdated`
- WHEN a reconnect request is handled
- THEN `Outdated` is returned before reconnect authentication is called

### Requirement: Reconnect failure and target ownership policy stay local

Reconnect authentication-result mapping MUST remain outside the handshake
validator. Session, claim, character, and authenticated-subject matching MUST
remain reconnect target checks in the reconnect handler unless a second
concrete consumer justifies a separate abstraction.

#### Scenario: Authentication failure is mapped by the reconnect handler

- GIVEN reconnect authentication reports invalid credentials
- WHEN the reconnect handler creates its failure response
- THEN the validator remains responsible only for handshake policy
- AND the reconnect handler returns `CredentialsInvalid`

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
candidate MUST pass authoritative revalidation inside the existing session
claim before it mutates the existing target. A losing or stale candidate MUST
NOT mutate the target protocol, ISAAC state, protocol lifetime, character
metadata, or transport. The winner MUST install the fresh revision-specific
protocol and reconnect client metadata, flush response 15, and then perform
the existing single Raido physical attachment transition.

#### Scenario: Response precedes resumed input

- GIVEN a valid revision-742 reconnect
- WHEN the target accepts the raw connection
- THEN the candidate passes the session-claim revalidation for the existing detached reconnect target
- THEN the fresh protocol is installed on the detached target
- AND response 15 is flushed on the raw handshake transport
- AND only then does the existing Raido single attach operation resume normal input
- AND subsequent game input is read by the existing target using the fresh
  reconnect protocol

#### Scenario: Losing candidate cannot disturb the winner

- GIVEN two valid candidates target the same detached logical connection
- WHEN the first candidate enters the existing session claim and completes reconnect
- THEN the other candidate receives no response 15
- AND its protocol scope is disposed locally
- AND the winner protocol, ISAAC state, metadata, lifetime, session,
  character, and physical transport remain unchanged

#### Scenario: Stale candidate after success is rejected

- GIVEN a candidate has resumed the existing logical connection
- WHEN a stale duplicate reconnect is handled
- THEN it is rejected without response 15
- AND the resumed target remains attached to the original winner

### Requirement: Attach failure after target mutation is terminal

If the existing single Raido attach operation fails after the target protocol,
metadata, and response 15 have been committed, the reconnect handler MUST abort
the raw replacement and terminate the partially transitioned logical target.
It MUST NOT leave that target reconnectable with the candidate protocol and
metadata, and it MUST NOT attempt protocol rollback.

#### Scenario: Final attach loses the narrow post-response race

- GIVEN authoritative revalidation succeeds and response 15 is flushed
- WHEN the existing single attach operation rejects the replacement
- THEN the replacement is aborted
- AND the logical target is terminated
- AND no second session, character, or reconnect transition is created
