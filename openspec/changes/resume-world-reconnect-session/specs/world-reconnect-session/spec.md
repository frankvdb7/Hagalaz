# World reconnect session

## ADDED Requirements

### Requirement: Reconnect is classified before logical connection creation

The GameWorld connection delegate MUST process opcode 14 and send its existing
acknowledgement before reading the following authentication request. It MUST
then classify that request before creating a logical Raido context. The raw
classification MUST inspect opcode 19 without full authentication decoding and
MUST inspect opcode 16 only far enough to validate its packet framing/header
and read the reconnect flag. Opcode 16 with flag 0 and opcode 19 MUST retain
their complete authentication bytes for the normal logical reader. Opcode 16
with reconnect flag 1 MUST then invoke the full world decoder exactly once and
go to the reconnect handler with the raw `ConnectionContext`; it MUST NOT
create a temporary candidate context or invoke fresh world sign-in.

#### Scenario: Reconnect flag selects raw reconnect handling

- GIVEN a complete opcode-14 handshake followed by opcode 16 with reconnect
  flag 1
- WHEN the connection is accepted
- THEN opcode 14 is acknowledged
- THEN the decoder produces `WorldReconnectRequest`
- AND no logical candidate context is created
- AND the raw connection is validated against the existing target

#### Scenario: Fresh and lobby classification avoids duplicate raw decoding

- GIVEN a complete opcode-14 handshake followed by a flag-0 opcode-16 request
  or an opcode-19 request
- WHEN the connection is accepted
- THEN the raw handler does not invoke the full world or lobby authentication
  decoder before logical connection creation
- AND the complete authentication bytes remain available to the logical reader
- AND flag-0 world uses stateful reconnect while lobby does not

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

### Requirement: Handshake policy is shared and injectable

Handshake revision and system-update policy MUST be provided through one
injectable policy shared by lobby, fresh-world, and reconnect sign-in. No
static global policy or request-specific validator hierarchy may own this
decision.

#### Scenario: Reconnect validation can be substituted

- GIVEN the injected shared handshake policy returns `Outdated` for a reconnect
  request
- WHEN a reconnect request is handled
- THEN `Outdated` is returned before reconnect authentication is called

### Requirement: Existing-token infrastructure failures remain faults

The existing-authentication consumer MUST preserve ordinary unsuccessful
validation results for invalid credentials, disabled accounts, locked accounts,
and absence of a valid existing token. Unexpected failures while looking up
existing tokens MUST propagate as request faults rather than being converted to
an ordinary unsuccessful response. Cancellation MUST propagate, and internal
authorization request calls MUST receive the consume cancellation token.

#### Scenario: Token lookup infrastructure failure

- GIVEN valid password credentials
- WHEN existing-token lookup throws an infrastructure exception
- THEN the consumer faults instead of returning an ordinary failed
  authentication response
- AND the password-grant and token-lookup requests receive the consume
  cancellation token

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
existing single physical attach internally. A response flush is successful only
when it is neither canceled nor completed; a completed writer MUST be treated as
failed response delivery and MUST prevent final attach.

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
protocol rollback. `SetProtocolAsync` commits the new protocol before disposing
the previous protocol lifetime, so a cleanup exception after that commit MUST be
treated as a committed mutation by checking the target's current protocol.

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

#### Scenario: Previous protocol cleanup fails after commit

- GIVEN `SetProtocolAsync` installs the fresh protocol and transfers its lifetime
- WHEN disposal of the previous protocol lifetime throws
- THEN the fresh protocol remains installed
- AND the target is terminalized
- AND the incoming protocol lifetime is disposed exactly once by target cleanup

### Requirement: Expected handshake cancellation is quiet

If the outer handshake or reconnect cancellation token is canceled, the handler
MUST release the session claim and return without logging `ReconnectFailed` or
causing the dispatcher to log an application failure. An unrelated
`OperationCanceledException` MUST still be surfaced and logged when that token is
not canceled.

#### Scenario: Handshake timeout cancels a blocked response flush

- GIVEN a reconnect response flush is waiting on the outer handshake token
- WHEN that token is canceled while the physical connection remains open
- THEN the claim callback exits
- AND the replacement is aborted
- AND no reconnect or dispatcher application failure is logged
