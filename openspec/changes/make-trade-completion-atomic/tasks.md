## 1. Session ownership and serialization

- [x] 1.1 Add the owner-controlled trade session gate/terminal state and link target lifecycle cleanup to that same session. (Acceptance: serialized terminal transition; disconnect cleanup)
- [x] 1.2 Route final acceptance, offer changes, cancellation, interruption, destruction, and confirmation revision checks through the shared protected operation. (Acceptance: repeated packets; accept/cancel race; offer invalidation)

## 2. Checked atomic exchange and cleanup

- [x] 2.1 Add checked offer/refund helpers and one focused opt-in trade-container operation whose public result is success/failure while exact compensation remains internal; commit storage independently of observer delivery, record notification failure separately, and make rollback apply the inverse delta while preserving unrelated container behavior. (Acceptance: all mutation results checked; cancellation conservation; no aggregate-count rollback; unrelated container behavior unchanged)
- [x] 2.2 Implement preflight and rollback-protected two-recipient exchange using existing containers and money-pouch behavior; clear escrow only after success. (Acceptance: capacity, mutation failure, stackable/non-stackable, and coin conservation)
- [x] 2.3 Consolidate completion and cancellation into one terminal cleanup path and remove any reachable post-completion refund path. (Acceptance: one cleanup implementation; no refund after completion)
- [x] 2.4 Distinguish incomplete exchange compensation from an ordinarily failed exchange; do not use persisted rewards/bank recovery to settle a compensation-pending exchange, while forced destruction may store untouched cancellation escrow for its original owner. (Acceptance: no partial exchange settlement; scoped disconnect conservation)

## 3. Deterministic regression coverage

- [x] 3.1 Add barrier/controlled-task coverage for concurrent final accepts, repeated accepts, accept/cancel, target disconnect/logout, and independent trades. (Required regression tests 1-4 and 10-11)
- [x] 3.2 Add focused success/failure coverage for item quantities, stackability, coin conservation, destination capacity, offer mutation, and recipient mutation rollback. (Required regression tests 5-9)
- [x] 3.3 Add regressions for exact compensation after an unrelated recipient mutation, all-or-nothing compensation-pending destruction, exact recovery rollback after an unrelated stack change, rollback notifications, and recovery persistence without a later tick. (Required regression tests 12-13)

## 4. Validation

- [x] 4.1 Run the focused `Hagalaz.Game.Scripts.Tests` trade suite and the relevant container tests to a clean exit.
- [x] 4.2 Run strict OpenSpec validation, a dependent build, and final diff/status review; report any unavailable distributed validation separately.
