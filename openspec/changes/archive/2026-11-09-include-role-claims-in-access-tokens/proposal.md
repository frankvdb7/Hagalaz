# Proposal

## Why

Cache API now accepts the introspected admin token, and the token includes `cache_api` and `roles` scopes, but the request still returns 403. Cache API's policy also requires the `SystemAdministrator` role, while Authorization's introspection response contains no role claim because the token issuance flow never assigns an access-token destination to it.

## What Changes

- Include user role claims in access tokens only when the `roles` scope was granted.
- Apply the same rule to password and refresh-token grants.
- Add regression tests for role-claim destinations with and without the `roles` scope.

## Non-goals

- Do not remove or weaken Cache API's administrator-role or `cache_api` scope checks.
- Do not change frontend UserInfo role handling, client scope registration, or unrelated access-token claims.

## Acceptance Criteria

- An access token issued with the `roles` scope contains the user's role claim so Cache API can enforce its administrator policy.
- A token issued without the `roles` scope does not contain role claims.
- Password and refresh-token grants follow the same rule.
- The existing Cache API policy continues to require both the configured administrator role and `cache_api` scope.

## Stop Conditions

- If the access token already contains the administrator role in a fresh grant, revisit the failure trace before changing token issuance.
- If including the scoped role claim does not satisfy the Cache API policy, investigate claim-type mapping as a separate root cause before expanding this change.

## Capabilities

### New Capabilities

- `api-access-token-claims`: defines which user claims Authorization exposes to API resource servers in access tokens.

### Modified Capabilities

None.

## Impact

- Authorization password-grant and refresh-token-grant principal handling.
- Authorization tests for token claim destinations.
- Cache API authorization behavior for valid administrator tokens.
