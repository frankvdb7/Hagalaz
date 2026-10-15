# Hunter — revision 742

**Repository assessment:** No dedicated scripts/services found in audited tree. Existing files are entry points, not evidence of feature completeness.

## Historical scope

Traps, tracking, bird/snare and creature capture, equipment placement and timed successes, limited to 742-available content.

## Hagalaz entry points

- `Hagalaz.Services.GameWorld/Services/NpcService.cs`
- `Hagalaz.Services.GameWorld/Services/GameObjectService.cs`
- `Hagalaz.Game.Abstractions/Tasks/GameLoopSynchronizationContext.cs`

## Implementation path

1. Identify trap items, target NPCs, map locations and animation/config IDs in the 742 cache first. Implement one trap family with an object script and existing creature task scheduling.
2. Ensure ownership of placed traps and reward pickup is explicit; reuse ground-item handling. Add remaining methods through specific trap scripts, not a speculative trap framework.

## Regression acceptance

- Multiple characters cannot claim one catch; logout and despawn clean up traps; placement respects object collision/capacity; catch only grants XP and loot once.
- Verify recipe IDs, rates, object interactions and interface details against revision 742 fixtures; extend focused project tests instead of copying later behavior.

## Reference and uncertainty

- Exact level tables, success chances and asset/config IDs remain unverified until reference fixtures are captured.
- Apply [implementation conventions](implementation-conventions.md) for ownership, testability and durable state.
