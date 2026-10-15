# Crafting — revision 742

**Repository assessment:** Existing gem, leather, jewelry, pottery and silver tasks. Existing files are entry points, not evidence of feature completeness.

## Historical scope

Multiple recipe families: gem cutting, spinning, hides, jewelry, pottery and silver casts, each with specific input tools and interfaces.

## Hagalaz entry points

- `Hagalaz.Game.Scripts/Skills/Crafting/CraftingSkillService.cs`
- `Hagalaz.Game.Scripts/Skills/Crafting/CraftingItemScreenOperations.cs`
- `Hagalaz.Game.Scripts/Skills/Crafting/JewelryTask.cs`
- `Hagalaz.Services.GameWorld/Services/CraftingService.cs`

## Implementation path

1. Keep individual recipe categories in their current scripts/data rather than inventing a global production DSL. Normalize common batch validation at the existing container operation boundary.
2. Map selection screens to the correct 742 item IDs, quantities, moulds and experience; output is produced only after all requirements pass.

## Regression acceptance

- Tool/mould presence is checked; one click cannot start two concurrent batch tasks; wrong components do nothing; all successful batches conserve materials and yield XP exactly once.
- Verify recipe IDs, rates, object interactions and interface details against revision 742 fixtures; extend focused project tests instead of copying later behavior.

## Reference and uncertainty

- Exact level tables, success chances and asset/config IDs remain unverified until reference fixtures are captured.
- Apply [implementation conventions](implementation-conventions.md) for ownership, testability and durable state.
