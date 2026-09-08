## Context

`AuthenticationService.SignOutAsync` owns both live GameWorld session cleanup and the authorization revocation request. The current sequence sends the revocation request first, then persists the character and removes the session. Authorization can be slower than the client retry, and concurrent disconnect callbacks can revoke the same token set.

## Decisions

1. Keep persistence before world-session removal. The existing pending-logout/dehydration flow relies on the registered character remaining available until the durable handoff has been queued.
2. Move token revocation after session removal and character-detach coordination. This makes the live-session owner authoritative for login admission and prevents an authorization failure from retaining a successfully persisted session.
3. Catch non-cancellation revocation exceptions after cleanup and log them. The logout boundary has already released live ownership; a failed remote cleanup must not recreate the login lock. Cancellation remains observable to the caller.
4. Resolve the authorization named by the logout request and verify its subject and application before revoking its tokens. An unknown or mismatched authorization is an idempotent no-op; it must never fall back to subject-wide or client-wide revocation. Revoke the authorization's tokens by authorization id and transition a still-valid authorization to revoked when required.
5. Treat ad-hoc authorization creation as an ownership transaction. The Authorization consumer revokes tokens and deletes the exact authorization with `CancellationToken.None` until it has returned a successful issuance response. GameWorld keeps the returned subject and authorization ID in local ownership until UserInfo, principal validation, and the relevant lobby/world session commit complete; failed post-issuance setup sends the existing exact revocation request with non-cancelable cleanup.
6. Keep UserInfo validation as the issuance boundary, then use the existing
   `RaidoCallerContextExtensions.GetMasterId` helper for lobby and world
   session admission. The helper returns `null` for a missing or malformed
   subject, so sign-in returns the existing failed result before session
   creation and routes the exact issued authorization through the existing
   revocation helper.
7. Make exact revocation return a small success result. GameWorld clears
   authentication features only after success; if cleanup throws or reports
   failure, the exact authorization metadata remains attached for the normal
   disconnect/sign-out owner to retry. Sign-out uses the same helper with its
   non-cancelable cleanup token after live-session release.

## Invariants

- Persistence failure leaves the session and pending character intact for retry.
- Session release failure leaves existing session-cleanup recovery behavior unchanged.
- Releasing a session never depends on a successful authorization response.
- Revocation remains scoped to the exact authorization, whose subject and application must match the requested client and subject.
- A token created by a replacement login belongs to a different authorization and is not revoked by the old logout request.
- A failed new login can only clean up the authorization returned by that login; it cannot infer or revoke another persisted authorization.
- A malformed or missing subject cannot create a GameWorld session and cannot
  revoke any authorization other than the one issued for that attempt.
