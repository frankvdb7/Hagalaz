# World reconnect session

## ADDED Requirements

### Requirement: Reconnect authentication proves the existing identity

The system MUST validate the submitted password before reconnect ownership is
considered and MUST return the resulting authenticated subject only when the
existing authentication is valid. Reconnect-only validation MUST use a
dedicated request/response contract, MUST NOT mint a replacement token, and
MUST NOT install candidate GameWorld features. The normal sign-in request MUST
remain a token-issuing contract without a reconnect boolean.

#### Scenario: Valid existing authentication returns the subject

- GIVEN valid credentials for an already-authenticated account
- WHEN reconnect authentication runs
- THEN it returns that password-validated account subject
- AND it allocates no GameWorld session or character state

#### Scenario: Invalid credentials cannot adopt another account

- GIVEN a reconnect candidate with an invalid password or another account's
  credentials
- WHEN reconnect authentication runs
- THEN it returns failure before session adoption
- AND the existing target session remains unchanged

#### Scenario: Normal sign-in contract remains token issuing

- GIVEN a normal sign-in request
- WHEN the authorization consumer handles it
- THEN it preserves the existing token response behavior
- AND reconnect-only validation is not selected by a request flag

### Requirement: Reconnect reuses exact GameWorld state

The system MUST resolve the existing world session by authenticated master ID,
verify its stable connection ID and character ownership, and reuse the same
logical connection, session claim, character reference, and world registration.

#### Scenario: Matching session resumes

- GIVEN an authenticated master ID with an existing world session and logical
  Raido connection inside its reconnect window
- WHEN the reconnect request is accepted
- THEN only the physical transport and reconnect protocol state change
- AND fresh-login allocation, hydration, registration, and world sign-in
  publication do not run

The stable logical connection ID MUST remain distinct from the replacement
physical connection ID. Reconnect MUST reuse the existing session claim,
character reference, registration, and handlers.

#### Scenario: Missing, stale, duplicate, or lost-claim target fails safely

- GIVEN no exact session, no matching logical connection, an expired or
  already-won target, or a session whose claim is no longer owned
- WHEN the candidate reconnects
- THEN the candidate is rejected
- AND an existing winner and its GameWorld state are not disturbed

### Requirement: Fresh login remains unchanged

Opcode 16 with reconnect flag 0 MUST continue through the existing fresh world
sign-in path with response 2. Lobby authentication MUST remain
non-reconnectable.

#### Scenario: Fresh world sign-in keeps its current behavior

- GIVEN a world request with reconnect flag 0
- WHEN the request is handled
- THEN normal session creation, character hydration, registration, and
  response-2 behavior remain in use

Stateful reconnect MUST be enabled before a successful fresh world-login
response is externally committed. Lobby and failed-login paths MUST NOT enable
it.

### Requirement: World reconnect request is handled distinctly

The existing world authentication decoder MUST route opcode 16 with reconnect
flag 1 to reconnect handling and MUST NOT silently route it through fresh world
sign-in.

#### Scenario: Reconnect flag selects reconnect handling

- GIVEN opcode 16 with reconnect flag 1
- WHEN the handshake decoder dispatches the request
- THEN it produces `WorldReconnectRequest`
- AND the request does not invoke fresh world sign-in
