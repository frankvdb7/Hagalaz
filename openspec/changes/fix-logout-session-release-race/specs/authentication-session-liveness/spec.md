## Purpose

Ensures logout releases live game-session ownership promptly while preserving durable character cleanup and idempotent authorization revocation.

## ADDED Requirements

### Requirement: Logout releases live ownership independently of revocation latency

The logout flow SHALL persist a world character before releasing its session, and SHALL release that session before waiting for token revocation to finish.

#### Scenario: Immediate login follows a persisted logout

- **WHEN** the disconnected session has been persisted and the client attempts to sign in while token revocation is still in progress
- **THEN** the old lobby/world session no longer owns the account and the new sign-in is not rejected by that old session

#### Scenario: Persistence fails

- **WHEN** character persistence fails during logout
- **THEN** the existing session and pending character remain available for recovery

### Requirement: Token revocation is idempotent for completed transitions

The authorization revocation flow SHALL only process valid tokens created before logout started for the requested client and SHALL treat a token that became non-valid during a concurrent revoke as successfully completed.

#### Scenario: Token was already revoked

- **WHEN** a repeated logout revokes a token that is already non-valid
- **THEN** the revocation response succeeds without `ID2079`

#### Scenario: Revocation genuinely fails

- **WHEN** a valid token remains valid after a failed revoke attempt
- **THEN** the revocation response reports the existing revocation failure

#### Scenario: Replacement-login token is preserved

- **WHEN** a new token is created after the original logout starts but before its revocation request is processed
- **THEN** the original logout does not revoke the new token

### Requirement: Uncommitted authorization issuance is cleaned up exactly

After an ad-hoc authorization is created, the authorization service and
GameWorld MUST retain ownership of that exact authorization until issuance and
authentication commit. Any failure before commit MUST revoke its tokens and
delete/revoke that authorization using cleanup independent of the canceled
request token.

#### Scenario: Authorization dispatch fails after request cancellation

- **WHEN** token dispatch fails after an authorization was created and the
  request cancellation token is canceled
- **THEN** cleanup MUST use a non-canceled cleanup token
- **AND** the created authorization MUST not remain orphaned

#### Scenario: GameWorld user validation fails after issuance

- **WHEN** token issuance succeeds but UserInfo or principal validation fails
- **THEN** GameWorld MUST revoke the exact newly issued authorization
- **AND** MUST NOT revoke an unrelated authorization belonging to another
  established session

#### Scenario: Authentication commits successfully

- **WHEN** token issuance, UserInfo, principal validation, and authentication
  feature installation all succeed
- **THEN** the exact authorization ID MUST be retained by the connection
- **AND** pre-commit cleanup MUST NOT run

#### Scenario: Existing authorization cleanup fails

- **WHEN** GameWorld attempts to revoke the exact authorization after a later
  login/setup failure and revocation throws or reports failure
- **THEN** the authentication features MUST retain the exact authorization ID
- **AND** the failed connection's normal disconnect/sign-out owner MUST remain
  able to retry that exact cleanup

#### Scenario: Sign-out cleanup outlives request cancellation

- **WHEN** sign-out has released the live session and the client request is
  canceled while exact authorization cleanup is still pending
- **THEN** exact authorization cleanup MUST use a non-canceled cleanup token

### Requirement: Pending authorization cleanup remains exact before authentication commit

GameWorld MUST retain the client, subject, and authorization ID for a
successfully issued but not yet committed authorization until exact cleanup
succeeds or authentication commits. This pending cleanup identity MUST NOT be
treated as authenticated state.

#### Scenario: Pre-commit revoke fails

- **WHEN** UserInfo or principal validation fails and exact authorization
  revocation reports failure
- **THEN** the connection retains the issued authorization's exact cleanup
  identity for disconnect retry
- **AND** no authentication feature is installed for that failed validation

#### Scenario: Disconnect retries pending cleanup

- **WHEN** disconnect cleanup runs after the pre-commit revoke failed
- **THEN** it retries the same authorization ID
- **AND** clears the pending identity only after exact revocation succeeds

### Requirement: Replacement authorization issuance is serialized with existing ownership

GameWorld MUST NOT issue a replacement authorization on a connection while that
connection has committed authentication state or unresolved pending authorization
cleanup. A replacement MAY be issued only after pending exact cleanup succeeds.

#### Scenario: Committed authorization blocks replacement issuance

- **WHEN** the connection already owns a committed authorization and another sign-in attempt is received
- **THEN** no replacement authorization request is sent
- **AND** the committed authorization remains attached to the connection

#### Scenario: Failed pending cleanup blocks replacement issuance

- **WHEN** exact cleanup of a previously issued but uncommitted authorization fails
- **THEN** no replacement authorization request is sent
- **AND** the original pending authorization identity remains available for retry

#### Scenario: Successful pending cleanup allows replacement issuance

- **WHEN** exact cleanup of a previously issued but uncommitted authorization succeeds
- **THEN** the pending identity is cleared
- **AND** the connection MAY issue and commit a replacement authorization

### Requirement: Lobby sign-out requires owned lobby session

GameWorld MUST publish lobby sign-out only when the connection has a real
session that is not a GameWorld session and has a valid authenticated subject.

#### Scenario: Failed sign-in has no lobby session

- **WHEN** a failed lobby or world sign-in retains authentication or pending
  cleanup metadata but owns no session
- **THEN** disconnect cleanup does not publish lobby sign-out

#### Scenario: Another lobby session owns the account

- **WHEN** a failed world connection has no session while another connection
  owns the account's lobby session
- **THEN** the failed connection does not publish lobby sign-out
