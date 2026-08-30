# World reconnect session

## ADDED Requirements

### Requirement: Reconnect credentials use existing validation

The system MUST validate reconnect credentials through the existing
`PasswordGrantCommand` behavior and expose the authenticated master ID without
issuing or inspecting an OpenIddict token.

#### Scenario: Valid credentials return the subject

- GIVEN a reconnect request with valid credentials
- WHEN tokenless authorization validates them
- THEN the response contains the authenticated subject/master ID
- AND no token is created or looked up

#### Scenario: Existing validation outcomes are preserved

- GIVEN credentials that produce an existing `PasswordGrantCommand` outcome
- WHEN the reconnect authorization request is consumed
- THEN that outcome is mapped without changing or normalizing account-status behavior

### Requirement: Reconnect resumes an owned existing world session

The system MUST resolve the existing GameWorld logical connection by authenticated
master ID and accept it only when the session and character belong to that same
identity.

#### Scenario: Matching ownership resumes the existing session

- GIVEN authenticated master ID M
- AND an existing GameWorld session and character both owned by M
- WHEN the reconnect request is handled
- THEN the existing logical connection is selected
- AND no new session or character is allocated

#### Scenario: Missing or wrong ownership is rejected

- GIVEN authenticated master ID M
- AND no matching existing session, or a session/character owned by another ID
- WHEN the reconnect request is handled
- THEN the request is rejected before successful response 15
- AND the existing logical state is unchanged

### Requirement: GameWorld delegates reconnect eligibility to Raido

GameWorld MUST NOT duplicate #477 reconnect lifetime, eligibility, concurrency,
timeout, stale, terminal, or adopted-state checks. The existing Raido transition
MUST decide whether the physical candidate can reconnect.

#### Scenario: Raido decides reconnect eligibility

- GIVEN GameWorld has authenticated the request and verified existing-session ownership
- WHEN the candidate attempts to reconnect
- THEN GameWorld delegates reconnect eligibility and lifetime decisions to the existing #477 transition

### Requirement: Fresh login remains unchanged

Opcode 16 with reconnect flag 0 MUST retain the existing fresh world sign-in
flow. Opcode 18, fresh allocation, hydration, registration, `WorldSignInCommand`,
and client changes are outside this capability.

#### Scenario: Reconnect changes do not alter fresh login

- GIVEN opcode 16 has reconnect flag 0
- WHEN a client performs the existing fresh world sign-in
- THEN the established fresh-login flow remains in effect
