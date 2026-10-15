# Dungeoneering expeditions — revision 742

**Repository assessment:** No dedicated dungeon-session feature found. Existing files are entry points, not evidence of feature completeness.

## Historical scope

Party formation, instanced floors, doors/keys, binding, floor completion, experience/token awards and temporary equipment/tool belt.

## Hagalaz entry points

- `Hagalaz.Services.GameWorld/Services/MapRegionService.cs`
- `Hagalaz.Services.GameWorld/Services/CharacterService.cs`
- `Hagalaz.Game.Scripts/Widgets/EquipmentTab/ToolBeltWindow.rsi.cs`

## Implementation path

1. Start with a dated 742 floor/party fixture and map-instance viability analysis. Separate temporary dungeon inventory/keys from normal character equipment and preserve party ownership across disconnects.
2. Implement one fixed floor and boss/reward path before procedural room generation. Existing game tick, NPC spawning and map collision should remain authoritative.

## Regression acceptance

- Floor exit never exports temporary assets; completion rewards exactly once; party permissions are enforced; logout leaves character and durable items in a safe state.
- Verify recipe IDs, rates, object interactions and interface details against revision 742 fixtures; extend focused project tests instead of copying later behavior.

## Reference and uncertainty

- [Period reference](https://wiki.darkan.org/Update%3ADungeoneering_and_Shop_Improvements)
- Apply [implementation conventions](implementation-conventions.md) for ownership, testability and durable state.
