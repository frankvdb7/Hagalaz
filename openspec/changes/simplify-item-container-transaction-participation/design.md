# Design

`IItemTransactional` is an empty public capability marker. `IItemContainer` and `IMoneyPouchContainer` inherit it; neither exposes mutation storage or transaction internals. The marker is intentionally not sufficient to implement a transaction source: `ItemContainerTransaction` accepts only repository domain objects that also implement the internal `IItemTransactionSource` bridge.

`IItemTransactionSource` exposes the internal boundaries contributed by an aggregate. `ItemContainer` contributes its one boundary. `MoneyPouchContainer` contributes pouch storage and every inventory storage it requires. Transaction resolution validates and deduplicates all contributions before locking, then keeps the existing deterministic lock ordering, snapshots, binding, rollback-on-dispose, irreversible commit point, unlock-before-completion, and publication ordering.

Callers pass domain objects directly to `ItemContainerTransaction.Begin(...)`. Ordinary mutations continue using their existing container APIs. `IItemContainer.TryTransferTo` remains the public transfer operation and resolves the destination through `ResolveSingleBoundary`; it does not create a transaction or enlist missing storage. Equipment composes the internal source contract directly, while `IEquipmentContainer` remains unchanged.

No transaction semantics, domain operation ownership, or failure behavior changes in this API cleanup. Tests cover direct participants, interface-decorated transfer destinations, pouch multi-storage contribution, and the existing rollback/publication cases.
