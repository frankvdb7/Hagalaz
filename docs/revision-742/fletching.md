# Fletching — revision 742

**Repository assessment:** Existing bows, ammo and item-use scripts. Existing files are entry points, not evidence of feature completeness.

## Historical scope

Bow shafts/stringing, arrow and bolt creation, material/tool requirements, batch quantities and experience.

## Hagalaz entry points

- `Hagalaz.Game.Scripts/Skills/Fletching/FletchingSkillService.cs`
- `Hagalaz.Game.Scripts/Skills/Fletching/FletchingTask.cs`
- `Hagalaz.Game.Scripts/Skills/Fletching/FletchingDefinition.cs`
- `Hagalaz.Game.Scripts.Tests/Skills/Fletching/FletchingCallbackRegressionTests.cs`

## Implementation path

1. Keep item-on-item dispatch in the current script factories; select recipe and batch size through existing dialogue/widget flow.
2. Move material consumption and output addition through existing container mutation boundaries. Prevent stale callback reuse from starting a second fletching job.

## Regression acceptance

- Correct materials, quantities and XP across bow/ammo variants; no duplicated output after repeated clicks; interrupt and inventory capacity preserve inputs.
- Verify recipe IDs, rates, object interactions and interface details against revision 742 fixtures; extend focused project tests instead of copying later behavior.

## Reference and uncertainty

- Exact level tables, success chances and asset/config IDs remain unverified until reference fixtures are captured.
- Apply [implementation conventions](implementation-conventions.md) for ownership, testability and durable state.

## Detailed 2012 candidate dataset

The [Fletching stage dossier](reference/deep-fletching.md) links 68 new production records spanning bows, crossbows, arrows, bolts and darts. Production quantities, XP event ownership, gating and failure tests are explicit; cache IDs remain unverified.
