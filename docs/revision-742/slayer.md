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

## Assignment screen (interface 161) component evidence

Evidence is a period 742 server, not a pinned-cache fixture: `RuneNova 742` (`Workspace/RuneNova 742/src/org/rse/network/packets/incoming/handlers/ActionButtonHandler.java`, `interfaceId == 161`) and `.../model/skills/Slayer.java`.

| Component | 742 behavior |
| --- | --- |
| 14 / 15 | Learn rewards / Buy rewards tabs |
| 19 | Slayer points text |
| 23 (alias 26) | `reassignTask`: needs a task and >= 30 points, debits 30, clears the task ("You cancel your Slayer task."), no replacement assigned |
| 24 (alias 27) | `addRemoval` (block): needs a task and >= 100 points, 5 slots |
| 37-41 | Cancel a blocked-task slot |

Component 23 is therefore the 30-point skip/cancel control, matching the Slayer points wiki. The client widget label itself (CS2/interface definition from the pinned cache) is still not decoded; Hagalaz only handles 23, not alias 26.

## Reference and uncertainty

- Exact level tables, success chances and asset/config IDs remain unverified until reference fixtures are captured.
- Apply [implementation conventions](implementation-conventions.md) for ownership, testability and durable state.
