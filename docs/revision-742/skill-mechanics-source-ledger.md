# Revision 742 skill mechanics: source evidence and extraction ledger

**Status:** partial, implementation-audited; historical numerical tables NOT revision-verified. **Target:** original 742-era behavior, not current live behavior. All cited paths below are Hagalaz repository paths. Do not infer historical correctness from server implementation.

## Skill and progression IDs verified in source

`Hagalaz.Game.Abstractions/Model/Creatures/Characters/StatisticsConstants.cs` defines 25 skills and an XP cap of 200,000,000 per skill. The table below is server skill ID -> client skill ID; copying the server ID straight into a client packet would be incorrect.

| Skill | Server | Client |
|---|---:|---:|
| Attack | 0 | 0 |
| Defence | 1 | 4 |
| Strength | 2 | 1 |
| Constitution | 3 | 5 |
| Ranged | 4 | 2 |
| Prayer | 5 | 6 |
| Magic | 6 | 3 |
| Cooking | 7 | 15 |
| Woodcutting | 8 | 17 |
| Fletching | 9 | 18 |
| Fishing | 10 | 14 |
| Firemaking | 11 | 16 |
| Crafting | 12 | 10 |
| Smithing | 13 | 13 |
| Mining | 14 | 12 |
| Herblore | 15 | 8 |
| Agility | 16 | 7 |
| Thieving | 17 | 9 |
| Slayer | 18 | 19 |
| Farming | 19 | 20 |
| Runecrafting | 20 | 11 |
| Construction | 21 | 21 |
| Hunter | 22 | 22 |
| Summoning | 23 | 23 |
| Dungeoneering | 24 | 24 |

These mappings are **verified repository constants**, not independently decoded client protocol evidence. Likewise `SkillFlashFlags` is an indexed client presentation mapping; use the source array, not the skill ID as the flash bit.

## Database and runtime provenance

**Mining** — `Hagalaz.Services.GameWorld/Services/MiningService.cs` uses `IMiningOreRepository`, `IMiningPickaxeRepository`, `IMiningRockRepository`, EF projection to `OreDto`, `PickaxeDto`, `RockDto`; ore lookup joins through rock definitions, and rock loot resolves through its object definition's `LootTableId`. The authoritative configured numeric data is in the actual DB rows, **not the source DTO declarations**. Export `FindAllOres()`, `FindAllPickaxes()`, `FindAllRocks()` and rock-to-loot associations against the populated 742 environment; include snapshot revision and DB migration/seed provenance.

**Woodcutting** — `Hagalaz.Game.Scripts/Skills/Woodcutting/WoodcuttingSkillService.cs` loads logs, trees, hatchets, an object loot table and a population count from injected services. Required level comes from `LogDto.RequiredLevel`, award XP from `LogDto.WoodcuttingExperience`, depleted object from `TreeDto.StumpId`, and fall chance from `LogDto.FallChance`. These values must be exported as related records, not inferred from tree names.

**Fishing** — `Hagalaz.Game.Scripts/Skills/Fishing/FishingSkillService.cs` consumes `IFishingSpotTable`, tool/bait IDs, `IFishingLoot` and a live NPC handle. XP is `result.Item.FishingExperience` only after a successful inventory add. A fishing spot is NOT a static world object; capture NPC identity, available click options and catch-table weights.

**Farming** — `Hagalaz.Services.GameWorld/Logic/Characters/FarmingPatchTickTask.cs` uses `SeedDto` (`ItemID`, `CycleTicks`, `MaxCycles`, product bounds, `VarpBitIndex`) and `PatchDto` (`ObjectID`, `Type`). `Refresh()` resolves the object definition's `VarpBitFileId` and publishes the character-specific patch stage. Persisted progress must be cross-referenced with the hydration DTO and tests.

## Actual Hagalaz formulas, not verified historical formulas

All three gathering tasks independently roll `RandomStatic.Generator.NextDouble() <= chance` once per task tick. The source computes:

```text
levelModifier = log10(log10(effectiveSkillLevel)) * 0.075
if (levelModifier > 0) chance += levelModifier

woodcuttingChance = logs.BaseHarvestChance + hatchet.BaseHarvestChance + positiveLevelModifier
miningChance = ore.BaseHarvestChance + pickaxe.BaseHarvestChance + positiveLevelModifier
fishingChance = table.BaseCatchChance + positiveLevelModifier
```

No explicit clamp to [0,1] was seen in these calculations; values over 1 mean guaranteed success, not a probability error in `NextDouble`. Level 1 results in a non-finite intermediate negative infinity (effectively ignored by the positive comparison); validate this boundary and invalid levels in tests before changing formula semantics. The tick task animates periodically (Mining/Woodcutting every fourth tick) and can be cancelled when the live target is missing/disabled or interrupted. No numeric cycle period should be invented without tracing `RsTickTask`.

Both Mining and Fishing use a population-sensitive respawn expression:

```text
respawnTick = (int)(configuredRespawnTime * (1 - characterCount * 0.00025) * 100)
```

This expression can reach zero/negative values at large character counts; document current behavior before selecting a cap. The formula is implementation evidence, not historical proof.

## Farming details actually implemented

`FarmingPatchTickTask` has 500 weed regrowth ticks and 3 weed cycles according to source constants (its comment equates 500 ticks with five minutes). Plants advance after `Seed.CycleTicks`; a diseased plant becomes dead at the next growth step. At maturity the product count is randomly selected between configured minimum and maximum (upper bound exclusive after adding one). Fertilized and super-fertilized conditions multiply *minimum* yield by 1.33 and 1.66 respectively, as integers. These are **current code rules**, not externally verified historical rules.

