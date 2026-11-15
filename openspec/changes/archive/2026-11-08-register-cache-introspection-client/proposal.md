# Proposal

## Why

The admin frontend reaches the Cache API, but `GET /api/v1/cache/types/archive-sizes` fails with HTTP 500. Aspire's trace shows Cache API receiving HTTP 401 from Authorization's `/connect/introspect`; Authorization returns `invalid_client` because the request identifies itself as `cache-service-1`, which is not registered.

## What Changes

- Register `cache-service-1` in Authorization as a confidential client permitted to call the introspection endpoint.
- Store one secret as an Aspire AppHost parameter in .NET User Secrets and pass it to both Authorization and Cache API as `Security:Introspection:ClientSecret`.
- Update an existing `cache-service-1` registration when its stored secret no longer matches the configured value.
- Remove the checked-in development secret values from the service appsettings files.
- Add regression coverage for the service-client registration while preserving the two existing web-client registrations.

## Non-goals

- Do not change admin login scopes, user permissions, Cache API routes, or frontend error handling.
- Do not change generic handling of downstream authentication failures.
- Do not add a new OAuth flow or third-party dependency.

## Acceptance Criteria

- On startup, Authorization creates the missing `cache-service-1` confidential application with the configured secret and the introspection endpoint permission.
- Existing normal and admin frontend registrations retain their current permissions.
- The shared development secret is stored outside the repository and both services receive the same configured value.
- Restarting Authorization after secret rotation updates the existing application's credential without changing its endpoint permissions.
- With matching Cache API and Authorization secret configuration, introspection accepts `cache-service-1` and the admin archive-sizes request no longer fails because of `invalid_client`.
- A regression test verifies the registered client ID, confidential type, configured secret, and endpoint permission.

## Stop Conditions

- If a fresh Aspire trace no longer identifies `invalid_client` as the source of the 401, revisit the diagnosis before broadening the fix.
- If the Cache API request fails after introspection authenticates the client, record that as a separate failure rather than expanding this change.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `authorization-client-registration`: Authorization also registers the Cache API's confidential introspection client.

## Impact

- `Hagalaz.AppHost` secret parameter wiring and local User Secrets.
- `Hagalaz.Services.Authorization` client registration.
- `Hagalaz.Services.Cache` development configuration.
- Focused Authorization registration tests.
- Aspire runtime verification of the Cache API archive-sizes request.
