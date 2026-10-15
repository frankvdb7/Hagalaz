# Farming — revision 742

**Repository assessment:** Existing patch/seed definitions and persisted patch state. Existing files are entry points, not evidence of feature completeness.

## Historical scope

Planting, compost, watering, growth stages, disease/death, harvesting and regeneration over real elapsed time.

## Hagalaz entry points

- `Hagalaz.Game.Scripts/Skills/Farming/FarmingSkillService.cs`
- `Hagalaz.Services.GameWorld/Model/Creatures/Characters/Farming.cs`
- `Hagalaz.Services.GameWorld/Logic/Dehydrators/FarmingDehydrator.cs`
- `Hagalaz.Services.GameWorld/Logic/Hydrators/FarmingHydrator.cs`
- `Hagalaz.Data/Entities/SkillsFarmingPatchDefinition.cs`

## Implementation path

1. Treat each character's patch lifecycle as authoritative state with persisted timestamps/stage; current scripts and hydrators are the owner. Apply elapsed-time catch-up on access/sign-in rather than creating one timer per patch.
2. Use patch/seed tables for required items and levels. Validate weather, growth cycle, compost, disease and yield against historical 742 observations before encoding exact rates.

## Regression acceptance

- Offline elapsed growth behaves deterministically; duplicate interactions cannot double harvest; patch state survives reconnect; incorrect seeds and full inventory leave state coherent.
- Verify recipe IDs, rates, object interactions and interface details against revision 742 fixtures; extend focused project tests instead of copying later behavior.

## Reference and uncertainty

- Exact level tables, success chances and asset/config IDs remain unverified until reference fixtures are captured.
- Apply [implementation conventions](implementation-conventions.md) for ownership, testability and durable state.
