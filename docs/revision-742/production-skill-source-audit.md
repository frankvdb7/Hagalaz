# Revision 742: production skill source behavior and test cases

**Status:** verified from named Hagalaz source files, NOT historically verified against the pinned 742 client. This document supplements [skills reconciliation](skills-reconciliation-test-matrix.md) and the canonical [period candidate tables](data/researched/README.md); it does not replace either.

## Smithing: smelting

Source: `Hagalaz.Game.Scripts/Skills/Smithing/SmeltTask.cs`.

- `Definition.SmeltDefinition.RequiredOres` provides the input requirements, `Definition.BarID` the product, and `SmithingExperience` the success reward.
- At tick 1 or each sixth tick the script validates all ingredients and queues animation 897.
- At ticks divisible by 3 it increments the processed count and removes input ores using a sum of individual `Remove` results. It only checks `removed <= 0`, **not that every required ingredient count was removed**.
- For a successful smelt, it invokes `Inventory.Items.Add(bar)` without checking its boolean outcome, and then grants XP. This has an evident reward-integrity gap when output insertion fails.
- For bar ID 2351 it draws `RandomStatic.Generator.Next(0, 100)` and tests `<= 80` at effective Smithing level 45+, or `<= 50` below. Inclusive comparisons mean **81% / 51%** in the current implementation, rather than the apparent intended 80% / 50%. Do not silently replace these values with an assumed historical formula.
- On iron failure ingredients are consumed and no XP is granted; whether failed ore consumption is version-correct needs historical verification.

**Tests:** with two ingredients, remove all of ingredient A between initial validation and consume time; confirm no partial ingredient loss without resulting bar. Fill output inventory before the reward, verify no silent lost bar or XP. At threshold level 44/45 use controlled random values 49/50/51/79/80/81 to pin existing behavior and choose a historically justified fix separately. Confirm 1/3/6 tick ordering and interruption.

## Cooking: raw, cooked and burnt item ownership

Source: `Hagalaz.Game.Scripts/Skills/Cooking/CookingTask.cs`.

- Uses `RawFoodDto.ItemId`, `CookedItemId`, `BurntItemId`, `StopBurningLevel`, and `Experience`.
- Validates the target object remains enabled and is still present **by reference** at its map position.
- At tick 1 or every sixth tick it queues animation 897; every third tick it looks up raw food, gets its slot, rolls burn, replaces that slot with cooked or burnt output, and grants XP only if not burnt.
- `IsBurned()` stops all burns when effective level meets `StopBurningLevel`. Glove item ID 775 lowers the no-burn threshold by six levels except for cooked item ID 391. Otherwise the level deficit is capped at 20 and compared with `RandomStatic.Generator.Next(45) <= deficit`. Inclusive comparison gives `(deficit+1)/45` while deficit is between 0 and 20; note that an early stop-burning guard normally prevents deficit zero.
- `Inventory.Items.Replace` result is not checked before granting success XP. Verify whether replace can reject mutation, and whether stacked/multi-count inputs are handled according to this script's supported items.

**Tests:** level just below/at `StopBurningLevel`; gloves present/absent; cooked ID 391 exception; exact source object removed/replaced; inventory slot replaced successfully; interrupted batch. Crosswalk actual rows to `data/researched/cooking-fish.csv` and `cooking-meat-products.csv`, preserving evidence labels.

## Firemaking: ignition

Source: `Hagalaz.Game.Scripts/Skills/Firemaking/FiremakingTask.cs`.

- Uses `FiremakingDto.RequiredLevel`; queues animation 733 at tick 1 and every twelfth tick.
- Rolls ignition every fourth tick. Current formula can be simplified algebraically for positive required level to `chance = 0.40 + (effectiveLevel + 1) / requiredLevel²`; the code expresses it through `Math.Log` and `Math.Exp`.
- `FinishCallback` is invoked before cancellation. Its item consumption, fire placement and XP ownership must be reviewed separately; this task alone does not prove their atomicity.
- No explicit chance clamp is applied, so values exceeding 1 are guaranteed to succeed against a uniform sample in [0,1).

**Tests:** required level/boost boundary, interrupted ignition, callback throws, no duplicate log consumption, fire placement collision, invalid/nonpositive level requirements. Compare ground fire vs bonfire with `data/researched/firemaking-method-xp.csv` rather than conflating methods.

## Runecrafting: multi-rune thresholds

Source: `Hagalaz.Game.Scripts/Skills/Runecrafting/Runecrafting.cs`.

`GetMultiplier` inspects `RunecraftingDto.LevelCountMultipliers` from last index down, ignores sentinel -1, and returns `2 + index` for the first level threshold satisfied, otherwise 1. This is source-backed and means the array configuration, not a hardcoded global tier table, controls output quantity.

**Tests:** no threshold; -1 sentinel; exact threshold, one below, boosted level, malformed/misordered array; check that quantity and XP award policy are distinct. Reconcile `data/runecrafting-runes.csv` and `data/researched/combination-runes.csv`.

## Fletching: interaction ownership

`Hagalaz.Game.Scripts/Skills/Fletching/FletchingBow.rsitem.cs` delegates `UseItemOnItem` to `IFletchingSkillService.TryFletchBow`. To document recipes or batch production accurately, follow the service implementation and its DTO/repository source rather than inferring recipes from this thin item script. Candidate data: `data/fletching-bows.csv`, `data/researched/fletching-bow-stages.csv`, and arrow/bolt/dart/crossbow tables. Verify quantity, cancellation, level requirements and consumed items per attempt.

## Evidence and follow-up discipline

These observations identify **potential correctness bugs requiring reproducible tests**; do not file a duplicate issue or claim a security defect without checking actual `ItemContainer` method return contracts and all callers. Keep runtime fixes in focused changes, with appropriate OpenSpec records under the repository's AGENTS.md rules. This PR remains documentation-only and must not be labeled a complete cache or database reconciliation.
