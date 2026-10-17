# Design

## Context

See `proposal.md` for motivation and scope. The current behavior contract is in `openspec/specs/item-container-storage/spec.md`. Storage algorithms remain synchronization-agnostic; storage mutation is eager, with rollback snapshots owned by the transaction.

## Goals / Non-Goals

**Goals:**

- Keep exactly one synchronization authority for each storage instance.
- Separate long-lived access ownership from one write operation.
- Let the lock set coordinate ordered physical monitor acquisition without learning lifecycle state.
- Preserve current standalone and transactional publication ownership.

**Non-Goals:**

- No public API changes, storage algorithm changes, new concurrency model, or domain behavior changes.
- No new coordinator, registration mechanism, callback system, or generic acquisition framework.

## Decisions

1. **Synchronization state has one owner.** `ItemContainerSynchronization` owns the monitor, lock order, transaction binding, standalone publication ownership, nested write count, and current-thread ownership accounting. `ItemContainerTransaction` remains the only component that transitions transaction state and binds/unbinds participants. Synchronization exposes focused access operations and does not implement item algorithms or invoke domain completion.

2. **A write is a scoped operation.** `ItemContainerWriteScope` captures the active transaction (if any), records the operation's changed slots, and releases its owned monitor/scope count. The outermost standalone scope retains logical publication ownership while the monitor is released, invokes the immutable domain completion owner, and then releases that logical ownership. No extra write-lock or publication abstraction is introduced.

3. **The lock set coordinates, synchronization authorizes.** `ItemContainerLockSet` orders synchronization owners by their stable lock identity and coordinates partial acquisition, release, retry, and waiting. It calls synchronization-owned acquisition/admission operations; it does not read transaction bindings or publication-owner fields and has no lifecycle-mode flags.

4. **The transaction is the rollback and multi-participant completion owner.** Participants resolve to unique synchronization owners in encounter order. The transaction acquires those owners through the lock set, captures storage snapshots, binds only after construction succeeds, and owns commit/rollback, tracker promotion/discard, and completion ordering. Physical locks are released before domain completion; logical transaction ownership remains until disposal.

5. **Tracker state stays in the tracker.** `ItemContainerChangeTracker` owns pending and committed batches. Its public reads go through its synchronization owner, which protects tracker access. The tracker never acquires multiple locks or invokes completion.

6. **Transfer orchestration remains one path.** Container/domain operations request transfers; one small internal container-level operation obtains access to both synchronization owners, calls the existing storage transfer algorithm, and records both trackers. Synchronization owns access policy only; storage owns the exact item algorithm.

7. **Completion ownership remains immutable.** Existing domain implementations continue to compose their completion owner with their storage synchronization. Completion methods are invoked by write-scope or transaction completion after physical unlock, never by storage or synchronization policy code.

## Risks / Trade-offs

- **Risk:** Moving transaction/publication binding can introduce a second source of truth. → Keep each binding field only in `ItemContainerSynchronization`, and let the transaction be the only writer of transaction bindings.
- **Risk:** Lock-set coordination can accidentally recreate access policy. → It may coordinate only outcomes from synchronization-owned access methods; it must not inspect lifecycle fields.
- **Risk:** Standalone completion ordering or tracker visibility can change during extraction. → Preserve owner order and add deterministic tests for nesting, foreign waiters/readers, completion failures, and rollback.
- **Risk:** A new helper could merely relocate the old boundary. → Keep transfer orchestration limited to acquiring both authorities, calling storage once, and recording both results; delete the old boundary rather than retaining forwarding wrappers.

## Migration Plan

1. Add the synchronization owner and migrate the lock set to synchronization-owned admission while retaining current behavior.
2. Extract the write-scope type and migrate ItemContainer and domain compositions.
3. Move shared transfer orchestration, tracker access, snapshots, and completion references to their designated owners; delete the old boundary.
4. Update architecture tests and canonical OpenSpec, then run focused tests, all requested validation, and the adversarial self-review.

Rollback is a local source revert; no persisted data or external schema changes are involved.
