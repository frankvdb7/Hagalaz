# Revision 742 Herblore: source transformation and dose contracts

**Status:** code-path audit of Hagalaz. This complements [historical Herblore candidates](data/researched/herblore-recipes.csv) and [the deeper feature reference](reference/herblore.md). It does not claim a runtime database export or decoded client fixture.

## Source constants

[Herblore numeric constants](data/catalogs/herblore-source-constants.csv) records **eight scalar definitions**, with source line evidence: filled vial 227, avantoe 261, dwarfweed 267, lantadyme 2481, ground mud runes 9594, grenwall spikes 12539, torstol 269, and potion-making animation 363.

The same source file also defines a five-ID OverloadPotions array: **15309, 15313, 15317, 15321, 15325**. The configured overload output in HerbloreSkillService is **15333**. These are repository-source IDs only, not cache-verified identities.

## Interaction flow and authoritative code owners

| Interaction | Source | Current action and XP |
| --- | --- | --- |
| Clean herb | HerbloreSkillService.TryCleanHerb -> CleanHerbTask | Validate herb and level, replace grimy herb slot with clean herb, award configured cleaning XP |
| Mix unfinished potion | HerbloreSkillService.MakePotion, unfinished branch | Consume source ingredient and replace vial; no completion XP in this branch |
| Finish potion | HerbloreSkillService.MakePotion, finished branch | Consume secondary ingredient, replace vial, award configured XP |
| Create overload | HerbloreSkillService.MakeOverload | Level 96; consume five extra potions; replace clean herb slot with output 15333; award 1,000 XP |
| Drink potion | PotionSkillService.DrinkPotion | Replace current slot with next dose, queue drinking state and animation 829, then parse dose text and invoke effect |
| Combine doses | PotionSkillService.CombinePotions | Apply explicit four-ID dose transition table to two source slots, possibly returning an empty vial |
| Empty potion | PotionSkillService.EmptyPotion | Replace potion slot with empty vial |

The existing recipe definitions live in the Herblore service DTOs; the consumption, potion effects and decanting paths must not be collapsed into a new recipe mechanism. A dialogue selection is not a reservation of ingredients across task ticks.

## Confirmed five-ingredient consumption defect

The completion callback in HerbloreSkillService.MakePotion checks only that the secondary ingredient is present. If its item ID equals **HerbloreConstants.Grenwallspikes (12539)**, the callback requests a removal of **five**. It does not inspect the actual quantity removed, then replaces the source potion slot and grants Herblore XP.

The current IItemContainer.Remove operation supports partial consumption; with only one to four spikes, the task can still create the product after removing insufficient material. This is a concrete path that does not require an artificial inventory-capacity failure.

The [period high-level ranging potion reference](https://wiki.darkan.org/Extreme_ranged) specifies **five** grenwall spikes plus a three-dose ranging potion for **260 XP**, and the [historical potion table](https://wiki.darkan.org/Potions) agrees. The source-level item ID and exact output variant still need independent cache verification. Fixing the behavior should be a separate issue using existing exact mutation boundaries.

## Overload and dose restrictions

The [period torstol source](https://wiki.darkan.org/Clean_torstol) gives level **96** and **1,000 XP** for overload creation. An [October 2012 unfinished torstol reference](https://wiki.darkan.org/Torstol_potion_%28unf%29) notes that ordinary unfinished torstol potion production is not the overload recipe. Preserve clean herb input identity and the five separate extreme potions.

PotionSkillService.CombinePotions uses a four-item ordered dose mapping and writes both slots, whereas DrinkPotion parses the current item's display name for dose messages. DrinkPotion currently changes the inventory slot **before** parsing the dose label and calling the finishing effect. An unexpected name without an opening parenthesis could throw at indexing, while an invalid numerical suffix can return before effect invocation. This merits tests against supported potion definitions before labeling a reachable gameplay bug.

## Regression matrix

| Case | Input state | Required invariant |
| --- | --- | --- |
| HERB-01 | Valid potion and 1–4 grenwall spikes | No ingredients consumed, no output or XP |
| HERB-02 | Valid potion and exactly 5 spikes | Exactly five spikes consumed, one output and one configured XP event |
| HERB-03 | Two potion products requested with only six spikes | One successful iteration, second rejected without consuming the last spike |
| HERB-04 | Clean herb task interrupted | No post-interruption reward or duplicate slot transformation |
| HERB-05 | Overload missing one of five required extreme potions | No partial consumption of the other ingredients |
| HERB-06 | Complete overload recipe and level 96 | Correct output 15333 and one 1,000 XP award for each full recipe |
| HERB-07 | Two one-dose potion items combined | Total dose count conserved and no duplicated liquid |
| HERB-08 | Valid potion drunk | Exactly one dose progression and one effect callback |
| HERB-09 | Unexpected item metadata | No item mutation before a recoverable validation failure |

These are proposed deterministic tests, **not tests already executed**.

## Deferred scope

Populated potion definition tables, full effect matrices, quest restrictions, client interface, flask mappings and 742 cache verification remain separate research tasks. They do not block completion of a source-backed documentation PR.