The displayed varbit base formula varies by patch type: Allotment `6 + index * 7`; Herb `4 + index * 7`; Flower `8 + index * 5`; Hop `3 + index * 5`; other types use the existing source expression. Condition offsets then vary by patch type. Validate these against actual revision-742 object transformations and client cache varbits before altering any offsets.

## Specific code-level validation findings

| Severity | Location | Evidence and required test |
| --- | --- | --- |
| High | `MiningRocks.rsobj.cs` harvest callback | `TryAddLoot(..., out _)` discards award outcome, then displays success and calls `AddExperience` unconditionally. Simulate a failed insertion, assert no XP or success message; also check whether `TryAddLoot` can partially succeed. |
| High | `FarmingPatchTickTask.cs` `Grow` | `RemoveCondition(Watered)` occurs before the disease-chance check `HasCondition(Watered)`, making that bonus unreachable in the shown growth path. Add fixed-RNG regression around watering before changing logic. |
| Medium | `FishingSkillService.cs` catch callback | Bait removal happens **after** catch delivery and XP; a final bait may award a catch first, and the removal condition branches on remaining bait. Verify whether one bait is consumed per attempt, per successful catch or per caught item; no behavior change without evidence. |
| Medium | `MiningRocks.rsobj.cs`, `FishingSkillService.cs` | Population-adjusted respawn calculation has no visible lower bound; test extreme counts and zero/negative scheduled ticks. |
| Medium | `FarmingPatchTickTask.cs` stage serialization | Rehydrate stage, condition, product count and elapsed time using original persisted snapshot; compare before/after client varbit values and timer progression, especially around maturity. |

These findings are **candidates requiring reproduction**, not confirmed issue tickets. Keep fixes out of this documentation PR.

## Historical numeric data: use the newly merged canonical datasets

The latest `main` introduces the source-attributed [mining ores CSV](data/mining-ores.csv), [gathering reference](reference/deep-gathering.md), [skill catalog index](data/README.md), and [expanded historical candidates](data/researched/README.md). **Do not maintain a second unsynchronized numeric table in this ledger.** The canonical candidate mining table reports iron at level 15, **35 XP**; an older guide consulted in the original audit gave **30 XP**. This discrepancy demonstrates why historical secondary sources cannot be treated as revision-verified. Retain the conflict as an evidence gap; only a pinned 742 fixture or verified gameplay reference can settle it. The canonical CSV also has no blurite entry, so do not assume it is absent from the game.

The `experience-levels-1-120.csv` file records a calculated XP curve; it is not a decoded cache asset. Follow [deep progression](reference/deep-progression.md) to compare against Hagalaz's actual level calculations and client mappings.

The repository's [read-only reconciliation tooling](tools/README.md) already defines the approved cache fixture, checksum and missing database exports. Extend those tools only after checking if the skills-specific data can be obtained through their existing inputs. **Do not assert any extraction ran** without actual fixture and database access.

## Concrete discrepancies to reconcile from the merged sources

| Question | Ledger / source conflict | Resolution evidence |
| --- | --- | --- |
| Iron ore XP | Earlier secondary reference: 30; `data/mining-ores.csv`: 35 | Pin to historical 742 action/definition and verify awarded XP in-game |
| Fishing bait semantics | Catch callback awards XP then consumes bait; `reference/deep-gathering.md` expects one bait per successful catch | Define ordering for single and multi-item catches and test last-bait behavior |
| Woodcutting and Mining loot | Server scripts use loot tables instead of guaranteed per-resource item creation | Compare actual loot weights and inventory mutation outcome against XP and depletion behavior |
| Client-side skill IDs | `StatisticsConstants.ClientSkillIDs` is repository evidence | Verify revision-742 packet encoding / skills interface index before approving a change |
| Farming growth varbit offsets | Source contains patch-type arithmetic and source-only timers | Decode index 16 objects and corresponding varbits with approved cache fixture |

## Concrete reproducible data export

For each row in a *running populated* environment, export:

```text
record_kind,definition_id,linked_id,required_level,xp,
base_chance,tool_modifier,depletion_chance,respawn_time,
animation_id,loot_table_id,loot_weight,quantity_min,quantity_max,
revision,source_path_or_table,source_snapshot,verification_status
```

Generate one normalized table per domain (trees/logs/hatchets; rocks/ores/pickaxes; fishing spots/tools/loot; patches/seeds/varbits). Do not combine unrelated IDs into one catch-all identity. Record exact SQL table names after inspecting repositories/entities. Cross-join data through actual relationship keys, **never names**. Keep missing values blank with `verification_status=unverified`.

## Remaining coverage

- Obtain populated database exports; this GitHub-only review does **not** provide runtime DB contents.
- Decode target cache object/item/NPC/animation/varbit definitions, including all transformed IDs and click options.
- Establish historical source provenance for per-item XP, levels, weights, success odds, depletion, respawn and tick rates.
- Validate deterministic runtime behavior with tests and compare against reference client captures.
- Expand skill documentation to Cooking, Smithing, Firemaking, Fletching, Crafting, Herblore, Runecrafting, Agility, Thieving, Slayer, Hunter, Construction, Summoning and Dungeoneering, as well as progression, boosts and client updates.
