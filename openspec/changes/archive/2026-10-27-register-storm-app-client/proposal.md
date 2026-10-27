# Proposal

## Why

The admin app sends `client_id=storm-app` to the authorization token endpoint, but the live OpenIddict application store does not recognize that client. OpenIddict responds with `401` and a Basic authentication challenge, which Chrome displays as an extra username/password popup and prevents the normal login flow from continuing.

## What Changes

- Register the existing `storm-app` browser client as a public web application when it is missing, with only the token and end-session endpoints, password and refresh-token grants, and scopes already requested by the web frontends.
- Preserve an existing registration without changing its permissions or credentials.
- Add focused coverage for the registration descriptor and its create-if-missing behavior.

## Capabilities

### New Capabilities

- `authorization-client-registration`: the authorization service recognizes and permits the first-party browser client used by Hagalaz web apps.

### Modified Capabilities

None.

## Impact

Authorization-service startup and tests, plus one OpenIddict application record in the existing EF-backed application store. Reuse the registered OpenIddict application manager and database store; no dependency or schema changes are needed.

## Acceptance Criteria

- A1: Startup creates `storm-app` only when no registration exists and leaves an existing registration unchanged.
- A2: The created client is public and is restricted to the endpoints, grants, and scopes used by the existing web app and admin app.
- A3: A token request using `storm-app` is no longer rejected as `invalid_client` with `WWW-Authenticate: Basic`, so Chrome does not show the extra HTTP login dialog.

## Non-goals and Stop Conditions

- Do not change the password grant, user credentials, account roles, CAPTCHA flow, token claims, issuer configuration, or either frontend's client ID.
- Do not permit anonymous or arbitrary OAuth clients, disable OpenIddict permission checks, or modify existing client registrations.
- If the live target store contains an existing `storm-app` registration with incompatible settings, stop and reassess before changing that record.
