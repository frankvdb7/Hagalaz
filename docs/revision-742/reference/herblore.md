# herblore — revision 742 research data

**Evidence tier:** period-preserved secondary reference, pending target build verification.

## Candidate potion production recipes

| Potion (3 doses) | Level | Clean herb + secondary | XP |
|---|---:|---|---:|
| Attack | 3* | Guam + eye of newt | 25 |
| Antipoison | 5 | Marrentill + unicorn horn dust | 37.5 |
| Strength | 12 | Tarromin + limpwurt root | 50 |
| Super attack | 45 | Irit + eye of newt | 100 |
| Super energy | 52 | Avantoe + mort myre fungus | 117.5 |
| Super restore | 63 | Snapdragon + red spiders' eggs | 142.5 |

*The preserved general Herblore page prints level **1** for Attack, while the potion calculator prints **3**. Do not silently resolve the discrepancy. Sources: [Potion recipe calculator](https://wiki.darkan.org/Herblore_calculator), [historical Herblore](https://wiki.darkan.org/Herblore), [training notes](https://wiki.darkan.org/Herblore_training).

**To implement:** distinguish herb cleaning, unfinished potions, secondary ingredients, dose identities, combining/dilution and drinking effects. Pull exact per-dose item IDs from target cache and local definitions; verify boost formula, drain timing and potion effects separately.

## Remaining target evidence

Validate all recipe, NPC/object, animation and config IDs in a captured 742 client/cache; trace live click packets, skill XP changes and cancellation through existing Hagalaz handlers. Numeric rows are candidates, not yet authoritative server constants.

[Implementation guide](../herblore.md) · [Research policy](../research-evidence.md)
