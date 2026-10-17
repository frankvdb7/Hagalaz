# Herblore and potions — revision 742

**Repository assessment:** Existing cleaning, potion and ingredient script families. Existing files are entry points, not evidence of feature completeness.

## Historical scope

Cleaning herbs, combining primary and secondary ingredients, potion doses, level requirements, effects and 742 recipe variants.

## Hagalaz entry points

- `Hagalaz.Game.Scripts/Skills/Herblore/Herbs/HerbloreSkillService.cs`
- `Hagalaz.Game.Scripts/Skills/Herblore/Potions/PotionSkillService.cs`
- `Hagalaz.Game.Scripts/Skills/Herblore/Potions/Potion.cs`
- `Hagalaz.Services.GameWorld/Services/HerbloreService.cs`

## Implementation path

1. Use herblore definitions to validate ingredients and dose identities. Keep active potion status in existing creature state/effects rather than a second stat-modification mechanism.
2. Separate creation, drinking, dose decrement and effect expiry; all four must be independently testable with fixed tick fixtures.

## Regression acceptance

- Wrong ingredient or skill does not consume supplies; dose progression preserves the potion family; repeated buffs honor 742 stacking rules; logout/save does not prolong an expired effect.
- Verify recipe IDs, rates, object interactions and interface details against revision 742 fixtures; extend focused project tests instead of copying later behavior.

## Reference and uncertainty

- Exact level tables, success chances and asset/config IDs remain unverified until reference fixtures are captured.
- Apply [implementation conventions](implementation-conventions.md) for ownership, testability and durable state.


## Implementation evidence supplement

[Herblore source transformations](herblore-source-transformations.md) documents cleaning, potion mixing, overload assembly, dose combination and input mutation. [Eight scalar source constants](data/catalogs/herblore-source-constants.csv) are separately cataloged. The [five-spike ingredient bug #529](https://github.com/frankvdb7/Hagalaz/issues/529) remains a code fix outside this documentation PR.
