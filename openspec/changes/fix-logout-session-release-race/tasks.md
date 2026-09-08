## 1. Logout ordering

- [x] 1.1 Move token revocation after persistence, session release, and character-detach coordination while preserving failure recovery for persistence and session removal.
- [x] 1.2 Ensure non-cancellation revocation failures are logged without restoring the released session claim.

## 2. Idempotent revocation

- [x] 2.1 Restrict revocation enumeration to valid tokens for the requested application.
- [x] 2.2 Re-check a token's status after a failed revoke and ignore a concurrent transition that already made it non-valid.
- [x] 2.3 Carry the logout start cutoff through the revocation request so replacement-login tokens are excluded.

## 3. Regression coverage and validation

- [x] 3.1 Add GameWorld tests proving persisted logout releases the session before revocation completes and that revocation failure does not retain the session.
- [x] 3.2 Add Authorization tests covering already-revoked/concurrently-revoked tokens, a token that remains valid after a failed revoke, and a replacement-login token excluded by the cutoff.
- [x] 3.3 Run focused tests and strict OpenSpec validation.
