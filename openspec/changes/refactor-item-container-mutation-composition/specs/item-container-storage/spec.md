# Spec Delta

## Purpose

Defines the ownership boundary and correctness guarantees for item storage shared by gameplay containers, including atomic transfers and post-commit domain publication.

## MODIFIED Requirements

### Requirement: Containers compose one authoritative item store
Storage mechanics MUST be composed rather than inherited. `BaseItemContainer`, `TradeItemContainer`, and `ItemContainerExtensions` MUST be removed. One concrete `ItemContainer` MUST implement `IItemContainer` and `ITradeItemContainer` and compose one `ItemContainerStorage`. Domain containers MUST compose `ItemContainer` and expose it through the narrow `Items` contract they require; they MUST NOT copy the generic container forwarding API. `IItemContainer`, `ITradeItemContainer`, and `IItemContainerStorageOwner` MUST contain declarations only. Composition MUST NOT be replaced by default-interface implementation inheritance; item-container interfaces define contracts only. `ItemContainerStorage` MUST be the single implementation of generic mutation algorithms and MUST NOT depend on character, trade, equipment, shop, UI, or persistence behavior. `ItemContainer` MUST invoke an optional simple publication callback only after committed generic mutations. Domain containers MUST retain ownership of specialized gameplay callbacks and orchestration. `IItemContainerStorageOwner` MUST expose only the composed storage and minimum publication operation needed by cross-container transfer coordination.

#### Scenario: Domain mutation publishes committed slots
- **WHEN** a domain container successfully adds, removes, replaces, moves, swaps, sorts, clears, or restores items
- **THEN** the authoritative storage reflects the mutation before the container publishes its changed slots

#### Scenario: Rejected mutation leaves storage unchanged
- **WHEN** a single-container mutation or exact cross-container transfer cannot satisfy its quantity, capacity, stacking, or overflow rules
- **THEN** every affected storage retains its pre-operation slot contents and counts

#### Scenario: Interface dispatch reaches the concrete domain container
- **WHEN** a caller invokes an item-container operation through `IItemContainer` or a checked trade operation through `ITradeItemContainer`
- **THEN** normal interface dispatch invokes the concrete container implementation, including any domain-specific behavior

## ADDED Requirements

### Requirement: Exact restoration validates before replacing state
`ItemContainerStorage` MUST own exact slot restoration validation and replacement. It MUST reject out-of-range slots and invalid counts with `ArgumentOutOfRangeException`, reject duplicate slots with `ArgumentException`, and leave the previous state unchanged if any input entry is invalid. Ordinary containers MUST reject zero counts; MoneyPouch MAY allow zero while validating its coin ID and physical slot at the domain boundary.

#### Scenario: Invalid restored data leaves the old contents intact
- **WHEN** restoration contains an invalid slot, duplicate slot, null item, or disallowed count
- **THEN** storage throws the established exception category and retains all previous slots, counts, and revision
