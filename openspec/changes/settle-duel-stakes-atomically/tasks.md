# Tasks

## 1. Atomic duel settlement

- [x] 1.1 Add a `DuelStakeExchange` settlement operation that transfers both stakes to the winner under one item-container transaction; verify mixed item/coin success, capacity rejection, and repeated settlement with focused regression tests.
- [x] 1.2 Route `DuelArenaCombatScript` victory payout through `DuelStakeExchange` without changing its public constructor; verify the focused Game Scripts test project builds and existing combat-script tests pass.
- [x] 1.3 Keep the victory stage and trapdoor exit blocked until stake settlement succeeds; keep an active duel busy so Equipment access and familiar withdrawals cannot consume confirmation-checked capacity; verify the full duel stage flow still settles the stakes.
- [x] 1.4 Make duel confirmation account for MoneyPouch coin capacity and require inventory space only for coin overflow; verify pouch-only payouts with full inventories and reject overflow without inventory space.
- [x] 1.5 Build the confirmation capacity projection without aliasing live stake items; verify confirmation leaves both coin stakes unchanged and successful settlement conserves their total.

## 2. Behavior specification and validation

- [x] 2.1 Add the duel victory settlement behavior to the canonical item-container specification and verify the change delta matches it.
- [x] 2.2 Run focused duel stake tests, validate OpenSpec strictly, and run `git diff --check`.
