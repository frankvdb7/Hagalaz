# Spec Delta

## MODIFIED Requirements

### Requirement: Storage and trade revisions have distinct purposes

Storage MUST own its mutation revision, which invalidates active enumerators after committed storage changes. `ItemContainerLockSet` MUST own deterministic mutation-boundary lock ordering; `ItemContainerTransaction` MUST use that lock order for scope acquisition and finalization. `ItemContainerMutationBoundary.TryTransferTo` MUST require an existing caller-owned scope containing both boundaries. `TradingCharacterScript` MUST own terminal completion, refund, and forced recovery scopes, establish them before TradeExchange stages mutations, and set the terminal `TradeState` before `Commit()`; TradeExchange MUST NOT begin or commit those scopes. Terminal TradeExchange movement MUST use exact `IItemContainer.TryTransferTo(...)` for offered non-coin items and selected escrow recovery destinations inside the existing caller-owned transaction, and `IMoneyPouchContainer.TryTransferCoinsFrom(...)` for offered coins. It MUST NOT use standalone-capable destination `AddRange` or terminal offer `Clear` for authoritative escrow movement. Trade session owners MUST NOT acquire mutation-boundary locks directly. A trade offer's acceptance revision MUST reflect committed offer contents and invalidate prior acceptance independently of notification publication.

#### Scenario: Storage revisions are independent from deterministic lock ordering
- **WHEN** storage changes during a transaction involving multiple boundaries
- **THEN** storage advances its own mutation revision while `ItemContainerLockSet` orders synchronization by boundary lock order

#### Scenario: Committed offer revision is independent of publication
- **WHEN** offer storage changes in a committed transaction and notification publication throws
- **THEN** the offer revision reflects the committed storage change and previously recorded acceptance is invalid
