# Design

## Decisions

- **New table, PK `potion_id`.** Every product id is unique. Ingredient lists are comma separated (same as Fletching) and parsed with the invariant culture.
- **`kind` discriminator.** Standard recipes consume one unfinished potion (or vial) plus one secondary ingredient. Overload is stored as a recipe row (base item torstol 269, secondary ingredients = the five extreme (3) potions, product 15333, level 96, 1000 XP), but it consumes *all* of its secondaries. That difference is the kind, so overload never leaks into the standard lookups or item script factories (its extreme ids would otherwise be registered as secondary-ingredient scripts and torstol as an unfinished potion, displacing their existing scripts in the first-wins `ItemScriptProvider`).
- **Extreme ingredients move into data.** Extreme attack/strength/defence/magic/ranging get their herb or ingredient as secondary ingredient; `secondary_item_count` = 5 for grenwall spikes. `IngredientPotion` now resolves `(this potion, other item)` with the standard secondary lookup instead of a per-class `Ingredient` constant. To keep dispatch unchanged, `SecondaryIngredientItemScriptFactory` skips ids that are primary ingredients (avantoe, dwarf weed, lantadyme keep `PrimaryIngredientItemScript`; the potion script handles the herb-on-potion use). Ground mud runes and grenwall spikes now get a `SecondaryIngredientItemScript`, which resolves the same recipe as the potion script.
- **`UnfinishedPotionFactory`** only registers unfinished potions of recipes that have a primary ingredient. Extreme, super-antifire, recover-special and super-prayer "unfinished" ids are finished potions with their own scripts; skipping them removes existing duplicate registrations without changing the winner.
- **Lookup order.** Recipes are loaded ordered by `potion_id`; for every shared primary ingredient (253, 257, 259, 263, 2481) this keeps the previous first-match winner. The unused unfinished-potion lookup is removed (its order differed for 93).
- **In-memory read model.** `HerbloreService` (singleton, `IStartupService`) loads potions once and is registered before `ItemScriptProvider`, whose factories enumerate them. Herb lookups keep querying the repository in a per-call scope.
- **Overload behaviour in code.** Consuming every extreme potion, tick delay 3, replacing the torstol, and the missing-potion message stay in `HerbloreSkillService.MakeOverload`; its level, inputs, output and XP come from the definition.
- **Remaining constants.** `FilledVial` (227) and `MakePotionAnimation` (363) are implementation constants and stay.

## Risks

- Edits need a restart (same as other preloaded stores).
- Source values that look wrong (energy potion primary 253 with unfinished 97) are seeded verbatim.
