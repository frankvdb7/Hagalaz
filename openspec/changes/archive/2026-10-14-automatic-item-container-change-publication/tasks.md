# Tasks

## 1. Core completion lifecycle

- [x] 1.1 Extend the internal completion-owner contract and add owner-aware internal `ItemContainer` constructors; verify public constructors remain ownerless and the Game Abstractions project builds.
- [x] 1.2 Make outermost standalone mutation disposal retain logical ownership, release the physical lock, invoke the two owner phases once, and clean up in `finally`; add focused tests for batching, no-op, lock state, and contention.
- [x] 1.3 Make transaction Commit promote every tracker, unlock, then invoke distinct owners in separate phases; test automatic completion, rollback, owner ordering, unchanged owners, and callback failure.
- [x] 1.4 Complete registered owners after standalone transfers while retaining both boundaries through publication; test both-owner, one-owner, ownerless, failure, lock-release, and contention cases.

## 2. Ordinary domain owners

- [x] 2.1 Register Inventory, Bank, Reward, and Familiar Inventory as owners; move their existing events into owner publication and remove redundant calls, verifying focused GameWorld tests for event counts and rollback.
- [x] 2.2 Register Shop Stock and preserve stock/sample event behavior and suppression; verify shop buy, sell, normalization, and shared-viewer tests.

## 3. Specialized completion owners

- [x] 3.1 Move Equipment lifecycle effects and equipment state publication into the effects/publication phases, preserving attempt-all effects and restoration semantics; verify Equipment tests including ordering and rollback.
- [x] 3.2 Make MoneyPouch contribute Inventory before pouch storage and publish its FIFO facts through its owner phase; verify overflow, FIFO, rollback, and inventory-before-pouch tests.

## 4. Call-site and API migration

- [x] 4.1 Remove redundant manual publication orchestration for registered owners in Bank, Reward, Familiar Inventory, Trade, Duel, Shop, and other callers; retain ownerless trade-offer tracker consumption and separate gameplay events; verify affected tests.
- [x] 4.2 Remove obsolete public `PublishChanges()` members from ordinary domain interfaces and implementations while retaining ownerless tracker consumption and initial full refreshes; verify no production callers of the removed APIs remain.

## 5. Specification and integration validation

- [x] 5.1 Update the canonical item-container spec and relevant documentation for automatic completion, lock ownership, ownerless pull tracking, domain ordering, and deferred tick batching; verify no stale manual-publication contract remains.
- [x] 5.2 Run Abstractions, GameWorld, Game Scripts, directly affected tests, solution build, strict OpenSpec validation, duplication/quality checks, and `git diff --check`; fix failures before commit.
