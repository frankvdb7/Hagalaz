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

## Extended 2012 candidate datasets

- [28 gold-jewellery recipes](data/researched/crafting-gold-jewellery.csv), [nine cut-gem recipes](data/researched/crafting-gem-cutting.csv) and [crafting transformation/atomicity contract](reference/deep-crafting.md). Numeric product/mould IDs require 742 cache reconciliation.


## Implementation evidence supplement

[Crafting source behavior and conservation](crafting-source-behavior-and-integrity.md) documents active task owners, per-tick sequencing, leather thread consumption and jewellery multi-ingredient integrity. [18 code-owned constants](data/catalogs/crafting-source-item-constants.csv) are recorded with source lines. Runtime defect [#528](https://github.com/frankvdb7/Hagalaz/issues/528) is tracked separately.
