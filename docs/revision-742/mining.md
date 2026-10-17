# Mining — revision 742

**Repository assessment:** Existing object, task and definition seams. Existing files are entry points, not evidence of feature completeness.

## Historical scope

Rocks require suitable pickaxe/skill, play gather animation/ticks, award ore/experience, deplete and later respawn. Tool-belt compatibility needs 742 verification.

## Hagalaz entry points

- `Hagalaz.Game.Scripts/Skills/Mining/MiningRocks.rsobj.cs`
- `Hagalaz.Game.Scripts/Skills/Mining/MiningTask.cs`
- `Hagalaz.Services.GameWorld/Services/MiningService.cs`
- `Hagalaz.Data/Entities/SkillsMiningRockDefinition.cs`

## Implementation path

1. Model rock identity and ore data through existing repositories; initiate one interruptible mining task per actor; use effective-tool selection shared with equipment/tool belt.
2. Use map-object mutation/respawn through existing game-object and region services, not independently created timers per rock.

## Regression acceptance

- Wrong level/tool fails without ore; inventory-full cancels without spawning items; several miners cannot deplete a node twice; cooldown and resumed rock identity survive a region refresh.
- Verify recipe IDs, rates, object interactions and interface details against revision 742 fixtures; extend focused project tests instead of copying later behavior.

## Reference and uncertainty

- Exact level tables, success chances and asset/config IDs remain unverified until reference fixtures are captured.
- Apply [implementation conventions](implementation-conventions.md) for ownership, testability and durable state.
