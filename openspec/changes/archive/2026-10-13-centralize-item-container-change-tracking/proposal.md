# Proposal

## Why

Changed-slot accumulation is currently split between standalone mutation scopes and transactions, while PriceChecker rescans and sends every selection slot after each mutation. A boundary-owned tracker can provide one consumable source of committed changes and let PriceChecker send only affected slots.

## What Changes

- Add one committed/provisional `ItemContainerChangeTracker` to each mutation boundary and expose that same instance from concrete `ItemContainer`.
- Move mutation attribution, transaction promotion/rollback, and automatic publication consumption to the tracker without changing boundary synchronization or publication order.
- Change PriceChecker to consume tracked slots for incremental selection and price updates, while retaining a full initial refresh and its existing projected-inventory behavior.
- Keep explicitly suppressed mutations out of the tracker.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `item-container-storage`: define boundary-owned committed/provisional change tracking and pull-based PriceChecker updates.

## Impact

Affected code is limited to Game Abstractions item-container boundary, transaction, and concrete container types; PriceChecker and its tests; related Abstractions tests; and the item-container-storage specification. No dependency, general container interface, locking, transaction ownership, or domain publication API changes are intended.

## Acceptance Criteria

- A standalone successful mutation is retrievable from `items.ChangeTracker.GetSlots()` and each retrieval consumes only committed changes.
- Transaction changes remain invisible until commit; rollback discards only provisional changes and preserves earlier unconsumed changes.
- Existing automatic publication consumes tracker changes once before callbacks, and standalone transfer tracking/publication remains atomic and ordered.
- PriceChecker performs a full initial refresh and sends only tracked selection slots and their price configurations on subsequent changes.
- Focused and broader tests, solution build, strict OpenSpec validation, duplication checks, and `git diff --check` pass.

## Non-Goals and Stop Conditions

- Do not change `IItemContainer`, mutation signatures, transaction nesting/locking, automatic domain publication, or equipment/MoneyPouch completion behavior.
- Do not add result objects, tracking interfaces, event systems, snapshots, synchronization primitives, or a projected-inventory tracker.
- If correct tracking requires changing transaction ownership, publication ordering, or a domain's semantics, stop and record that as a separate proposal rather than expanding this change.
