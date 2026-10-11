# Tasks

## 1. Boundary ownership guard

- [x] 1.1 Add thread-affine ownership accounting and use nonblocking acquisition only when the current thread already owns storage; verify balanced counts on transaction begin/commit/rollback and standalone publication cleanup.
- [x] 1.2 Add deterministic contention regressions for the A/B publication cycle, ordinary foreign waiting, standalone transfer waiting, disjoint scopes, rollback, and publisher failure; run focused Abstractions tests.

## 2. Equipment replacement recovery

- [x] 2.1 Make occupied non-weapon/non-shield replacement restore the exact incoming item before returning or propagating failures that occur before equip; verify successful lifecycle behavior remains unchanged.
- [x] 2.2 Add Equipment tests for rejected/false/throwing unequip, unavailable destination, committed inventory publication failure, identity/extra data, recovery fallbacks, duplication, and lock-free script invocation; run focused GameWorld tests.

## 3. MoneyPouch command policy

- [x] 3.1 Reject caller-owned participation at the start of `AddFromInventory` and `MoveToInventory`, and document their standalone-only behavior; verify rejection precedes validation, messaging, and mutation.
- [x] 3.2 Add focused fully-enlisted and partially-enlisted MoneyPouch regressions and run MoneyPouch tests.

## 4. Empty clear semantics

- [x] 4.1 Make empty storage clear a no-op while preserving non-null zero-count sentinel behavior; add revision, publication, transaction, zero-capacity, and nonempty rollback tests and run storage/transaction tests.

## 5. Integration, specification, and PR text

- [x] 5.1 Review affected production call sites for blocking, nested transaction, publication-order, and command-participation regressions; update the item-container spec delta and canonical spec to match behavior.
- [x] 5.2 Run the full solution test suite (covering Trade, Duel, Bank, Shop, Familiar, Reward, Equipment, MoneyPouch, GameWorld, Scripts, and Abstractions), solution build, strict OpenSpec validation, duplication gate, and `git diff --check`.
- [x] 5.3 Update PR #516 description to accurately state implemented architecture and only verified validation results; verify locally that no commit or push occurred.
