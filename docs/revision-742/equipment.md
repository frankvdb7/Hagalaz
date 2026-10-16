# Equipment and tool belt — revision 742

**Repository assessment:** Equipment present; tool-belt widget is only a shell. This is an implementation guide, not a claim of complete feature parity.

## Revision-specific behavior

Equipment slots, skill/quest requirements, worn bonuses, two-handed weapon conflicts, auras and basic tools carried outside inventory.

## Existing Hagalaz entry points

- `Hagalaz.Game.Scripts/Widgets/EquipmentTab/EquipmentWindow.rsi.cs`
- `Hagalaz.Game.Scripts/Widgets/EquipmentTab/ToolBeltWindow.rsi.cs`
- `Hagalaz.Services.GameWorld/Services/EquipmentService.cs`
- `Hagalaz.Game.Abstractions/Collections/IEquipmentContainer.cs`
- `Hagalaz.Game.Scripts.Tests/Widgets/InventoryTabItemUseRegressionTests.cs`

## Build approach

1. Keep equipped item effects in equipment/character state and use container mutations for equip/unequip. The tool-belt widget currently has empty open/close methods; do not claim it implements tool storage.
2. For tools, establish a persistent owner and supported tool IDs from 742 definitions; have gathering skills request an effective tool from inventory/equipment/tool belt in one place. Do not create a second tool-specific inventory unless demonstrated necessary.

## Acceptance and tests

- Equipping with full inventory fails without loss; two-handed swaps are atomic; tool-belt tools apply only at valid skill tiers; logout restores stored tools; unsupported quest tools are excluded.
- Add focused tests near the cited code; check wire UI behavior with a revision 742 client/cache fixture before treating IDs or timings as exact.

## Evidence and open questions

- [Historical feature reference](https://wiki.darkan.org/Toolbelt)
- Read [implementation conventions](implementation-conventions.md) before modifying runtime state or persistence.

## Revision-742 source-backed equipment relationships

See [item relationship implementation dossier](reference/deep-item-relationships.md) and the [140 degradation stage records](data/catalogs/equipment-degradation-script-stages.csv). Explicit item note/lend/bought stack rules and source equipment requirements are explained there; no complete cache-wide variant graph has been decoded in this repository pass.
