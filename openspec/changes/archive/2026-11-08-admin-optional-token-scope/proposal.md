# Proposal

## Why

Admin sign-in reaches successful token issuance and user-info, but the admin store throws when it checks `token.scope` because the live OAuth token response omits that field. OAuth permits omitting `scope` when the granted scopes match the request, so the client should handle that response shape.

## What Changes

- Normalize an omitted token `scope` in the admin auth service to the exact scope requested for that grant.
- Keep explicitly returned scopes authoritative so a reduced grant cannot be mistaken for admin access.
- Add a regression test for the admin sign-in path with the live token response shape.

## Non-goals

- Do not change authorization server behavior, client registration, scope permissions, CAPTCHA verification, account roles, or admin policy.
- Do not change normal-app authentication or redesign login errors.

## Acceptance Criteria

- When the token endpoint omits `scope` for the requested admin scopes, a valid admin user-info response completes sign-in without the generic authentication error.
- When the token endpoint explicitly returns a scope without `cache_api`, the admin app does not treat the account as an authorized administrator.
- The regression test covers CAPTCHA, token, bearer-authenticated user-info, and the final admin authorization result.

## Stop Conditions

- If the end-to-end store test does not reproduce the missing-scope failure, stop and revise the diagnosis before changing production code.
- If the token or user-info request fails independently, record that as a separate cause and keep this change limited to the observed failure.

## Capabilities

### New Capabilities

- `admin-web-authentication`: Admin sign-in interprets OAuth token responses and enforces admin access.

### Modified Capabilities

None.

## Impact

- Admin Angular `AuthService` and `AuthStore` plus their tests.
- No backend endpoint, dependency, or database changes.
