# Revision 742: Smithing source catalog, recipe integrity and evidence

**Evidence scope:** The linked machine-readable tables were extracted from `Hagalaz.Game.Scripts/Skills/Smithing/Smithing.cs` on `main` at the start of this research pass. They represent *server source configuration*, not a live DB export or a verified client cache. Do not use them to override the historical candidate datasets without reconciliation.

## Exact source catalogs

- [Smelting ingredients](data/catalogs/smithing-smelting-source-definitions.csv): **18 ingredient rows for 13 bar definitions**; each row records bar item ID, ore item ID, required quantity, Smithing level and XP per successful bar.
- [Forging products](data/catalogs/smithing-forging-source-definitions.csv): **106 nonempty product rows**, including product item ID/quantity, required level, required bar quantity and base XP per bar. The source also has **10 intentionally empty `ForgingBarEntry()` placeholders**; the CSV excludes these and therefore **must not** be used directly to rebuild UI slot indexes. Keep ordered entry positions when implementing #377.
- Both CSVs include source file, line number and `repository_source_unverified_742` evidence status. No cache ID identity or historical parity is implied.
- Historical candidate comparisons: [Smithing bars](data/smithing-bars.csv) and [forging product candidates](data/researched/smithing-forging-products.csv). Use the [canonical researched source index](data/researched/README.md) for their provenance and limitations.

**Representative source relationships:**

| Bar item ID | Ore inputs and quantity | Minimum level | XP/bar |
| --- | --- | ---: | ---: |
| 2349 | 436 × 1 + 438 × 1 | 1 | 6.2 |
| 2351 | 440 × 1 | 15 | 12.5 |
| 2353 | 440 × 1 + 453 × 2 | 30 | 17.5 |
| 2359 | 447 × 1 + 453 × 4 | 50 | 30.0 |
| 2361 | 449 × 1 + 453 × 6 | 70 | 37.5 |
| 2363 | 451 × 1 + 453 × 8 | 85 | 50.0 |

These values are **present in Hagalaz source**, not verified target-742 fixture facts. The source includes additional single-ore special bars; use the CSV for the full list.

## Inventory mutation contract: confirmed

- `IItemContainer.Remove(item)` returns the **actual quantity removed**; it is **not** exact by default. `ItemContainerStorage.ApplyRemove` supports partial removal, and the API exposes `TryRemoveExact` and mutation boundaries for stricter operations.
- `IItemContainer.Add(item)` returns a boolean when destination capacity/stack limits reject insertion; `Replace(slot,item)` is a **void method** that writes the specified slot. A previous note implying that Cooking should check a `Replace` success boolean is incorrect and superseded here.
- `ContainerExtensions.TryAddLoot` intentionally spawns rejected overflow as owned ground items. An inventory-capacity rejection alone therefore does **not** establish that Mining loot is lost or that XP was awarded without a world reward.
- The current [Item Container Storage specification](../../openspec/specs/item-container-storage/spec.md) requires rejected exact mutations to preserve original state, and explicitly provides an atomic transaction boundary. Follow the owner-preserving interface already present; do not introduce a second inventory transaction framework.

## Confirmed Smithing hazards

### Partial smelting inputs

`SmeltTask.PerformTickImpl` checks each ore on tick 1 / every sixth tick, then consumes at the third tick using:

```csharp
var removed = ores.Sum(ore => inventory.Remove(requestedOre));
if (removed <= 0) { Cancel(); return; }
// succeeds and awards a bar and XP even when only one of several ores was removed
```

For a bronze bar: tick 1 confirms both 436 and 438. If 436 is spent by another action before tick 3 but 438 remains, the first remove returns 0, the second returns 1, their sum is positive, and the script can create bar 2349 and award XP. This is a reachable inventory change **between task ticks**, not a hypothetical full-inventory failure.

`ForgeTask` uses a related partial-count pattern: `Remove(bar, requiredCount)` can consume fewer than the required bars and then cancel, destroying the partial amount without producing the forged item. For a product requiring three bars, only two remaining at the consume tick yields 2 removed and a cancellation **after** losing them.

**Recommended focused issue:** perform one exact recipe validation/consumption operation for each output in the existing inventory owner, with all input quantities checked at the commit point. If multiple item types must be removed, stage them through one existing `ItemContainerTransaction` so a missing component cannot consume another. Preserve expected failure behavior (including intentional iron ore loss on an unsuccessful smelt) only *after* the full recipe has been consumed. Tests must use realistic overlapping inventory mutations between ticks. Do not treat a free slot after consuming a non-stackable ore as a reason to invent impossible output failures.

### Iron smelting chance

`SmeltTask.IsSuccess()` implements:

```csharp
Random.Next(0, 100) <= (level >= 45 ? 80 : 50)
```

This produces **51% success below 45 and 81% at/above 45**, and ignores the incremental bonus at levels 16–44. Historical candidate references support **50% at level 15 increasing by 1 percentage point per Smithing level up to 80% at 45** ([period reference](https://wiki.darkan.org/Iron_ore), [smithing reference](https://wiki.darkan.org/Smithing)). The proposed chance in percent for a normal furnace would be `Math.Clamp(level + 35, 50, 80)`; use a **strict comparison** `random.Next(100) < percent`. Validate any ring, spell and alternate-furnace bypasses independently; do not conflate furnace chance with every way to create a bar.

## Regression matrix

| Test | Preconditions | Expected result |
| --- | --- | --- |
| SMELT-01 | Bronze recipe, both ores initially present, copper disappears before consume tick | No bronze bar, no Smithing XP, tin remains |
| SMELT-02 | Steel recipe, iron remains, insufficient coal at consume tick | No iron/coal consumed, no bar or XP |
| SMELT-03 | Iron recipe, full ingredient present, failure roll | One iron ore consumed and no bar/XP on intended failed attempt |
| SMELT-04 | Iron recipe at level 15 / 16 / 44 / 45 / 99 | Chance 50 / 51 / 79 / 80 / 80 percent with boundary RNG tests |
| FORGE-01 | Forge recipe requires 3 bars, only 2 remain at consume tick | Two bars preserved, no output/XP |
| FORGE-02 | Full recipe present; forging multiple-unit result (arrowheads, nails, bolts) | Exact output count and expected XP per consumed bar; no partial output |
| COOK-01 | Cooking target remains live; raw input replaced with cooked or burnt in same slot | Correct item state and XP only for successful cooking; do not assume `Replace` has a rejection flag |

Existing `docs/revision-742/reference/deep-smithing.md` and issue [#377](https://github.com/frankvdb7/Hagalaz/issues/377) own broader definition migration. These gameplay correctness repairs should remain focused issues, *not* silently expand #377 or this documentation PR.
