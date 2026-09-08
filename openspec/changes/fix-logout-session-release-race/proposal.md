## Why

After a client disconnects, GameWorld currently waits for token revocation before releasing the local or distributed game-session ownership. An immediate login therefore observes the old session and receives `Already logged in`; repeated disconnect cleanup can also race on the same persisted tokens and surface an authorization connection error.

## What Changes

- Keep character persistence ahead of world-session release so an acknowledged logout cannot discard the last in-memory character state.
- Release the game-session ownership before performing best-effort token revocation, so a completed session logout is not blocked by authorization latency or a duplicate revocation.
- Make token revocation idempotent for tokens that are no longer valid, while retaining an error response for tokens that remain valid after a failed revoke attempt.
- Scope logout revocation to tokens created before logout started so a concurrent replacement login cannot have its fresh token revoked by the old logout.
- Add regression coverage for session release ordering, revocation failure, and repeated revocation of already-revoked tokens.

## Non-Goals

- Do not add a second online-session registry, retry queue, or token cleanup worker.
- Do not change token lifetime, reconnect validation, distributed claim leases, or character persistence acknowledgement semantics.
- Do not weaken the existing rule that a persistence failure retains the world session for recovery.

## Acceptance Criteria

- A logout with a character persists it before releasing world ownership.
- A token-revocation request cannot prevent a successfully persisted logout from releasing its session or detaching its character.
- A repeated revocation request does not return `ID2079` merely because the token was already revoked; a token that remains valid after a failed revoke still reports failure.
- Focused GameWorld and Authorization tests pass, and strict OpenSpec validation passes.

## Affected Runtime Boundary

The change is limited to GameWorld logout cleanup and the Authorization token-revocation consumer. It affects the disconnect-to-login transition and the authorization message exchanged during logout.
