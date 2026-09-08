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
