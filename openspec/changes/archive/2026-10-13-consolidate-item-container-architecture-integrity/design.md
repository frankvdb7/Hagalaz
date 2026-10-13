# Design

## Context

See `proposal.md` for motivation and `specs/item-container-storage/spec.md` for observable requirements. The current implementation has ordered multi-boundary acquisition in both transaction startup and standalone transfer, but each path owns its own retry and release loop. Standalone `MutationScope` only enters thread ownership accounting when it records publication. Equipment's interactive replacement runs an inventory removal, a script command, then an equipment insertion and tries to recover the incoming object after failures. Shop normalization edits item references returned by a shallow snapshot and indexes original stock by current slot.

## Goals / Non-Goals

**Goals:** Keep one source of truth for ordered multi-boundary locking; preserve ordinary top-level waiting and reject nested contention; make the Equipment replacement operation all-or-nothing before commit; preserve current domain publication order; correct total removal and stock-baseline behavior.

**Non-Goals:** Add a general transaction coordinator, ambient context, callback registry, async transaction, compensation system, deep item snapshot, or public lock/transaction internals.

## Decisions

### One lock-set implementation

Add one internal `ItemContainerLockSet` under `Hagalaz.Game.Abstractions.Collections`. It sorts boundaries by `LockOrder`, acquires in ascending order, tracks the acquired prefix, releases in reverse, and supports normal acquisition plus owner-finalization reacquisition. Independent callers may wait; a caller already owning storage only uses nonblocking acquisition. On a logical-owner conflict, the implementation releases the entire prefix before waiting and retries from the start. `ItemContainerTransaction`, standalone two-boundary transfer, and finalization use this implementation. The single-boundary `MutationScope` stays allocation-free and follows the same ownership policy directly.

The lock set counts thread ownership from the first physical monitor acquisition until the set releases its last acquired monitor. Standalone single-boundary mutation similarly counts physical ownership, then transfers that protection to retained publication ownership before unlocking if changes were recorded. Transaction binding remains boundary-local and remains the only transaction-discovery mechanism.

### Preserve eager snapshots and existing publication semantics

Transactions keep their current live-storage mutation and rollback snapshots. The lock set acquires every boundary before snapshots are taken, and bindings are installed only after all snapshots succeed. Resolved participant order remains publication/completion order; only physical acquisition uses `LockOrder`. The commit point and post-commit exception behavior remain unchanged.

### Keep aura interaction in the existing equipment-script entry point

`AuraEquipmentScript.EquipItem` owns the active-aura confirmation dialog. It captures the currently equipped aura, returns without mutation while waiting, and delegates to `EquipmentScript.EquipItem` only after acceptance if that same aura is still equipped. The Yes/No dialogue consumes and clears its callback before invocation and clears it when closed. No transaction is held across UI interaction. The delegated call reaches `EquipmentContainer.EquipItem`, which keeps eligibility checks and the exact-instance inventory/equipment swap inside its existing transaction. Repeated or concurrent delivery cannot duplicate the incoming item because that operation revalidates storage under transaction locks. Existing direct unequip behavior continues using `UnEquipItem`.

### Correct removal and stock normalization at their owners

`ItemContainerStorage.Remove` tracks one remaining total quantity across matching stacks, while `TryRemoveExact` remains all-or-nothing. Caller audit changes only operations that require exact consumption, especially ground drops.

`ShopStockContainer` stores original baseline counts aggregated by item ID with checked overflow. `NormalizeStock` plans from a snapshot without mutating shared items, clones only items whose count changes, applies one replacement under the existing mutation scope, and sorts the planned state without changing unrelated object identities. Failure before replacement leaves live storage and revision untouched.

### Report real sorting changes

`ItemContainerStorage.Sort` reports whether slot positions changed. `ItemContainer.Sort` records full-container publication only when it did. Empty clear and the non-null zero-count sentinel keep their established behavior.

### Keep preflight reads narrow

Do not lock all getters. Revalidate only concrete decision inputs found by the requested audit. Mutation-time validations belong inside the already-required transaction or direct multi-boundary operation; script eligibility and interactive behavior remain outside physical mutation locks.

## Risks / Trade-offs

- [Risk] Lock sharing changes subtle contention behavior → Keep the current owner model, add deterministic thread-interleaving tests for independent waits and nested rejection, and review every Monitor operation.
- [Risk] Aura confirmation can arrive after the equipped aura changes → Capture the original instance and continue only while it remains equipped; the Equipment transaction revalidates item identity under locks.
- [Risk] Copy-on-write normalization changes identity for stock items whose count changes → Preserve identity for unchanged items and update tests/callers that incorrectly retain mutable normalized instances.
- [Risk] Aggregated duplicate original stock can overflow → Validate construction and fail before the container is exposed.

## Migration Plan

There is no persisted format change. Keep public container operations compatible except for the single replacement-approval script capability. Update every `IEquipmentScript` implementation and test substitute, then migrate recovery-based Equipment tests to atomic-conservation assertions. Archive this change only after focused and full validation pass and the canonical spec reflects the behavior.
