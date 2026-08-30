# World reconnect session

## ADDED Requirements

### Requirement: Reconnect credentials use existing validation

The system MUST validate reconnect credentials through the existing
`PasswordGrantCommand` behavior and expose the authenticated subject/master ID
without issuing or inspecting a token.

#### Scenario: Valid credentials return the subject

- GIVEN a reconnect request with valid credentials
- WHEN tokenless authorization validates them
- THEN the response contains the authenticated subject/master ID
- AND no token is created or looked up

#### Scenario: Existing validation outcomes are preserved

- GIVEN credentials that produce an existing `PasswordGrantCommand` outcome
- WHEN reconnect authorization consumes them
- THEN that outcome is mapped without changing account-status behavior

### Requirement: Reconnect resumes an owned existing world session

The system MUST resolve the existing GameWorld logical connection by authenticated
master ID and accept it only when the session and character belong to that identity.

#### Scenario: Matching ownership resumes the existing session

- GIVEN authenticated master ID M
- AND an existing GameWorld session and character owned by M
- WHEN the reconnect request is handled
- THEN the existing logical connection is selected
- AND no new session or character is allocated

#### Scenario: Missing or wrong ownership is rejected

- GIVEN authenticated master ID M
- AND no matching existing session, or a session/character owned by another ID
- WHEN the reconnect request is handled
- THEN the request is rejected before a successful reconnect response
- AND existing logical state is unchanged

### Requirement: Reconnect response is sent through the existing flow

The reconnect handler MUST construct the revision-742 response from the existing
character and send it through the ordinary caller send path before scheduling
the existing reconnect publication.

#### Scenario: Valid reconnect

- GIVEN valid credentials and matching existing ownership
- WHEN the reconnect request is handled
- THEN response 15 is sent
- AND the existing reconnect operation is scheduled
- AND no fresh sign-in flow is started

### Requirement: Fresh sign-in remains unchanged

The existing fresh world sign-in flow MUST continue to use its established
authentication, session, and character behavior.

#### Scenario: Fresh flag-zero login

- GIVEN a fresh world login request with reconnect flag 0
- WHEN the established sign-in flow handles it
- THEN its existing authentication, session, and character behavior is used
