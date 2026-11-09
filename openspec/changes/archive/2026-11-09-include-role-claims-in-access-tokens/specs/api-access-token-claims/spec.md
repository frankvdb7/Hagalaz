## Purpose

Defines which user authorization claims the Authorization service exposes to API resource servers through OAuth access tokens.

## ADDED Requirements

### Requirement: Role claims are included only for the roles scope

The authorization service SHALL include a user's role claims in access tokens only when the corresponding grant includes the `roles` scope.

#### Scenario: Password grant includes roles scope
- **WHEN** a user with assigned roles completes a password grant that includes `roles`
- **THEN** the issued access token contains the user's role claims

#### Scenario: Password grant omits roles scope
- **WHEN** a user with assigned roles completes a password grant without `roles`
- **THEN** the issued access token does not contain the user's role claims

#### Scenario: Refresh grant preserves roles scope
- **WHEN** a refresh token was issued with `roles` and the user still has assigned roles
- **THEN** the refreshed access token contains the user's current role claims
