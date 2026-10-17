# Farming: patch lifecycle, crops, persistence and exact model caveats

## Current authoritative state

The actual `Hagalaz.Services.GameWorld/Model/Creatures/Characters/Farming.cs` owns a `Dictionary<int,FarmingPatchTickTask>`, keyed by object ID. On hydration it constructs a per-patch tick task, hydrates it and requeues it. On dehydration it serializes each task's state. **Do not add a second scheduler or separate shadow patch registry**; improve existing tick ownership if needed.

`CharactersFarmingPatch` stores `MasterId`, `PatchId`, `SeedId`, `ConditionFlag`, `CurrentCycle`, `CurrentCycleTicks`, `ProductCount`. `SkillsFarmingSeedDefinition` supplies product ID/yield range, required level, planting/harvest XP, a varbit index, cycle count/ticks and patch type. These fields can express ordinary growth stages, but offline elapsed time/disease rolls are **not proven** simply by the presence of `CurrentCycleTicks`.

`FarmingSkillService.HandlePatchClickPerform` dispatches different options by cleared/dead/diseased/fertilized/planted states. `HandlePatchRaking` checks **item ID 5341** (rake), plays animation **2273**, may add weeds item **6055**, awards **4 XP**, and marks the patch cleared after three successful weed cycles. These IDs and rates are **direct source observations**, not yet 742 client parity evidence. It explicitly has a TODO for tree/fruit/bush chopping after health checks.

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
