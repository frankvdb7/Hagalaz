# Cooking — source-aware recipes, burn policy and item conservation

## Period food and source distinctions

[Meat products](../data/researched/cooking-meat-products.csv) provide 10 representative required levels, XP and healing values, with a separate product-specific cooking method. A spit is needed for beast/bird roasting. [Historical cooking source](https://wiki.darkan.org/Cooking) distinguishes fire, range, iron spit and unusual food; some pastries require an oven/range. Historical modifiers include cooking gauntlets and special ranges. Source-specific burn probabilities still need a target build fixture.

## Actual Hagalaz task implementation

`Hagalaz.Game.Scripts/Skills/Cooking/CookingTask.cs`:

- Stores `GameObject`, `RawFoodDto`, `TotalCookCount`, `CookCount`.
- Each tick checks disabled/unloaded/changed world object *by exact object identity* before processing; cancellation unsubscribes interrupt event.
- Replays cooking animation **897** on the first tick and every sixth tick.
- Every third tick it increments `CookCount`, finds the current raw item instance in inventory, selects burn outcome and replaces the exact slot with cooked/burnt result.
- Adds `RawDto.Experience` only when **not** burned.
- `IsBurned` uses `StopBurningLevel`, compares cooking-gloves item **775**, has a special case for product **391**, clamps a stop-burning level gap to 20, then rolls `RandomStatic.Generator.Next(45) <= levelsToStopBurn`.

These are *direct code observations*. They are **not validated 742 client parity or a finding that the implementation is perfect**.

## Data and implementation boundaries

`SkillsCookingRawFoodDefinition` has raw item ID, cooked item ID, burnt item ID, required level, stop-burning level and experience. `SkillsCookingFoodDefinition` separately has healed life points, leftover item and eating time. This separation is correct: **cooking XP and eating heal are independent**.

Before changing `IsBurned`, check whether the source requires range/fire/bonfire distinctions. This method, as inspected, does not reference `GameObject` in its burn calculation; a uniform fire and range threshold **may be a parity gap**. Prove it with a focus test and historical reference capture, not a guessed 742 formula.

The task uses `Performer.Inventory.Items.Replace(slot, ...)` directly. Confirm the mutation API's change publication, identity and rollback semantics before replacing it with an unrelated general transaction wrapper. The proposed output must fit the reserved source slot, and a repeated click must not schedule two overlapping replacements for one item.

## Failure and callback cases

1. Cook X with exactly X input food -> X output items, XP for cooked-only successes, stop after final item.
2. Input missing mid-task -> no phantom animation/XP/output; same when world object disabled or despawned.
3. Source changes at runtime (range turns off, bonfire disappears, region unloaded) -> cancel without cooking on stale source.
4. Cooking gloves on/off during action, skill boost expires, stop-burning threshold crossed -> recompute effective odds without exceeding valid probability.
5. Full inventory with one source item, different noted/cooked/burnt output shapes, item stack limits and slot replacement.
6. Two concurrent cooking tasks, interruption during tick and logout snapshot boundary -> no duplicated inputs/output.
7. Food eaten and cooking source use must not share healing-state handlers or XP awards.

## Completion dataset still required

Every cookable raw product and burned/cooked pair with level/XP/burn threshold and cooking source categories, source object/animation IDs and item-on-object widget operation. The historical CSV deliberately represents only ten **meat dishes**, not the full fish, pie, cake, stew, beer, wine and potato catalogs.

## Fish source/definition reconciliation

[23 candidate fish and special-cooking rows](../data/researched/cooking-fish.csv) include raw-to-cooked name, minimum level, historical stop-burning threshold, success XP and heal value. Include source-specific burn distinctions in parity fixtures: fire vs range, gauntlets ID 775, output item 391 special case, member-only high-tier catches and minigame/quest availability. This dataset does **not** determine raw/cooked/burnt cache IDs.

Do not silently map a literal `never` threshold to a byte value: the Hagalaz `SkillsCookingRawFoodDefinition.StopBurningLevel` field is a byte, and the existing `IsBurned` logic assumes a numeric threshold. Resolve the matching 742 definition's semantics and any equipment-dependent exception before importing. For example, some high-level foods never become unburnable without a bonus, while gauntlets change the threshold.

Add cooking catch-to-recipe tests for trout, salmon, lobster, shark, cavefish and rocktail, and verify the item identity and burn behavior against the pinned target cache; assume neither that all fish cook on fire nor that every fish has only one healing outcome.
