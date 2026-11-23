# Farming: patch lifecycle, crops, persistence and exact model caveats

## Current authoritative state

The actual `Hagalaz.Services.GameWorld/Model/Creatures/Characters/Farming.cs` owns a `Dictionary<int,FarmingPatchTickTask>`, keyed by object ID. On hydration it constructs a per-patch tick task, hydrates it and requeues it. On dehydration it serializes each task's state. **Do not add a second scheduler or separate shadow patch registry**; improve existing tick ownership if needed.

`CharactersFarmingPatch` stores `MasterId`, `PatchId`, `SeedId`, `ConditionFlag`, `CurrentCycle`, `CurrentCycleTicks`, `ProductCount`. `SkillsFarmingSeedDefinition` supplies product ID/yield range, required level, planting/harvest XP, a varbit index, cycle count/ticks and patch type. These fields can express ordinary growth stages, but offline elapsed time/disease rolls are **not proven** simply by the presence of `CurrentCycleTicks`.

`FarmingSkillService.HandlePatchClickPerform` dispatches different options by cleared/dead/diseased/fertilized/planted states. `HandlePatchRaking` checks **item ID 5341** (rake), plays animation **2273**, may add weeds item **6055**, awards **4 XP**, and marks the patch cleared after three successful weed cycles. These IDs and rates are **direct source observations**, not yet 742 client parity evidence. After the single health check, an ordinary tree is felled with the best usable hatchet (leaving the `Checked`+`Empty` stump, rendered by the existing varbit logic) and then dug up with a spade. Fruit trees and bushes are picked without clearing: `FarmingPatchTickTask` owns the product count and regrows one product per `CycleTicks` up to the seed's `MaximumProductCount`, including offline catch-up on hydration. Per-product XP comes from the new `skills_farming_seed_definitions.product_experience` column and the regrow interval from `product_regrow_ticks` (0 falls back to `CycleTicks`); the health check keeps `harvest_experience`. The seven fruit tree seeds are created by `FarmingSeedSeeder` / `FruitTreeSeedSource` in `Hagalaz.Database.Migrations/Seeding` (add-only, never changes existing rows). Patch object 7965 in the pinned 742 cache uses 27 varbit states per species (6 growth, 7 fruit count, 6 diseased, 6 dead, stump, check-health) with a 10 state gap after every second species; `varpbit_index` is the species slot. Cache-verified: those layouts, 6 growth stages, up to 6 fruit, and the seed/product item ids. Not cache-verified: the 16000 tick (160 min) growth cycle and 6000 tick regrow interval (Farming reference summary; the Fruit_tree page's 14h40m total conflicts) and the XP values (2012 Fruit tree reference). Bushes are seeded the same way by `BushSeedSource`, including the four patch objects 7577-7580 (`skills_farming_patch_definitions`). Their cache layout stores growth (5-8 stages, then 0-4 berries), diseased, dead and check-health states in separate ranges, so `FarmingPatchTickTask` keeps a per-species table. Cache-verified: those layouts, the stage counts, 4 berries per bush, seed/product item ids. From the 2012 Seeds reference: levels, planting/check XP, XP per berry (poison ivy check-health is 675 in the table and infobox but 674 in its prose), 20 minute stages (2000 ticks). Not stated anywhere found: the farmed-bush berry regrow time, so `product_regrow_ticks` is 0 and berries regrow once per growth cycle. Poison ivy is documented as never diseased; the server still rolls disease for it.

## Growth transition table

| Phase | Required action | Durable state | Failure case |
|---|---|---|---|
| Weeds | Rake appropriate patch | cleared + weed progress | missing rake, inventory full, interruption |
| Prepared | Compost/fertilize where legal | fertilization flag | incompatible compost or already treated |
| Planted | Consume seed and check level | seed ID, cycle zero, planted | unsupported seed/type, wrong patch |
| Growing | Advance cycle at defined rate | cycle + remaining ticks | player offline, patch reload, duplicated timer |
| Diseased | Resolve per-cycle condition | disease flag; pause/regress rules | cure attempt without treatment |
| Dead | Clear failed crop | dead/cleared transition | double clear |
| Mature | Check health or harvest | checked + yield balance | unsupported harvesting tool, full inventory |
| Empty | Remove crop, reset flags | clean patch | double harvest or repeated grant |

Historical [Farming reference](https://wiki.darkan.org/Farming) includes individual crop cycle duration/yield, plant/harvest XP, compost and protection payments; the existing small data file is only a subset. **No complete crop-by-crop seed catalog has been extracted.**

## Implementation procedure

1. Enumerate actual `SkillsFarmingSeedDefinition` and patch records, then join 742 item definitions and patch object IDs by type.
2. Read `FarmingPatchTickTask` and every mutation of `CurrentCycleTicks`, `ConditionFlag` and `ProductCount` to establish what persists and in what unit. Character hydration uses tasks, so reconcile elapsed offline time inside **that owner** if the current behavior is deficient.
3. Separate per-character crop state from publicly shared map objects; another player's crop must never be rendered/harvested as yours.
4. Implement seed-to-crop and harvested-output changes through item-container operations. Protect repeated click callbacks, growth tick races and save/restore.
5. Make target client varbit/config updates from `VarpbitIndex` and exact crop stage assets; do not guess offsets.

## Regression tests

- Two characters planting the same location see independent crops.
- Tick advancement at exact cycle boundary, reconnect before/after boundary, huge offline elapsed interval.
- Compost then seed, seed then attempted compost, diseased cure with/without required item.
- Plant failure (capacity/requirements) changes nothing; harvest full inventory retains uncollected yield.
- Health checking cannot award XP repeatedly; clearing diseased/tree patch cannot mint tools or resources.
- Crop mutation during logout snapshot is either fully persisted or fully rejected.

**Open question:** if server wall-clock catchup is needed, add it inside existing patch hydration/task data rather than one timer per plot or a second background farming service.

## Crop-by-crop timed candidate table

[Seven allotments and six flowers](../data/researched/farming-allotments-and-flowers.csv) now record exact *reported* growth cycle count and cycle-minutes, planting/harvest XP and protection (where specified). Historical [seed definitions](https://wiki.darkan.org/Seeds) explicitly warn that disease or growth-cycle repeats can extend the minimum total growth interval. For example, potato and onion plots have four ten-minute stages, while white lily uses four 20-minute stages. These are wall-clock candidate durations, **not** preconverted Hagalaz game ticks.

Implementation detail: `SkillsFarmingSeedDefinition.CycleTicks` and `CharactersFarmingPatch.CurrentCycleTicks` must be reconciled with elapsed real time before interpreting the seed table. Record which transition awards planting XP, check-health XP and per-product harvest XP; the CSV includes individual harvest values that must not be unconditionally granted once per entire patch.

Additional test: disease at stage boundary holds or replays a cycle; offline growth crosses multiple cycles but does not skip a disease event or generate extra harvested produce; crop receives protection and cannot simultaneously roll an incompatible death transition; white lily protection prevents disease only when the relevant conditions hold.
