## Purpose

Ensures authorization token persistence is not used as a substitute for live game-session ownership, so interrupted clients can authenticate again while active sessions remain exclusive.

## ADDED Requirements

### Requirement: Credential sign-in is independent of persisted token existence

The authorization sign-in flow SHALL evaluate the supplied credentials and issue a new authorization response without rejecting the request merely because a valid token for the same user and client already exists in persistent storage.

#### Scenario: Interrupted client signs in while an old token remains valid

- **WHEN** valid credentials are submitted and persistent storage contains a valid token for the same user and client, but no active game session owns that user
- **THEN** the authorization flow issues a successful sign-in response with fresh token data

#### Scenario: Invalid credentials remain rejected

- **WHEN** invalid credentials are submitted
- **THEN** the authorization flow returns the existing credential failure response and does not issue tokens

### Requirement: Live session ownership remains authoritative

The GameWorld session ownership mechanism SHALL remain the authority for rejecting concurrent lobby or world sessions for the same account.

#### Scenario: Active lobby session rejects a second lobby sign-in

- **WHEN** an account already has an active lobby session and another lobby connection authenticates it
- **THEN** the second sign-in returns the existing already-logged-on result

#### Scenario: Active world session rejects a second world sign-in

- **WHEN** an account already has an active world session or distributed world-session claim and another world connection authenticates it
- **THEN** the second sign-in returns the existing already-logged-on result

### Requirement: Existing token lifecycle boundaries remain unchanged

Graceful logout SHALL continue to request token revocation, and reconnect authentication SHALL continue to validate its supplied token through the existing validation flow.

#### Scenario: Graceful logout requests revocation

- **WHEN** an authenticated session signs out normally
- **THEN** the existing token-revocation request is sent before logout cleanup completes

#### Scenario: Reconnect validates its token

- **WHEN** a reconnect request is received
- **THEN** the existing reconnect token-validation flow determines whether the session may be resumed
