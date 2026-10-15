# Smithing and smelting — revision 742

**Repository assessment:** Existing furnace, anvil and data definitions. Existing files are entry points, not evidence of feature completeness.

## Historical scope

Smelting ore into bars, smithing bars into equipment, required levels, coal quantities, selection interfaces, skill XP and forging animations.

## Hagalaz entry points

- `Hagalaz.Game.Scripts/Skills/Smithing/SmeltTask.cs`
- `Hagalaz.Game.Scripts/Skills/Smithing/ForgeTask.cs`
- `Hagalaz.Game.Scripts/Skills/Smithing/SmithingInterface.rsi.cs`
- `Hagalaz.Services.GameWorld/Services/SmithingService.cs`

## Implementation path

1. Use the existing database recipes instead of switch statements for item IDs. Keep forge/smelt tasks interruptible and capture each selected recipe's required ingredients.
2. Before starting a batch, validate skill, tool and initial resources. Revalidate per iteration inside an item-container transaction, with output-space checks.

## Regression acceptance

- Mixed ore/bar batches consume exact material ratios; full inventory and insufficient bars cannot duplicate output; quantity-X stops at requested count; iron success probability matches a 742 fixture.
- Verify recipe IDs, rates, object interactions and interface details against revision 742 fixtures; extend focused project tests instead of copying later behavior.

## Reference and uncertainty

- Exact level tables, success chances and asset/config IDs remain unverified until reference fixtures are captured.
- Apply [implementation conventions](implementation-conventions.md) for ownership, testability and durable state.
