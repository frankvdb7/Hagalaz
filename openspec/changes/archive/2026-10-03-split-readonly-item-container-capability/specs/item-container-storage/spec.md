# Spec Delta

## MODIFIED Requirements

### Requirement: Containers compose one authoritative item store
Storage mechanics MUST be composed rather than inherited. `BaseItemContainer`, `TradeItemContainer`, `ITradeItemContainer`, `ItemContainerExtensions`, generic forwarding wrappers, `IItemContainerStorageOwner`, and `ItemContainerTransfer` MUST be removed. One concrete `ItemContainer` MUST implement `IItemContainer` and privately compose one `ItemContainerStorage` and one `ItemContainerMutationBoundary`, exposed as `IItemContainerMutationBoundary Mutations`. Ordinary domain implementations privately own concrete `ItemContainer`; ordinary domain interfaces MUST expose `IItemContainer Items` and MUST NOT copy generic forwarding operations. Equipment and MoneyPouch MUST compose storage directly with private mutation boundaries and MUST NOT expose raw storage boundaries. MoneyPouch MUST NOT expose generic `Items`; Equipment MUST expose a read-only `IReadOnlyItemContainer Items` view and retain its `EquipmentSlot` indexer. Equipment MUST NOT itself expose `IContainer<IItem?>` or generic integer indexing. TradeOffer, Duel, and Price Checker MUST compose the concrete `ItemContainer` where generic behavior is required. `IItemContainer` MUST NOT reference concrete `ItemContainer`; bulk movement MUST NOT be part of its contract. Exact and multi-container mutations MUST use `ItemContainerTransaction` through opaque participants obtained from `.Mutations`. Domain callers MUST NOT cast containers to recover storage infrastructure. `ItemContainerStorage` MUST own low-level slot and mutation mechanics and MUST NOT depend on character, trade, equipment, shop, UI, or persistence behavior. `ItemContainerTransaction` MUST own deterministic lock ordering, snapshots, rollback, changed-slot tracking, and post-commit publication. Domain containers MUST retain ownership of specialized gameplay callbacks and orchestration. Exact removal MUST be available through a neutral generic operation. MoneyPouch MUST perform pouch and inventory mutations through the same scope, with its participant contributing both storage boundaries; it MUST NOT expose the concrete storage boundary or keep a separate rollback path. Concrete storage and mutation-boundary implementations SHOULD remain assembly-internal implementation details where friend-assembly access is sufficient.

#### Scenario: Domain mutation publishes committed slots
- **WHEN** a domain container successfully adds, removes, replaces, moves, swaps, sorts, clears, or restores items
- **THEN** the authoritative storage reflects the mutation before the container publishes its changed slots

#### Scenario: Rejected mutation leaves storage unchanged
- **WHEN** a single-container mutation or exact cross-container transfer cannot satisfy its quantity, capacity, stacking, or overflow rules
- **THEN** every affected storage retains its pre-operation slot contents and counts

## ADDED Requirements

### Requirement: Read-only item access is an explicit capability
`IReadOnlyItemContainer` MUST expose indexed/enumerable item reads and item-specific queries without mutation or transaction participation. `IItemContainer` MUST inherit this capability and retain mutation members. `IEquipmentContainer` MUST compose an `IReadOnlyItemContainer Items` view and retain its equipment-slot indexer and semantic operations.

#### Scenario: Equipment exposes only a read-only composed view
- **WHEN** a caller accesses equipment through `IEquipmentContainer`
- **THEN** generic item reads and queries are available through `Items`, equipment-slot reads remain available through the `EquipmentSlot` indexer, and no generic mutation or transaction capability is exposed

#### Scenario: Equipment read view reflects its storage
- **WHEN** equipment contains an item
- **THEN** the read-only view reports the same capacity, slots, enumeration, ID lookup, counts, and containment as the equipment storage
