# Revision 742 Crafting: source-level lifecycle and item conservation

**Status:** code-path audit of Hagalaz `main` on 17 October 2026. The [historical candidate guide](reference/deep-crafting.md) and [period candidate tables](data/researched/README.md) remain the primary historical references. Literal numbers below are **repository-source evidence**, not independently verified from the version-742 client. This document does **not** require a database migration or a live database export.

## Numeric source constants

[Crafting constants CSV](data/catalogs/crafting-source-item-constants.csv) contains **18 declarations** from `Hagalaz.Game.Scripts/Skills/Crafting/CraftingSkillService.cs`, with source line and evidence type. Relevant examples:

| Meaning | Source ID |
| --- | ---: |
| Gold bar | 2357 |
| Silver bar | 2355 |
| Ring mould | 1592 |
| Amulet mould | 1595 |
| Necklace mould | 1597 |
| Bracelet mould | 11065 |
| Needle | 1733 |
| Thread | 1734 |
| Soft clay | 1761 |

The remaining mould and no-gem product sentinel IDs are in the CSV; they are **not** proof of a decoded item definition. Static mould ownership belongs to `CraftingSkillService`. No second constant registry is proposed.

## Production and interaction matrix

| Action / actual owner | Selection and requirements | Completion mutation | Experience timing | Animation / cadence |
| --- | --- | --- | --- | --- |
| Bake pottery: `CraftingSkillService.BakePottery` | Formed product and level | Replace exact formed-product inventory slot with baked product | Bake XP after replace | 897 at tick 1/every 6, transform every 3 |
| Form pottery: `CraftingSkillService.FormPottery` | Soft clay and level | Replace source clay slot with formed product | Form XP after replace | 897 at tick 1/every 6, transform every 3 |
| Cut gems: `CraftingSkillService.CutGem` -> `CutGemTask` | Eligible uncut gem and level | Task-owned gem transformation | Consult task for award/failure | Do not infer from UI |
| Spin: `CraftingSkillService.Spin` -> `SpinTask` | Spinning input and level | Task-owned transformed output | Consult task | Do not infer from UI |
| Leather: `CraftingSkillService.CraftLeather` -> `LeatherTask` | Needle, thread, required hides, level | Removes hide quantity; intermittently consumes thread and can break needle | XP after output add | 1249 at tick 1/every 6, makes every 3 |
| Jewellery: `JewelryTask` | Gold bar + gem, or gold only, configured mould/level at prior selection | Removes resource and (if different) bar, adds product | XP after output add | 3243 at tick 1/every 6, makes every 3 |
| Tanning: `CraftingSkillService.Tan` | Tanner and chosen hide | Separate domain interaction | No Crafting XP assumption without confirmation | Task/callback-specific |
| Silver shaping: `SilverTask` | Silver bar, selected product and mould | Separate task owner | Consult task | Do not reuse jewellery rules |

**Important:** a client dialogue listing eligible products does not prove that the recipe's ingredients remain present at the later production tick. An intermediate inventory action can change those counts.

## Item mutation semantics that must be retained

`IItemContainer.Remove` returns the **actual quantity removed** and can remove fewer units than requested. `TryRemoveExact` and the existing transaction boundary are available for an exact all-or-nothing operation. `IItemContainer.Add` reports whether insertion succeeded; `Replace` is `void`, directly replaces a slot, and does not have a success boolean to inspect. See `openspec/specs/item-container-storage/spec.md`.

### Jewellery: partial resource path

`JewelryTask` confirms the bar and gem at tick 1 or every sixth tick. At tick 3 it removes `Definition.ResourceID`, removes one gold bar if that ID differs, **sums** the quantities, and only rejects when `removed <= 0`. Hence a gold bar can be awarded into jewellery with the gem absent (or vice versa) after intervening inventory changes. For plain gold (`ResourceID == GoldBar`), one removal is expected; do not require two gold bars.

The item's mould is chosen earlier by the dialogue/service; verify mould/tool belt eligibility at source rather than assuming every possible mould is present in this task. Do not create a second crafting recipe engine to address this.

### Leather: non-exact hide removal and intermediate tool consumption

`LeatherTask` checks at tick 1/sixth tick: needle, thread and at least `Definition.RequiredResourceCount` hides. At each third tick it increments made count, consumes one spool of thread every fifth item, then may remove a needle with `Random.Next(0,30) <= 3` (**4/30 of random draws**), and finally calls partial `Remove(hides, RequiredResourceCount)`. If only some hides remain, it cancels **after** removing what remains and potentially after consuming thread/needle.

One period [thread reference](https://wiki.darkan.org/Thread) states roughly four or five uses per spool, broadly compatible with amortized thread consumption, but it does not validate the precise session counter reset, tool-break probability or item-loss timing.

A production fix should use existing exact item mutation ownership; keep intentional thread/needle rules independently evidenced instead of silently deleting them.

## Targeted deterministic tests

| Case | State changes | Expected |
| --- | --- | --- |
| CRAFT-01 | Jewellery initially has gem and bar; gem spent before production tick | Reject entire jewellery action without consuming remaining bar; no product/XP |
| CRAFT-02 | Jewellery initially has gem and bar; bar spent before production tick | Gem remains, no product/XP |
| CRAFT-03 | Plain gold jewellery configured with gold bar as resource | Exactly one bar consumed; no invented gem requirement |
| CRAFT-04 | Leather recipe needs 3 hides, only 2 remain before completion | No hides/thread/needle consumed for rejected iteration |
| CRAFT-05 | Fifth leather item consumes spool on successful iteration | One spool debited at documented consumption event; rejected iteration does not consume it |
| CRAFT-06 | Needle-break roll on successful iteration | Actual configured behavior represented, with deterministic RNG; no flaky probabilities |
| CRAFT-07 | Pottery input swapped/removed after dialog | Exact source slot is replaced only if still eligible; no output/XP on rejection |
| CRAFT-08 | Batch interrupted before next tick | No next product; interrupt handlers unregistered |
| CRAFT-09 | Multi-output candidate quantity vs source recipe | Preserve per-action vs per-item XP; do not silently multiply output or XP |

Do not treat these as tests already run. They are reproducible requirements for later code PRs.

## Historical data crosswalk

- [28 jewellery candidates](data/researched/crafting-gold-jewellery.csv) and [gem cutting candidates](data/researched/crafting-gem-cutting.csv) supply period levels and XP, with cache IDs still unverified.
- Stringing, spinning, silver, gem cutting, jewellery, pottery and tanning are **distinct** interactions. Do not flatten into generic `CraftingDefinition` or apply the same consume/reward semantics to all.
- No populated database dump or approved cache was used; recipe completeness remains an explicit future research concern, separate from this source-only dossier.

## Source follow-ups

The confirmed partial-consumption failures in `JewelryTask` and `LeatherTask` merit one focused issue with acceptance tests; **do not implement it in this documentation PR**. Changes should follow repository `AGENTS.md` / OpenSpec and the existing item-container storage specification.
