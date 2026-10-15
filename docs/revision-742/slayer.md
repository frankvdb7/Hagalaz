# Slayer tasks and rewards — revision 742

**Repository assessment:** Existing task assignment and persistence; reward TODOs remain. Existing files are entry points, not evidence of feature completeness.

## Historical scope

Master assignment, eligible monster selection, kill attribution, task progression, streaks, points and equipment/reward unlocks.

## Hagalaz entry points

- `Hagalaz.Services.GameWorld/Model/Creatures/Characters/Slayer.cs`
- `Hagalaz.Services.GameWorld/Services/SlayerService.cs`
- `Hagalaz.Services.GameWorld/Logic/Hydrators/SlayerHydrator.cs`
- `Hagalaz.Game.Scripts/Skills/Slayer/SlayerMasterDialogue.rsi.cs`

## Implementation path

1. Use one authoritative task definition/assignment per character. Validate unlocks, task exclusions and killed NPC identity from existing repositories; persist counters via current hydrator/dehydrator.
2. The current Slayer model includes TODOs for reward coin/point multipliers. Research 742 streak rules before implementing rather than assuming recent values.

## Regression acceptance

- No off-task kills count; boss/variant matching is explicit; task completion and streak points award once; switching masters and relog cannot reset or duplicate an active task.
- Verify recipe IDs, rates, object interactions and interface details against revision 742 fixtures; extend focused project tests instead of copying later behavior.

## Reference and uncertainty

- Exact level tables, success chances and asset/config IDs remain unverified until reference fixtures are captured.
- Apply [implementation conventions](implementation-conventions.md) for ownership, testability and durable state.
