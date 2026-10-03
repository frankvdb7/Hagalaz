# Proposal

## Why

Inventory exposes its generic item container through composition, while Equipment directly implements a generic container interface. An explicit read-only item capability makes the shared query surface clear and prevents Equipment's domain contract from masquerading as a generic collection.

## What Changes

- **BREAKING** Add `IReadOnlyItemContainer` for indexed/enumerable item reads and item queries.
- Make `IItemContainer` inherit the new read-only interface while keeping mutation and transaction capabilities on `IItemContainer`.
- Change `IEquipmentContainer` to expose `IReadOnlyItemContainer Items` and retain its `EquipmentSlot` indexer and equipment-specific operations.
- Expose Equipment's existing storage through a minimal read-only projection and migrate generic equipment reads to `.Items`.
- Add interface-shape and read-projection regression tests.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `item-container-storage`: item containers share an explicit read-only item capability, and Equipment composes that view without exposing the generic container contract.

## Impact

Public interfaces in `Hagalaz.Game.Abstractions`, the Equipment implementation in GameWorld, generic equipment read call sites in GameWorld and Scripts, and focused abstraction/GameWorld tests. No dependencies or transaction behavior change.
