# Design

## Context

See proposal.md for the problem and acceptance criteria. The main web app already uses `storm-app` and requests `characters_api`; the admin app requests both `characters_api` and `cache_api`. The preceding fix registered one `storm-app` client with both API scopes, so the existing record needs a narrow scope correction.

## Goals / Non-Goals

**Goals:** Give each frontend a stable client ID and keep admin-only scope permission off the normal client while retaining the existing OpenIddict store as owner.

**Non-Goals:** Change user authorization, token contents, OAuth grant flow, API scopes, or persistence schema.

## Decisions

- Retain `storm-app` for normal web and use `storm-admin` for admin. This avoids changing the normal app's existing client ID and gives the two frontends distinct, recognizable IDs.
- Build both public web client descriptors from the existing token/end-session endpoints, password/refresh-token grants, and common `email`, `profile`, and `roles` scopes. Add `characters_api` to both; add `cache_api` only to `storm-admin`. Set the OpenIddict application type to `Web`, which the live database requires, and configure no client secret.
- Keep `IOpenIddictApplicationManager` and its EF store as the only registration owner. At startup, create either missing client. For an existing `storm-app`, populate a descriptor from the stored application and update it only when its `cache_api` permission is present; this removes the one permission introduced by the prior shared registration while preserving every other setting. Leave an existing `storm-admin` unchanged.
- Keep each ID in its corresponding frontend auth service. Changing the existing normal ID or retaining one shared ID was rejected because it would break the normal app's existing identity or keep the two permission boundaries coupled.
- Deploy the authorization service and admin frontend together so the new `storm-admin` registration and the admin app's updated request ID are available as one release.

## Risks / Trade-offs

- [An older admin frontend may still request `cache_api` through `storm-app` after reconciliation] → Deploy the authorization service and the updated admin frontend together.
- [An unexpected existing `storm-app` has settings maintained elsewhere] → Populate and update its existing descriptor, changing only the `cache_api` permission.
- [OpenIddict tables are unavailable at startup] → Fail service startup rather than serve token requests with missing clients; Aspire already orders authorization after the migration resource.

## Migration Plan

No schema change is needed. Authorization startup creates `storm-admin` if absent and removes `cache_api` from the existing `storm-app` permission set while preserving other fields. The normal frontend continues using `storm-app`; deploy the admin frontend update with the authorization service so it switches to `storm-admin`. Rolling back the code leaves both stored records in place; a rollback that restores the old admin frontend must also restore `cache_api` on `storm-app` through the existing OpenIddict store.
