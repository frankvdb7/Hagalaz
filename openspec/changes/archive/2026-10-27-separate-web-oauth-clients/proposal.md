# Proposal

## Why

The normal and admin web apps currently send the same OAuth client ID, while the admin app requests the additional `cache_api` scope. That makes the normal client eligible for an admin-only permission and prevents the authorization service from distinguishing the two frontends.

## What Changes

- Keep `storm-app` as the normal web client and give the admin frontend a distinct `storm-admin` client ID.
- Register both clients as public web applications with only the endpoints, grants, and scopes each frontend uses.
- Remove only the `cache_api` scope permission from an existing `storm-app` registration while preserving its other settings.
- Add authorization-service and frontend tests that assert the client IDs and scope separation.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `authorization-client-registration`: define separate registrations and login requests for the normal and admin web apps.

## Impact

Authorization-service startup and OpenIddict application permissions; the admin Angular auth service and its tests; the normal Angular auth service tests. No dependencies or database schema changes are needed.

## Acceptance Criteria

- A1: Normal web uses `client_id=storm-app` and can request `characters_api` without permission to request `cache_api`.
- A2: Admin web uses `client_id=storm-admin` and can request both `characters_api` and `cache_api`.
- A3: Both registrations are public web clients without secrets and permit only the existing password/refresh-token grants, token/end-session endpoints, and their respective scopes.
- A4: Startup creates either client when missing, leaves an existing `storm-admin` registration unchanged, and removes only `cache_api` from an existing `storm-app` registration.
- A5: Token requests using either frontend's client ID reach grant validation without `invalid_client` or an HTTP Basic challenge.

## Non-Goals and Stop Conditions

- Do not change password/refresh-token handling, user credentials, account roles, CAPTCHA, token claims, endpoint configuration, or API scope definitions.
- Do not add a client secret, accept arbitrary clients, disable OpenIddict permission checks, or modify other client registrations.
- If preserving all existing `storm-app` settings while removing `cache_api` is not supported by the manager, stop and revise the design rather than replacing the application descriptor wholesale.
