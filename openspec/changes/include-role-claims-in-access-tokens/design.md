# Design

## Current behavior

The latest Aspire trace shows an active token for `storm-admin` with `cache_api` and `roles` in its `scope`, followed by Cache API returning 403. The token introspection response has no `role` claim. `CacheAuthorization` requires both `Roles.SystemAdministrator` and the `cache_api` scope. The frontend still knows the role because its UserInfo handler reads roles from the database independently.

`PasswordGrantCommandConsumer` creates an Identity principal and assigns scopes, but no authorization code calls `SetDestinations`. OpenIddict only copies the mandatory subject claim into access tokens by default; other claims need explicit destinations.

## Chosen design

- Add a small authorization helper that marks role claims for the access token only when the granted scopes include `roles`.
- Call it after setting scopes in the password-grant flow.
- In the refresh-token flow, use the validated refresh-token principal's granted scopes to decide whether to mark role claims on the refreshed principal.
- Leave other claims without an access-token destination and keep Cache API's existing role-plus-scope policy.
- Test the helper with granted and ungranted `roles` scope cases.

## Alternatives considered

- Removing `RequireRole` would let any user holding a token with `cache_api` through, weakening an existing authorization invariant.
- Loading the role through a new Cache API call would add another request and state path when the authorization server already owns user roles.
- Changing the frontend role display would not affect the server-side role requirement.

## Risks

Role claims become readable by resource servers receiving tokens when the client requests and receives the `roles` scope. Tokens without that scope remain role-free.

## Open questions

None.
