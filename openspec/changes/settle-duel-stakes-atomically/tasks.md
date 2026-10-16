# Tasks

## 1. Atomic duel settlement

- [x] 1.1 Add a `DuelStakeExchange` settlement operation that transfers both stakes to the winner under one item-container transaction; verify mixed item/coin success, capacity rejection, and repeated settlement with focused regression tests.
- [x] 1.2 Route `DuelArenaCombatScript` victory payout through `DuelStakeExchange` without changing its public constructor; verify the focused Game Scripts test project builds and existing combat-script tests pass.
- [x] 1.3 Keep the victory stage and trapdoor exit blocked until stake settlement succeeds; verify a capacity failure preserves both stake containers and a later retry can succeed.

## 2. Behavior specification and validation

- [x] 2.1 Add the duel victory settlement behavior to the canonical item-container specification and verify the change delta matches it.
- [x] 2.2 Run focused duel stake tests, validate OpenSpec strictly, and run `git diff --check`.
