# Cooking — revision 742

**Repository assessment:** Existing food recipes and cooking task. Existing files are entry points, not evidence of feature completeness.

## Historical scope

Raw-to-cooked/burned transformation, required level, chance to burn, cooking source bonuses and bulk cooking.

## Hagalaz entry points

- `Hagalaz.Game.Scripts/Skills/Cooking/CookingTask.cs`
- `Hagalaz.Game.Scripts/Skills/Cooking/RawFood.rsitem.cs`
- `Hagalaz.Game.Scripts/Skills/Cooking/StandardFood.rsitem.cs`
- `Hagalaz.Services.GameWorld/Services/CookingService.cs`

## Implementation path

1. Query recipe definitions by raw food and current cooking source. Keep burn chance and XP in cooking-specific service/task rather than spreading cooking rules into generic item containers.
2. Consume a raw item only inside the same checked operation that adds the cooked/burned output. Use the existing task lifecycle for interruptions.

## Regression acceptance

- Full inventory, source removal and cancelled batches do not destroy food; burnt and successful outcomes are mutually exclusive; exact skill XP and 742 animation behavior are fixture-backed.
- Verify recipe IDs, rates, object interactions and interface details against revision 742 fixtures; extend focused project tests instead of copying later behavior.

## Reference and uncertainty

- Exact level tables, success chances and asset/config IDs remain unverified until reference fixtures are captured.
- Apply [implementation conventions](implementation-conventions.md) for ownership, testability and durable state.
