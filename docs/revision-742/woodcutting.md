# Woodcutting — revision 742

**Repository assessment:** Existing tree and task implementation. Existing files are entry points, not evidence of feature completeness.

## Historical scope

Tree harvesting, hatchet requirements, logs/experience, object-state depletion, respawn and members' tree varieties.

## Hagalaz entry points

- `Hagalaz.Game.Scripts/Skills/Woodcutting/WoodcuttingSkillService.cs`
- `Hagalaz.Game.Scripts/Skills/Woodcutting/WoodcuttingTask.cs`
- `Hagalaz.Game.Scripts/Skills/Woodcutting/Tree.rsobj.cs`
- `Hagalaz.Services.GameWorld/Services/WoodcuttingService.cs`

## Implementation path

1. Reuse the existing tree definition/loot path, effective hatchet selection and interruptible task. Keep object state and respawn controlled by the map's object owner.
2. Separate per-player chopping chance from shared-tree depletion state. Derive timings and animation IDs from revision fixtures.

## Regression acceptance

- Tree can be shared where appropriate; no logs after depletion; wrong tier/full inventory block correctly; teleport, death or movement interrupts work without phantom rewards.
- Verify recipe IDs, rates, object interactions and interface details against revision 742 fixtures; extend focused project tests instead of copying later behavior.

## Reference and uncertainty

- Exact level tables, success chances and asset/config IDs remain unverified until reference fixtures are captured.
- Apply [implementation conventions](implementation-conventions.md) for ownership, testability and durable state.
