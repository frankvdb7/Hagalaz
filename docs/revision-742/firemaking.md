# Firemaking and bonfires — revision 742

**Repository assessment:** Existing ordinary log task; bonfire parity unverified. Existing files are entry points, not evidence of feature completeness.

## Historical scope

Log lighting plus communal bonfires were in scope by mid-2012; distinguish floor-fire placement from adding logs to an existing fire.

## Hagalaz entry points

- `Hagalaz.Game.Scripts/Skills/Firemaking/FiremakingTask.cs`
- `Hagalaz.Game.Scripts/Skills/Firemaking/StandardLog.cs`
- `Hagalaz.Services.GameWorld/Services/FiremakingService.cs`
- `Hagalaz.Data/Entities/SkillsFiremakingDefinition.cs`

## Implementation path

1. Reuse log definitions, animation and item consumption from normal firemaking. Check if bonfire object interaction is already wired; if absent, implement through existing object scripting and task scheduling.
2. Use one XP and log-consumption rule set with explicit recipe/method differences. Preserve shared bonfire lifetime in the world-object owner.

## Regression acceptance

- Lighting requires valid tile, level and item; adding to fire does not spawn another fire; multiple players do not interfere with each other's inventory; extinguished fires reject additions.
- Verify recipe IDs, rates, object interactions and interface details against revision 742 fixtures; extend focused project tests instead of copying later behavior.

## Reference and uncertainty

- [Period reference](https://wiki.darkan.org/Free-to-play)
- Apply [implementation conventions](implementation-conventions.md) for ownership, testability and durable state.
