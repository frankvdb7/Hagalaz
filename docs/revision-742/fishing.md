# Fishing — revision 742

**Repository assessment:** Existing spot, tool and task implementation. Existing files are entry points, not evidence of feature completeness.

## Historical scope

Spot types, bait/feathers, fishing-tool requirement, timed catches, fish tables and spot movement where supported.

## Hagalaz entry points

- `Hagalaz.Game.Scripts/Skills/Fishing/FishingSkillService.cs`
- `Hagalaz.Game.Scripts/Skills/Fishing/FishingSpot.rsnpc.cs`
- `Hagalaz.Game.Scripts/Skills/Fishing/FishingTask.cs`
- `Hagalaz.Services.GameWorld/Services/FishingService.cs`

## Implementation path

1. Resolve actual spot NPC and click-option to definition before checking tools; reuse a single task to validate inventory, grant fish and consume bait.
2. Only relocate spots via their NPC/region owner and fixture-backed 742 movement rules; avoid generic random wandering for all fishing nodes.

## Regression acceptance

- Insufficient bait/tool and full inventory fail safely; selected method determines catch table; task cancels on despawn or walking away; catches and XP are delivered once.
- Verify recipe IDs, rates, object interactions and interface details against revision 742 fixtures; extend focused project tests instead of copying later behavior.

## Reference and uncertainty

- Exact level tables, success chances and asset/config IDs remain unverified until reference fixtures are captured.
- Apply [implementation conventions](implementation-conventions.md) for ownership, testability and durable state.
