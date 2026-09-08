## 1. Logout ordering

- [x] 1.1 Move token revocation after persistence, session release, and character-detach coordination while preserving failure recovery for persistence and session removal.
- [x] 1.2 Ensure non-cancellation revocation failures are logged without restoring the released session claim.

## 2. Idempotent revocation

- [x] 2.1 Carry the OpenIddict authorization id through sign-in, authentication properties, logout, and the revocation request.
- [x] 2.2 Validate the authorization subject and application before revoking by authorization id; mismatches and unknown ids are idempotent no-ops.
- [x] 2.3 Revoke the exact authorization's tokens and transition its authorization without broad subject/client revocation.

## 3. Regression coverage and validation

- [x] 3.1 Add GameWorld tests proving persisted logout releases the session before revocation completes and that revocation failure does not retain the session.
- [x] 3.2 Add Authorization tests covering exact authorization revocation, already-revoked authorization idempotency, unknown/mismatched ownership, and orphan authorization cleanup.
- [x] 3.3 Run focused tests and strict OpenSpec validation.
