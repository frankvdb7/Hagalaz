# Tasks

## 1. Callback-free storage and transaction lifecycle

- [x] 1.1 Remove publication delegates from `ItemContainer` constructors and `ItemContainerMutationBoundary`; verify callback-free standalone operations record only ChangeTracker batches.
- [x] 1.2 Keep committed transaction bindings through owner publication and release them from `Dispose`; add tests for owner-thread consumption, foreign readers waiting, rollback, and post-commit cleanup.

## 2. Domain-owned mutation and publication

- [x] 2.1 Make domain-owned item views read-only and add only the mutation commands required by production; verify consumers cannot mutate through `Items` and transaction participants remain accessible through public domain interfaces.
- [x] 2.2 Move Inventory, Bank, Reward, Familiar, Shop, Equipment, and MoneyPouch publication into those owners; verify event payloads, ordering, suppression, and owner-specific failure behavior.
- [x] 2.3 Migrate every direct production mutation through domain `Items` and explicit transaction owner; verify repository searches find no remaining domain mutation bypass.

## 3. Regression coverage and specifications

- [x] 3.1 Update/add Abstractions, GameWorld, and Scripts tests for tracker consumption, rollback, standalone commands, publication ordering/failure, Equipment lifecycle, MoneyPouch, Shop suppression, trade, and mutation bypass; verify affected test projects pass.
- [x] 3.2 Update canonical and active OpenSpec requirements to the callback-free lifecycle and archive only completed related changes after canonical sync; verify strict OpenSpec validation passes.

## 4. Integration validation

- [x] 4.1 Run the full solution build, requested test projects, C# duplication gate, and `git diff --check`; record command results.
- [x] 4.2 Review functional correctness, synchronization/transactions, and architecture against the full diff; remove any new callback mechanism, generic mutation gateway, or unrelated scope before completion.
