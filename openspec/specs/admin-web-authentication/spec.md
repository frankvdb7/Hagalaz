# Admin Web Authentication Specification

## Purpose

Defines how the Hagalaz admin frontend completes sign-in from OAuth token responses while enforcing the server-granted scope and administrator role.

## Requirements

### Requirement: Admin sign-in accepts an omitted scope when the grant matches the request
The admin frontend SHALL treat an omitted token response scope as the scope requested for that grant, while continuing to enforce the admin scope and role requirements.

#### Scenario: Successful admin grant omits scope
- **WHEN** a token response omits `scope` and the user-info response contains the `SystemAdministrator` role
- **THEN** the frontend treats the granted scope as the scope requested for that token and completes sign-in

#### Scenario: Server explicitly reduces the granted scope
- **WHEN** a token response includes a scope that does not contain `cache_api`
- **THEN** the frontend does not infer `cache_api` from the request and denies admin access
