## Context

See proposal.md for motivation and scope. `BaseItemContainer` owns slot storage and the insertion rules already made atomic for `AddRange` by #434. It currently has no common mutation lock. `TradeItemContainer` adds its own stable lock order and checked add/remove helpers, so normal containers and trade containers do not share one synchronization boundary today.

## Goals / Non-Goals

**Goals:**

- Make the base container the single owner of a per-container mutation lock and stable order.
- Reuse the existing insertion rules to validate a complete destination state before either real container changes.
- Reuse one exact source-removal implementation for transfers and trade checked removal.
- Keep item instances in place when moving whole items; clone only a split stack or a transformed destination item.
- Separate storage mutation from container-change publication for composed operations, and publish only after all related storage is stable and required equipment effects have run.
- Keep bank conversion/count policy, reward/familiar/UI policy, equipment validation, and equipment callbacks in their owning domains.

**Non-Goals:**

- A generic transaction or reusable mutation-plan framework. A transfer may use private temporary container representations, but those do not escape the operation.
- A reusable multi-container transaction for equipment swaps, shop purchases, or payment. Existing trade, Money Pouch, and equipment operations may use explicit storage-only steps within their existing domain methods.
- A new synchronization mechanism beside the existing trade lock-order concept.

## Decisions

1. **Put the transfer facade beside the container storage code.** A small synchronous operation accepts existing item-container interfaces and requires their implementations to use `BaseItemContainer`. A separate DI service, request hierarchy, or public mutation receipt would add an owner without adding a second required behavior.

2. **Use one lock and order per base container.** Move the lock/order source from `TradeItemContainer` into `BaseItemContainer`, keeping the trade-facing properties available. All base storage mutators and checked trade mutations use this same lock; pair operations acquire distinct locks by ascending order. This reuses the proven trade ordering instead of relying on object hash codes or adding another lock scheme.

3. **Validate both sides before touching stored item instances.** Under the ordered locks, compute exact source removals and apply the destination insertion rules to a temporary destination representation. Reuse the current range insertion algorithm for stack checks, free-slot consumption, and overflow. If validation succeeds, apply the planned slot/count changes to the original arrays, retain whole moved item references, and advance both revisions. The public standalone transfer then releases locks and publishes both changed containers. A protected storage-only entry point reuses the same planner and commit for equipment, which runs its domain effect before publishing.

4. **Keep checked trade mutations explicit.** Trade containers expose checked storage-only add/remove methods that return changed slots. Existing standalone checked methods compose storage mutation with immediate publication. Trade settlement, refund, conservation, and offer coin movement use the storage-only methods while holding their existing ordered locks, including each participating Money Pouch, restore all base-container snapshots storage-only on a checked failure, release locks, and only then publish the final or restored state.

5. **Keep partial-count and domain decisions outside the primitive.** Bank withdrawal and reward/familiar flows calculate their intended quantity before requesting it. Bank may provide a destination item when deposit/withdraw-as-note behavior transforms the item ID. Equipment owns eligibility and effects; the storage primitive does not call equipment scripts. Equipment uses the protected storage-only transfer for a move, runs `OnEquipped` or `OnUnequipped`, then publishes the changed containers. Replacement decisions and multi-item weapon/shield behavior stay in `EquipmentContainer`.

6. **Publish after the operation is stable.** A composed operation performs all storage mutations and required domain effects before change publication. Publication and domain exceptions propagate; they do not roll committed storage back. No publication runs under the ordered multi-container locks.

7. **Retain the legacy bulk helper as explicit best-effort movement.** `AddAndRemoveFrom` keeps its existing "move complete source items that fit" behavior by determining one exact quantity per source item and calling the common transfer operation. The helper does not offer hidden partial counts within a single item.

8. **Limit shop migration to the item leg.** Shop sales use the primitive to move the sold item from inventory into stock. Payment remains in the existing shop workflow; this change does not make payout, stock, and inventory one transaction or alter shop purchasing.

9. **Keep Price Checker non-owning.** Inventory remains authoritative; Price Checker selections are clones. Closing or disconnecting discards selections without losing items.

10. **Keep widget close unconditional and retain the batch fix.** `CloseAll()` snapshots the open widgets before recursively closing their trees. Replacement callers inspect the resulting widget state when close callbacks open another frame.

11. **Reject impossible non-stackable unit expansion before cloning.** For a transfer shape that expands a quantity into per-unit non-stackable items, use existing slot and stackability facts to reject only requests that cannot fit. Do not add a quantity cap or replace the normal insertion algorithm.

12. **Let the public exact transfer own only its standalone atomic boundary.** `IItemContainer.TryTransferTo(...)` resolves destination capability, then uses current-thread lock ownership to choose among owning a short transaction when neither storage is held, participating in the strict existing transaction when both are held, or rejecting partial participation. The internal mutation boundary remains transaction-required; the existing storage transfer algorithm and explicit larger domain transactions are unchanged.

## Risks / Trade-offs

- [Risk] A caller that bypasses the base mutation boundary could still mutate shared item objects concurrently. → Keep storage-changing base methods under the same lock and inspect derived overrides; current GameWorld container implementations either use base storage methods or their existing ordered trade/pouch boundary.
- [Risk] Cloning during preflight could lose item-specific data if an `IItem.Clone` implementation is incomplete. → Preserve original item references for whole-instance moves and add regressions for identity and serialized item data on split moves.
- [Risk] Publication or an equipment domain effect can fail after storage commits. → Let the exception propagate and keep the final committed storage in place; do not publish between storage legs or roll back because publication/effects failed.
- [Risk] A broad storage lock can expose publication reentrancy deadlocks if publication runs under it. → Release the locks before `OnUpdate` and retain deterministic ordering for every pair operation.

## Migration Plan

Implement and validate the Abstractions transfer boundary first. Migrate named GameWorld container operations and their domain-owned callbacks next, then migrate Price Checker and any direct bulk helper callers. Run focused transfer tests, affected project suites, strict OpenSpec validation, and the broader solution check required by the repository. Rollback is a source revert; no data migration is needed.
