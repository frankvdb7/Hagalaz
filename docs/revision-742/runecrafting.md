# Runecrafting altars — revision 742

**Repository assessment:** Existing altar/ruins/portal scripts and recipe service. Existing files are entry points, not evidence of feature completeness.

## Historical scope

Rune essence conversion at altars, talisman/tiara/portal access, level and multiplier rules, pouches and rune outputs.

## Hagalaz entry points

- `Hagalaz.Game.Scripts/Skills/Runecrafting/Runecrafting.cs`
- `Hagalaz.Game.Scripts/Skills/Runecrafting/Altar.rsobj.cs`
- `Hagalaz.Services.GameWorld/Services/RunecraftingService.cs`
- `Hagalaz.Data/Entities/SkillsRunecraftingDefinition.cs`

## Implementation path

1. Reuse altar and ruins object scripts; map each altar's rune recipe and level into database definitions. Check standard/pure essence and output stack space as one transaction.
2. Keep region transitions in existing map/teleport service, not new instance logic. If essence pouch support is missing, document separately before extending persistence.

## Regression acceptance

- Access gates are correct; crafted runes reflect level multiplier without consuming extra essence; full stack cap does not mint or delete runes; exit/teleport returns to correct area.
- Verify recipe IDs, rates, object interactions and interface details against revision 742 fixtures; extend focused project tests instead of copying later behavior.

## Reference and uncertainty

- Exact level tables, success chances and asset/config IDs remain unverified until reference fixtures are captured.
- Apply [implementation conventions](implementation-conventions.md) for ownership, testability and durable state.
