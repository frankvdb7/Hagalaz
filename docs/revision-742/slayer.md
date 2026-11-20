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

Decoded from the pinned cache (`TheStormProject/data/Cache`, index 3, group 161, 51 components) with the Actinium 742 `IComponentDefinitions` loader (`if1` format). Text and right-click options per component:

| Component | Text | Options |
| --- | --- | --- |
| 14 / 15 | (button) | Learn / Buy |
| 19 | points value | |
| 23 | "Reassign current mission" | Reassign current task |
| 24 | "Permanently remove current task" | Permanently remove current task |
| 26 | "(30 points)" | Reassign current task |
| 27 | "(100 points)" | Permanently remove current task |
| 37-41 | blocked-task slots | Cancel Removal |

Components 23 (label) and 26 (cost text) both expose the "Reassign current task" option, with the 30-point cost on 26, so the pinned client can emit either. Hagalaz binds both to the same server-validated skip handler (`RewardsAssignmentScreenTests`). The 100-point block (24/27) is separate and unimplemented. Period servers (RuneNova 742 `ActionButtonHandler`, `interfaceId == 161`) agree: 23/26 reassign (30), 24/27 block (100).

The cache has no per-component "skip" wording distinct from "Reassign"; the 30-point action is the historical skip/cancel (no replacement assigned), as the Slayer points wiki describes.

## Reference and uncertainty

- Exact level tables, success chances and asset/config IDs remain unverified until reference fixtures are captured.
- Apply [implementation conventions](implementation-conventions.md) for ownership, testability and durable state.
