# Proposal

## Why

`BeginIfNeeded` makes transaction ownership depend on hidden thread-local storage bindings. Callers cannot tell whether their helper owns a transaction or is borrowing one, which adds nullable commits and behavior that changes according to the caller's ambient scope.

## What Changes

- Remove `ItemContainerTransaction.BeginIfNeeded` and all call sites.
- Make public standalone operations create and commit explicit transactions.
- Keep composable storage and money-pouch operations transaction-participating only; they require all storage to be enlisted in the caller-owned scope.
- Make `IItemContainerMutationBoundary.TryTransferTo` reject calls outside one active transaction containing both boundaries.
- Preserve normal domain rejection, rollback, publication, messages, and existing transaction lifecycle behavior.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `item-container-storage`: transaction ownership becomes explicit; helper operations never discover or join an ambient scope.

## Impact

Affected code includes the abstractions transaction boundary, GameWorld bank/pouch/equipment/familiar/reward/shop owners, script trade/duel owners, related tests, and the item-container storage OpenSpec behavior. No new package or transaction framework is needed.
