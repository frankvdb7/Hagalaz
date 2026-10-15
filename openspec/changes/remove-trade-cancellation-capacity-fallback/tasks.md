# Tasks

## 1. Remove forced capacity recovery

- [x] 1.1 Route detach and ordinary cancellation through one refund path; remove `forceConservation` and Rewards/Bank fallback, then verify no production references to recovery remain.
- [x] 1.2 Remove recovery-only test cases and helper code that require an offer not funded from its owner's Inventory or MoneyPouch; retain normal refund and failed-completion coverage.

## 2. Reconcile behavior specification

- [x] 2.1 Apply the `trading-completion` delta and verify strict OpenSpec validation passes.
- [x] 2.2 Build the affected Game Scripts project and run `git diff --check`.
