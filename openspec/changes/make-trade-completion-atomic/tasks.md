## 1. Session ownership and serialization

- [x] 1.1 Add the owner-controlled trade session gate/terminal state and link target lifecycle cleanup to that same session. (Acceptance: serialized terminal transition; disconnect cleanup)
- [x] 1.2 Route final acceptance, offer changes, cancellation, interruption, destruction, and confirmation revision checks through the shared protected operation. (Acceptance: repeated packets; accept/cancel race; offer invalidation)

## 2. Checked atomic exchange and cleanup

- [x] 2.1 Add checked offer/refund helpers for inventory, trade-container, and money-pouch mutations, preserving escrow when a reversal cannot be safely completed. (Acceptance: all mutation results checked; cancellation conservation)
- [x] 2.2 Implement preflight and rollback-protected two-recipient exchange using existing containers and money-pouch behavior; clear escrow only after success. (Acceptance: capacity, mutation failure, stackable/non-stackable, and coin conservation)
- [x] 2.3 Consolidate completion and cancellation into one terminal cleanup path and remove any reachable post-completion refund path. (Acceptance: one cleanup implementation; no refund after completion)
- [x] 2.4 Distinguish incomplete exchange compensation from an ordinarily failed exchange, and use persisted rewards/bank recovery during destruction when inventory refund cannot fit. (Acceptance: no refund after incomplete compensation; deterministic disconnect conservation)

## 3. Deterministic regression coverage

- [x] 3.1 Add barrier/controlled-task coverage for concurrent final accepts, repeated accepts, accept/cancel, target disconnect/logout, and independent trades. (Required regression tests 1-4 and 10-11)
- [x] 3.2 Add focused success/failure coverage for item quantities, stackability, coin conservation, destination capacity, offer mutation, and recipient mutation rollback. (Required regression tests 5-9)
- [x] 3.3 Add regressions for compensation-pending completion and destruction recovery without a later tick. (Required regression tests 12-13)

## 4. Validation

- [x] 4.1 Run the focused `Hagalaz.Game.Scripts.Tests` trade suite and the relevant container tests to a clean exit.
- [x] 4.2 Run strict OpenSpec validation, a dependent build, and final diff/status review; report any unavailable distributed validation separately.
