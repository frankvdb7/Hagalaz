## Context

See proposal.md for the observed login failure. The authorization consumer currently queries valid persisted tokens before creating tokens and returns `IsAuthenticated` when any are found. GameWorld already performs the authoritative lobby/world session registration after authorization, including the distributed claim for world ownership.

## Goals / Non-Goals

**Goals:**

- Remove the stale persisted-token decision from credential sign-in.
- Preserve the existing token-creation, logout-revocation, reconnect-validation, and GameWorld session-ownership paths.
- Make the failure mode deterministic through a focused authorization consumer regression test.

**Non-Goals:**

- Changing token expiration, revocation, or OpenIddict persistence.
- Adding a cleanup worker, second online-session registry, or new retry path.
- Changing reconnect protocol messages or distributed claim lease behavior.

## Decisions

1. **GameWorld session ownership is authoritative.** Remove only the valid-token lookup from `SignInUserRequestConsumer`; after credentials succeed, the existing token creation path continues. `AuthenticationService` then calls the existing lobby/world registration methods, which reject true concurrent sessions locally or through the distributed claim store.

2. **Keep token lookup where it validates reconnect credentials.** `ValidateExistingAuthenticationRequestConsumer` remains unchanged because reconnect validation has a different contract: it must validate an existing token supplied by the reconnecting client.

3. **Keep graceful revocation unchanged.** `AuthenticationService.SignOutAsync` continues to send `RevokeTokenRequestMessage`; this change addresses the restart/abrupt-disconnect gap where no logout callback can run, not the existing revocation mechanism.

The rejected alternative is retaining the authorization token query and attempting to clear tokens during startup. That would make persistent token state continue to own online status, would require a new reconciliation policy, and could incorrectly disconnect a live session. A new session registry is also rejected because the existing GameWorld stores and distributed claim already own that responsibility.

## Risks / Trade-offs

- [Risk] Multiple successful token issuances can occur before GameWorld session registration rejects a duplicate connection. → Mitigation: GameWorld remains the gate that exposes a session; the authorization response is not treated as proof that a game session was accepted.
- [Risk] An abrupt process termination can leave tokens persisted until their normal expiry. → Mitigation: those tokens are no longer used as the online-session gate; graceful logout still revokes them.

## Migration Plan

Deploy the authorization consumer and its regression tests with the existing service. No data migration is required. Rolling back restores the prior stale-token behavior but does not alter stored tokens.
