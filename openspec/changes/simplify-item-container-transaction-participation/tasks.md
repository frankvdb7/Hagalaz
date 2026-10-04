# Tasks

- [x] 1. Simplify ordinary item-container participation
  - [x] 1.1 Remove `EnsureOutsideTransaction` from the boundary contract, implementation, and ordinary `ItemContainer` operations; retain low-level mutation access and automatic change notification.
  - [x] 1.2 Reduce `IItemContainerMutationBoundary` and its implementation to participant plumbing and `TryTransferTo`.
  - [x] 1.3 Migrate ordinary mutation calls in Trade, Duel, Bank, Shop, Reward, Familiar, Equipment, MoneyPouch, and fixtures to normal container APIs; preserve transfer calls and transaction participant lists.
- [x] 2. Align MoneyPouch and Equipment participation
  - [x] 2.1 Replace the public MoneyPouch mutation boundary with opaque participant exposure and make public exact operations participate in complete active scopes or own a scope when unbound.
  - [x] 2.2 Make MoneyPouch inventory changes use normal container APIs and migrate all production and test callers.
  - [x] 2.3 Allow the specified simple Equipment mutations to participate in an active scope while preserving immediate unbound and deferred bound completion.
- [x] 3. Replace obsolete coverage and specify the contract
  - [x] 3.1 Replace tests that expect ordinary mutation rejection with automatic participation, rollback, unenlisted mutation, and wrong-thread behavior coverage.
  - [x] 3.2 Update MoneyPouch and Equipment behavior/API tests and remove tests for deleted boundary operations.
  - [x] 3.3 Update canonical item-container specification and audit stale terminology/references.
- [x] 4. Validate the refactor
  - [x] 4.1 Run Abstractions, GameWorld, and Scripts test projects plus the full solution build.
  - [x] 4.2 Run strict OpenSpec validation, jscpd, and `git diff --check`; report exact local results and leave all work uncommitted.
