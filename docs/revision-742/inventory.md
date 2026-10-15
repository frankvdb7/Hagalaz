# Inventory and item operations — revision 742

**Repository assessment:** Partial; established container abstractions. This is an implementation guide, not a claim of complete feature parity.

## Revision-specific behavior

Twenty-eight slot inventory, item identity, stackability, movement, examine, item-on-item operations, ground pickups and interactions with containers.

## Existing Hagalaz entry points

- `Hagalaz.Game.Abstractions/Collections/ItemContainer.cs`
- `Hagalaz.Game.Abstractions/Collections/ItemContainerTransaction.cs`
- `Hagalaz.Game.Scripts/Widgets/Tabs/InventoryTab.rsi.cs`
- `Hagalaz.Game.Scripts/Widgets/ItemWidgetOperations.cs`
- `Hagalaz.Game.Scripts.Tests/Widgets/InventoryTabItemUseRegressionTests.cs`

## Build approach

1. Resolve item definition and revision-specific widget operation first. Reuse `Mutations` and `ItemContainerTransaction` for cross-container changes; do not mutate slots outside the established boundary.
2. Preserve item instance/attributes when moving; enforce slot counts and stack limits. Keep use-on-item behavior in existing item/script dispatch, not a second generic click router.

## Acceptance and tests

- Exact-stack and nonstack moves, full-inventory pickup failures, item use, inventory reorder, cancel/interrupt and persistence all conserve item identities and quantities.
- Add focused tests near the cited code; check wire UI behavior with a revision 742 client/cache fixture before treating IDs or timings as exact.

## Evidence and open questions

- Exact historical values and component mappings remain fixture-gated; do not replace unknowns with later-client defaults.
- Read [implementation conventions](implementation-conventions.md) before modifying runtime state or persistence.


## Direct item identifiers from the repository

The [numeric item registration catalog](data/catalogs/item-script-registrations.csv) provides source-linked ID references for existing scripts, including checked cache group/file arithmetic. Treat these as script registrations only until direct 742 cache presence and item definition properties have been validated. See [item/NPC evidence methodology](data/catalogs/README.md).
