# Spec Delta

## Purpose

Defines the ownership boundary and correctness guarantees for item storage shared by gameplay containers, including atomic transfers and post-commit domain publication.

## MODIFIED Requirements

### Requirement: Containers compose one authoritative item store
Storage mechanics MUST be composed rather than inherited. `BaseItemContainer`, `TradeItemContainer`, `ITradeItemContainer`, `ItemContainerExtensions`, and generic forwarding wrappers MUST be removed. One concrete `ItemContainer` MUST be the single implementation of `IItemContainer` and MUST compose one `ItemContainerStorage`. Ordinary domain implementations MUST own a concrete `ItemContainer`; ordinary domain interfaces MUST expose it as `IItemContainer Items`, not as the concrete type. Ordinary domains MUST NOT implement `IItemContainer` or `IContainer` by forwarding generic operations. Equipment and MoneyPouch MUST compose `ItemContainerStorage` directly and MUST NOT expose a generic `Items` property. Equipment MUST itself remain a read-only `IContainer<IItem?>`. TradeOffer, Duel, Price Checker, and test fixtures MUST compose the real `ItemContainer` when they need normal generic behavior. Trade is a consumer of the generic synchronous mutation/transfer boundary and MUST NOT be modeled as a capability inherited or implemented by ordinary item containers. Inventory, bank, reward and other generic domain containers MUST NOT expose trade-specific mutation contracts merely because trade can move items through them. `IItemContainer` MUST NOT reference concrete `ItemContainer`; bulk movement MUST belong to `ItemContainerTransfer`. Domain-facing interfaces MUST NOT inherit or expose `IItemContainerStorageOwner`, raw storage, or equipment publication methods. `IItemContainer` and `IItemContainerStorageOwner` MUST contain declarations only. Composition MUST NOT be replaced by default-interface implementation inheritance; item-container interfaces define contracts only. `ItemContainerStorage` MUST own low-level slot and mutation mechanics and MUST NOT depend on character, trade, equipment, shop, UI, or persistence behavior. `ItemContainer` MUST invoke an optional simple publication callback only after committed generic mutations. Domain containers MUST retain ownership of specialized gameplay callbacks and orchestration. `IItemContainerStorageOwner` MUST expose only the composed storage and minimum publication operation needed by cross-container transfer coordination. Exact removal MUST be available through a neutral generic operation. Any staged MoneyPouch mutation APIs needed by transaction coordination MUST use domain-neutral exact-operation names.

#### Scenario: Domain mutation publishes committed slots
- **WHEN** a domain container successfully adds, removes, replaces, moves, swaps, sorts, clears, or restores items
- **THEN** the authoritative storage reflects the mutation before the container publishes its changed slots

#### Scenario: Rejected mutation leaves storage unchanged
- **WHEN** a single-container mutation or exact cross-container transfer cannot satisfy its quantity, capacity, stacking, or overflow rules
- **THEN** every affected storage retains its pre-operation slot contents and counts

#### Scenario: Generic exact removal is available to domain consumers
- **WHEN** a caller needs to remove an exact item quantity for payment or settlement
- **THEN** it uses the generic exact-removal contract and receives success or failure without a trade-specific container capability

## ADDED Requirements

### Requirement: Exact restoration validates before replacing state
`ItemContainerStorage` MUST own exact slot restoration validation and replacement. It MUST reject out-of-range slots and invalid counts with `ArgumentOutOfRangeException`, reject duplicate slots with `ArgumentException`, and leave the previous state unchanged if any input entry is invalid. Ordinary containers MUST reject zero counts; MoneyPouch MAY allow zero while validating its coin ID and physical slot at the domain boundary.

#### Scenario: Invalid restored data leaves the old contents intact
- **WHEN** restoration contains an invalid slot, duplicate slot, null item, or disallowed count
- **THEN** storage throws the established exception category and retains all previous slots, counts, and revision
