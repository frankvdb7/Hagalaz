# Design

## Context

See proposal.md for the observed failure and scope. Both web frontends already use the same client ID and the authorization service already persists OpenIddict application registrations through its EF Core store. The AppHost waits for database migrations before starting Authorization.

## Goals / Non-Goals

**Goals:** Add the missing first-party client registration before the authorization service accepts requests, using the existing OpenIddict manager and store.

**Non-Goals:** Change the existing password-grant flow, relax client validation, or update an already registered client.

## Decisions

- Use `IOpenIddictApplicationManager` as the source of truth. A focused registration component checks for `storm-app` and creates it only when absent. It runs once during service startup in an async DI scope, before `RunAsync`; no worker, retry loop, or parallel configuration store is needed.
- Configure `storm-app` as a public web application without a secret because the client runs in browser JavaScript. The current Aspire database requires `ApplicationType` when persisting application records, so set it to OpenIddict's `Web` application type. Grant only the token and end-session endpoints, password and refresh-token grants, and the current requested scopes: `email`, `profile`, `roles`, `characters_api`, and `cache_api`. OpenIddict handles `openid` and `offline_access` as special scopes without explicit scope permissions.
- Leave an existing registration untouched. This avoids overwriting settings maintained outside this service. If an existing registration is present but incompatible, stop and revise the change rather than silently reconciling it.
- Reject accepting anonymous clients or disabling permission checks. Those options would hide the missing registration by weakening validation for every client. Changing the frontend client ID is also rejected because both clients already agree on `storm-app` and no other registration exists in repository configuration.

## Risks / Trade-offs

- [The public browser client can request user tokens using the existing password grant] → Keep the registration limited to the two existing grants, existing token/logout endpoints, and existing scopes; do not add a client secret that browser code cannot keep confidential.
- [Startup cannot create the registration if the OpenIddict tables are unavailable] → Run initialization after service construction, rely on the AppHost's migration dependency, and fail service startup rather than serve a broken login endpoint.

## Migration Plan

No schema migration is required. The authorization service adds the application record on its next startup if it is absent. Rolling back the code stops future registration attempts; it does not delete an existing OpenIddict record.
