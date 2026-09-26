## Context

See proposal.md for motivation and scope. `BaseItemContainer` owns slot storage and the insertion rules already made atomic for `AddRange` by #434. It currently has no common mutation lock. `TradeItemContainer` adds its own stable lock order and checked add/remove helpers, so normal containers and trade containers do not share one synchronization boundary today.

## Goals / Non-Goals

**Goals:**

- Make the base container the single owner of a per-container mutation lock and stable order.
- Reuse the existing insertion rules to validate a complete destination state before either real container changes.
- Reuse one exact source-removal implementation for transfers and trade checked removal.
- Keep item instances in place when moving whole items; clone only a split stack or a transformed destination item.
- Invoke container callbacks only after both storage states and revisions are committed and after pair locks are released.
- Keep bank conversion/count policy, reward/familiar/UI policy, equipment validation, and equipment callbacks in their owning domains.

**Non-Goals:**

- A generic transaction or reusable mutation-plan framework. A transfer may use private temporary container representations, but those do not escape the operation.
- A multi-container transaction for equipment swaps, trade settlement, shop purchase, or payment.
- A new synchronization mechanism beside the existing trade lock-order concept.

## Decisions

1. **Put the transfer facade beside the container storage code.** A small synchronous operation accepts existing item-container interfaces and requires their implementations to use `BaseItemContainer`. A separate DI service, request hierarchy, or public mutation receipt would add an owner without adding a second required behavior.

2. **Use one lock and order per base container.** Move the lock/order source from `TradeItemContainer` into `BaseItemContainer`, keeping the trade-facing properties available. All base storage mutators and checked trade mutations use this same lock; pair operations acquire distinct locks by ascending order. This reuses the proven trade ordering instead of relying on object hash codes or adding another lock scheme.

3. **Validate both sides before touching stored item instances.** Under the ordered locks, compute exact source removals and apply the destination insertion rules to a temporary destination representation. Reuse the current range insertion algorithm for stack checks, free-slot consumption, and overflow. If validation succeeds, apply the planned slot/count changes to the original arrays, retain whole moved item references, advance both revisions, release locks, and then notify.

4. **Keep exact add/remove semantics shared with trade.** `AddRangeForTrade` remains a trade-facing checked operation because its observer behavior is trade-specific, but it uses the common range insertion code. `RemoveForTrade` delegates to the common exact-removal code. Trade settlement keeps its current multi-container lifecycle and snapshots; this change does not turn a two-container primitive into a settlement transaction.

5. **Keep partial-count and domain decisions outside the primitive.** Bank withdrawal and reward/familiar flows calculate their intended quantity before requesting it. Bank may provide a destination item when deposit/withdraw-as-note behavior transforms the item ID. Equipment owns eligibility and callbacks; the storage primitive does not call equipment scripts. Simple equipment moves use the primitive with an explicit equipment slot; replacement decisions and multi-item weapon/shield behavior stay in `EquipmentContainer`.

6. **Preserve observer exception behavior after commit.** Update callbacks run after the transfer is committed and locks are released. If a callback throws, the operation does not restore storage or turn the committed transfer into a capacity failure. Generic observer exceptions keep their existing propagation behavior; trade's existing best-effort handling remains local to trade notifications.

7. **Retain the legacy bulk helper as explicit best-effort movement.** `AddAndRemoveFrom` keeps its existing "move complete source items that fit" behavior by determining one exact quantity per source item and calling the common transfer operation. The helper does not offer hidden partial counts within a single item.

8. **Limit shop migration to the item leg.** Shop sales use the primitive to move the sold item from inventory into stock. Payment remains in the existing shop workflow; this change does not make payout, stock, and inventory one transaction or alter shop purchasing.

9. **Keep Price Checker contents reachable on close failure.** A Price Checker close guard attempts exact returns before the widget is detached. If inventory cannot accept the remaining contents, closing is refused and the widget stays attached so those items remain accessible.

## Risks / Trade-offs

- [Risk] A caller that bypasses the base mutation boundary could still mutate shared item objects concurrently. → Keep storage-changing base methods under the same lock and inspect derived overrides; current GameWorld container implementations either use base storage methods or their existing ordered trade/pouch boundary.
- [Risk] Cloning during preflight could lose item-specific data if an `IItem.Clone` implementation is incomplete. → Preserve original item references for whole-instance moves and add regressions for identity and serialized item data on split moves.
- [Risk] An observer can throw after the transfer has committed, so the caller may receive an exception while storage remains changed. → Preserve current callback exception rules and test the committed contents explicitly; never add storage rollback after notifications.
- [Risk] A broad storage lock can expose callback reentrancy deadlocks if callbacks run under it. → Release the locks before `OnUpdate` and retain deterministic ordering for every pair operation.

## Migration Plan

Implement and validate the Abstractions transfer boundary first. Migrate named GameWorld container operations and their domain-owned callbacks next, then migrate Price Checker and any direct bulk helper callers. Run focused transfer tests, affected project suites, strict OpenSpec validation, and the broader solution check required by the repository. Rollback is a source revert; no data migration is needed.
