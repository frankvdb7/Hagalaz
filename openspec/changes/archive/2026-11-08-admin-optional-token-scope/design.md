# Design

## Context

See `proposal.md` and `specs/admin-web-authentication/spec.md`. The authorization server validates the admin request, issues tokens, and successfully serves user-info. Its token response omits `scope` when the granted scope equals the requested scope, as allowed by OAuth 2.0.

## Goals / Non-Goals

**Goals:**

- Preserve the requested scope for admin authorization checks when the successful response omits it.
- Preserve an explicitly returned scope as the source of truth.
- Keep admin role and `cache_api` checks in the existing `AuthStore`.

**Non-Goals:**

- Change the authorization server, scopes, admin roles, client registration, CAPTCHA flow, or normal frontend.
- Add a new token state owner, retry path, or dependency.

## Decisions

- `AuthService` owns token response normalization because it constructs the exact scope request. It uses the returned scope when present and the requested scope only when the response omits it. The same path handles password and refresh grants.
- `AuthStore` remains the owner of admin authorization. Its scope check will safely treat an absent value as no permissions, preventing malformed data from throwing or granting access.
- A store-level HTTP regression test will exercise CAPTCHA verification, a token response without `scope`, bearer-authenticated user-info, and the admin role check. It will also verify an explicitly reduced scope remains denied.
- The server will not be changed to always return `scope`: the observed response is valid OAuth behavior, and adapting the existing client is the smaller boundary change.

## Risks / Trade-offs

- If an authorization server omits `scope` while granting less than requested, using the requested value could overstate access. OAuth 2.0 requires a response scope when the grant differs; this client relies on that protocol contract and continues to honor any explicit response scope.

## Migration Plan

- No data migration is needed. The admin frontend change takes effect when its current development server rebuilds or the deployed bundle is refreshed.
