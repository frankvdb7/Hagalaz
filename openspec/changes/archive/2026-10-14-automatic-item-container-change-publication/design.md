# Design

## Context

The existing `ItemContainerChangeTracker`, mutation boundary, lock set, transaction, and completion-owner contract already own the needed state. Standalone domain mutations currently use explicit two-phase publication scopes, while transaction and transfer callers manually invoke domain publishers after mutation. See `proposal.md` and the `item-container-storage` delta for the behavior contract.

## Goals / Non-Goals

**Goals:**

- Complete registered owners from the outermost standalone mutation scope, transaction commit, and standalone transfer.
- Release physical locks before completion while retaining logical ownership until completion ends.
- Preserve domain-owned event/effect construction and ownerless tracker consumption.
- Remove redundant public publication APIs and caller orchestration.

**Non-Goals:**

- No new publication framework, publisher delegates, tick batching, standalone rollback snapshots, or changes to gameplay semantics.

## Decisions

1. **Extend the existing completion owner.** Add separate effects and publication phases, with no-op defaults for owners that do not need effects or rollback facts. Equipment keeps its local FIFO of typed effects; MoneyPouch keeps its local FIFO of immutable balance facts. This preserves domain ownership without turning the transaction into a callback registry.

2. **Register immutable owners at container construction.** Add internal owner-aware `ItemContainer` constructors for empty and initially populated storage. Inventory, Bank, Reward, Familiar Inventory, and Shop Stock use them; public constructors remain ownerless. Equipment and MoneyPouch retain their existing boundary registrations.

3. **Complete standalone scopes at outermost disposal.** While still holding the physical lock, the boundary claims its existing logical standalone ownership only when the tracker has committed changes and the outermost scope exits. It then releases the physical lock, invokes effects followed by publication, and releases logical ownership in `finally`. Nested scopes only contribute to the same tracked batch; rejected, suppressed, and no-op mutations do not trigger completion.

4. **Complete transactions after promotion and unlock.** Commit first becomes irreversible, promotes all boundary trackers, and releases all physical locks. It then invokes the reference-distinct completion owners in participant boundary order, first for effects and then for publication. Transaction bindings remain through completion until disposal. A callback exception propagates; storage stays committed and completion is not retried.

5. **Use the existing lock set for standalone transfer completion.** A successful transfer records both sides and, when at least one owner exists, claims logical publication ownership for both boundaries before releasing physical locks. Owners are deduplicated in source-then-destination encounter order. Finalization reacquires both boundaries in the existing lock order, clears both claims, and pulses waiters together. If neither side has an owner, the existing tracker-only path remains unchanged.

6. **Keep ordering structural and domain-owned.** Boundary resolution order remains publication order and lock acquisition remains `LockOrder`. MoneyPouch contributes Inventory before its pouch boundary so Inventory events precede pouch messages/events. Transaction effects form a separate first phase, ensuring Equipment lifecycle hooks precede Inventory and Equipment publication. No generic publisher error aggregation is added; Equipment retains attempt-all semantics for its own effects, and MoneyPouch retains its own FIFO/failure cleanup.

7. **Retain ownerless pull-based containers.** Raw temporary `ItemContainer` instances, including PriceChecker selections and trade-offer storage, have no completion owner and continue consuming tracker slots in their local domain flow. Initial full refreshes and separate gameplay events remain explicit.

## Risks / Trade-offs

- **A callback can throw after storage commits.** Commit remains irreversible and the exception propagates; boundary/transaction disposal releases retained logical ownership, and consumed event batches are not replayed.
- **Registration could publish during construction if constructors mutate tracked storage.** Use owner-aware construction only for the listed containers and keep hydration/initialization paths using restoration or suppressed tracking.
- **Participant order controls domain publication order.** Preserve existing participant order at multi-owner call sites and explicitly contribute Inventory before pouch storage.
- **Removing public publication methods affects tests and mocks.** Migrate callers to assert automatic events; keep explicit tracker consumption only for ownerless containers and initial UI refreshes.

## Migration Plan

Implement the core contract and scopes first and run Game Abstractions tests. Register each domain owner in the specified order, migrate Equipment and MoneyPouch completion, then Shop and caller orchestration. Update the canonical spec and tests, run direct project tests, solution build, strict OpenSpec validation, duplication checks, and diff checks. Revert the new commit if deployment reveals a regression; no data migration is required.
