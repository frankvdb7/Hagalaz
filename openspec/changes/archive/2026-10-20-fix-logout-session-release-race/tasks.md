## 1. Logout ordering

- [x] 1.1 Keep the session claim and registered character until the exact final persistence receipt is acknowledged, then release the session and detach while preserving failure recovery for persistence and session removal.
- [x] 1.2 Ensure non-cancellation revocation failures are logged without restoring the released session claim.
- [x] 1.3 Return an exact persistence receipt for forced logout snapshots and match acknowledgements to that receipt.
- [x] 1.4 Keep persistence acknowledgement in the persistence service and
      prevent duplicate forced snapshots by reusing the exact logout receipt.

## 2. Idempotent revocation

- [x] 2.1 Carry the OpenIddict authorization id through sign-in, authentication properties, logout, and the revocation request.
- [x] 2.2 Validate the authorization subject and application before revoking by authorization id; mismatches and unknown ids are idempotent no-ops.
- [x] 2.3 Revoke the exact authorization's tokens and transition its authorization without broad subject/client revocation.
- [x] 2.4 Make authorization issuance cleanup exception-safe after creation,
      including canceled-request dispatch failures and authorization-ID lookup
      failures.
- [x] 2.5 Retain the issued subject and authorization ID through GameWorld
      UserInfo/principal/session commit and revoke only that authorization on
      post-issuance failure.
- [x] 2.6 Reject missing or malformed UserInfo subjects with `uint.TryParse`,
      revoke the exact issued authorization, and avoid session creation.
- [x] 2.7 Retain authentication features when exact cleanup fails, and route
      sign-out through the same non-cancelable exact-revocation helper.
- [x] 2.8 Retain pending exact authorization identity until pre-commit cleanup
      succeeds or authentication commits, including disconnect retry.
- [x] 2.9 Publish lobby sign-out only for an actually owned non-world session.
- [x] 2.10 Serialize replacement authorization issuance with committed and pending
      authorization ownership, including the exact cleanup-success gate.

## 3. Regression coverage and validation

- [x] 3.1 Add GameWorld tests proving persisted logout releases the session before revocation completes and that revocation failure does not retain the session.
- [x] 3.2 Add Authorization tests covering exact authorization revocation, already-revoked authorization idempotency, unknown/mismatched ownership, and orphan authorization cleanup.
- [x] 3.3 Add issuance and GameWorld sign-in regression tests for canceled
      cleanup, post-token validation failure, exact ownership, and successful
      commit.
- [x] 3.4 Run focused tests and strict OpenSpec validation.
