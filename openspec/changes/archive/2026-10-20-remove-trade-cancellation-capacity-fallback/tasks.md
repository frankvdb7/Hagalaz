# Tasks

## 1. Remove forced capacity recovery

- [x] 1.1 Route detach and ordinary cancellation through one refund path; remove `forceConservation` and Rewards/Bank fallback, then verify no production references to recovery remain.
- [x] 1.2 Remove recovery-only test cases and helper code that require an offer not funded from its owner's Inventory or MoneyPouch; retain normal refund and failed-completion coverage.

## 2. Reconcile behavior specification

- [x] 2.1 Apply the `trading-completion` delta and verify strict OpenSpec validation passes.
- [x] 2.2 Build the affected Game Scripts project and run `git diff --check`.

## 3. Preserve cancellation capacity during an active trade

- [x] 3.1 Reject a deferred GameFrame money-pouch withdrawal when the character is busy, before calling `MoveToInventory`.
- [x] 3.2 Replace the synthetic zero-capacity offer test with a regression that funds offers through the inventory click handler, attempts the GameFrame withdrawal, runs detach cleanup, and checks the real inventory and pouch dehydration payloads.
- [x] 3.3 Update the canonical trading-completion requirement and validate the OpenSpec change.
- [x] 3.4 Reject Orb and Summoning Tab Take BoB withdrawals during active trade, allow normal withdrawals from the familiar inventory screen, and cover both behaviors with regressions.
- [x] 3.5 Preserve interrupt-before-unequip for aura equipment clicks and test normal and cosmetic aura refunds during trade.

## 4. Keep pre-snapshot cleanup narrow

- [x] 4.1 Dispatch a script-specific `OnDetach()` callback before dehydration; keep general `OnDestroy()` callbacks at actual destruction and remove the duplicate-callback suppression state.
- [x] 4.2 Move trade and pre-combat duel refunds into their script detach callbacks without changing destruction cleanup or combat-stage settlement.
- [x] 4.3 Update lifecycle specifications and documentation, and test callback phase ordering plus trade and duel stake refunds before snapshot.
