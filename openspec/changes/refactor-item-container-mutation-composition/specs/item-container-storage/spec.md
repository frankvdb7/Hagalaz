# Spec Delta

## Purpose

Defines the ownership boundary and correctness guarantees for item storage shared by gameplay containers, including atomic transfers and post-commit domain publication.

## MODIFIED Requirements

### Requirement: Containers compose one authoritative item store
Storage mechanics MUST be composed rather than inherited. `BaseItemContainer` and `TradeItemContainer` MUST be removed, and every domain container MUST directly own one `ItemContainerStorage` instance as its authoritative slot state. Storage mutation MUST be implemented once and MUST NOT depend on character, trade, equipment, shop, UI, or persistence behavior. `IItemContainer` MUST retain its normal operation surface as default delegations to one shared extension implementation, without declaring `OnUpdate`; the composed domain container/infrastructure owner MUST publish only after committed changes. The storage owner MAY implement the base `IContainer<IItem?>` read projection directly from its owned store. Checked trade operations MUST remain on `ITradeItemContainer` and delegate to the same shared operation layer. Domain containers MUST retain ownership of gameplay callbacks.

#### Scenario: Domain mutation publishes committed slots
- **WHEN** a domain container successfully adds, removes, replaces, moves, swaps, sorts, clears, or restores items
- **THEN** the authoritative storage reflects the mutation before the container publishes its changed slots

#### Scenario: Rejected mutation leaves storage unchanged
- **WHEN** a single-container mutation or exact cross-container transfer cannot satisfy its quantity, capacity, stacking, or overflow rules
- **THEN** every affected storage retains its pre-operation slot contents and counts

## ADDED Requirements

### Requirement: Exact restoration validates before replacing state
`ItemContainerStorage` MUST own exact slot restoration validation and replacement. It MUST reject out-of-range slots and invalid counts with `ArgumentOutOfRangeException`, reject duplicate slots with `ArgumentException`, and leave the previous state unchanged if any input entry is invalid. Ordinary containers MUST reject zero counts; MoneyPouch MAY allow zero while validating its coin ID and physical slot at the domain boundary.

#### Scenario: Invalid restored data leaves the old contents intact
- **WHEN** restoration contains an invalid slot, duplicate slot, null item, or disallowed count
- **THEN** storage throws the established exception category and retains all previous slots, counts, and revision
