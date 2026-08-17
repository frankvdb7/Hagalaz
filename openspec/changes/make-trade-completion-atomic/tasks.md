## 1. Session ownership and serialization

- [x] 1.1 Keep one owner-controlled session gate and link target lifecycle cleanup to that session.
- [x] 1.2 Restrict lifecycle to Active, Completing, Completed, and Cancelled; make accept/cancel/destruction terminal transitions idempotent.

## 2. Checked completion and refund

- [x] 2.1 Add a neutral per-container synchronization boundary and deterministic multi-container acquisition without adding trade behavior to `BaseItemContainer`.
- [x] 2.2 Keep opt-in trade-container operations boolean-only and preserve normal container semantics.
- [x] 2.3 Make money-pouch trade operations boolean-only while retaining pouch overflow/underflow ownership and notifications.
- [x] 2.4 Implement locked preflight, complete exchange, normal refund, and forced untouched-escrow recovery; do not retain receipts or compensation state.

## 3. Regression coverage

- [x] 3.1 Cover concurrent/repeated completion, accept/cancel, target cleanup, and independent trades.
- [x] 3.2 Cover capacity, offer revision, stackable/non-stackable items, coins, notification failure, refund, and conservation.
- [x] 3.3 Remove tests that assert rollback objects, deferred compensation, partial settlement, or aggregate-count restoration.

## 4. Validation

- [x] 4.1 Run focused trade and base-container tests and dependent builds.
- [x] 4.2 Run strict OpenSpec validation and final stale-reference/diff checks.
