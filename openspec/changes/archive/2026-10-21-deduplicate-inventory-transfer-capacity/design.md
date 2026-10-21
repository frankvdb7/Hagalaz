# Design

## Context

`BankContainer.WithdrawFromBank` and `RewardContainer.Claim` each inspect inventory stack compatibility, integer count overflow, and free slots before opening their existing transfer transaction. The character exposes its inventory through `IInventoryContainer.Items`, a read-only item-container view backed by the existing `ItemContainerStorage` capacity owner.

## Goals and Non-Goals

**Goals:**

- Give the existing item-storage composition one implementation of the count-aware capacity decision.
- Keep partial nonstack behavior and all caller-owned side effects at their existing call sites.
- Preserve bank note handling and both transfer transaction boundaries.

**Non-Goals:**

- Do not merge bank and reward workflows.
- Do not modify shop capacity rules or any transfer primitive.
- Do not add a helper class, strategy, or other transfer framework.

## Decisions

- Add `TryGetTransferableItemCount` to `IReadOnlyItemContainer`; implement it in `ItemContainerStorage` and forward it through `ItemContainer`.
- Treat `AlwaysStack`, stackable, and noted items as all-or-nothing stack transfers; require one free slot for a new stack and reject integer overflow for an existing stack.
- Permit partial counts for nonstackable transfers when some free slots remain.
- Return the selected transfer count and a flag indicating whether free slots limited the request. A false result with that flag set represents an inventory capacity failure; a false result without it represents stack-count overflow.
- Keep inventory-full messages, item count updates, and transaction handling in the existing bank and reward methods.
- Test the contract directly through `IReadOnlyItemContainer`, alongside existing caller-level transfer tests.

## Risks and Trade-offs

- The call still observes free slots before the transfer transaction, matching the existing order. This refactor does not add synchronization or change how a later transfer failure is handled.
- `HasSpaceFor` remains the all-or-nothing boolean query. The new count result is needed to preserve partial nonstack transfers and distinguish free-slot limits from stack overflow.
- Shop behavior is deliberately unchanged.

## Migration Plan

No persisted state or protocol changes. Rollback consists of restoring the existing checks in bank and reward containers and removing the item-container contract method and its tests.
