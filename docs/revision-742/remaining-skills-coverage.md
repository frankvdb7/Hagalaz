# Revision 742: remaining skills coverage inventory

**Scope:** implementation discovery from repository code search, not functional verification. Files below establish *some* code exists, **not** that a feature is complete. See [source ledger](skill-mechanics-source-ledger.md) for the 25 server/client skill-ID mappings and [gathering audit](gathering-skills-and-progression-audit.md) for the initial skills.

| Skill | Source entrypoints observed | Next evidence to extract |
| --- | --- | --- |
| Cooking | `Skills/Cooking/CookingTask.cs`, `RawFood.rsitem.cs`, `FoodScriptFactory.cs` | Raw-to-cooked/burnt item IDs, cooking level, XP, burn rate vs level and range, fire/range differences, batch stopping |
| Smithing | `Skills/Smithing/Smithing.cs`, `SmeltTask.cs`, `ForgeTask.cs`, `Anvil.rsobj.cs` | Ore/bar recipes, fuel, hammer, metal tiers, bars consumed, XP, UI/production count |
| Firemaking | `Skills/Firemaking/StandardLog.cs`, `FiremakingTask.cs` | Log/fuel IDs, XP, timing, bonfire rules, collision and fire lifetime |
| Fletching | `Skills/Fletching/FletchingAmmo.cs`, `FletchingBow.rsitem.cs`, `FletchingTips.rsitem.cs` | Unstrung/strung bow, shafts, feather, ammo and crossbow recipe chains, amounts, XP, interface parameters |
| Crafting | `Skills/Crafting/SilverTask.cs`, `LeatherTask.cs`, `JewelryTask.cs` | Hide, pottery, gem, glass, jewelry and silver recipe relationships, equipment/tool requirements |
| Herblore | `Skills/Herblore/Herbs/HerbloreSkillService.cs`, `Potions/Potion.cs` | Grimy/clean herb IDs, vial/water/unfinished/finished potions, dose combinations and XP, potion effects |
| Runecrafting | `Skills/Runecrafting/Runecrafting.cs`, `Rift.rsobj.cs`, `Altar.rsobj.cs` | Essence tiers, altar IDs, talismans/tiaras, rune multiplier thresholds, XP, pouch rules |
| Agility | `Skills/Agility/Agility.cs`, `Courses/Gnome/*` | Course obstacle sequence, requirement levels, obstacle XP and full-lap bonus, failure and pathing |
| Thieving | `Skills/Thieving/Thieving.cs`, `Stall.rsobj.cs`, `StandardStealTask.cs`, `PickPocketDefinition.cs` | Stall/NPC options, success odds, stun/damage/aggression, respawn, item weights, XP |
| Slayer | `Services.GameWorld/Model/Creatures/Characters/Slayer.cs`, `Skills/Slayer/SlayerMaster.rsnpc.cs` | Master/task pools, level/combat requirements, weights, task count, point rewards, unlocks and restrictions |
| Hunter | No domain implementation demonstrated by targeted code search | Check other names/registrations before labeling unsupported; enumerate traps, catchable NPCs and XP |
| Construction | No domain implementation demonstrated by targeted code search | Verify instanced houses, furniture recipes, build/remove actions, room layouts and XP |
| Summoning | `Skills/Summoning/SummoningSkillService.cs`, `PouchScreen.rsi.cs`, `ScrollScreen.rsi.cs` | Pouch/scroll recipes, shards/charms, familiar summoning points, timers, abilities and XP |
| Dungeoneering | Skill constants and statistics helpers were found; no domain gameplay path proven by targeted search | Verify dungeon lifecycle, party allocation, floors, complexity, rewards and XP independently |


## Relationship to the canonical revision-742 research pack

The latest `main` already contains feature guides under `docs/revision-742/`, deeper source-backed references under `docs/revision-742/reference/` and per-feature candidate tables under `docs/revision-742/data/` and `data/researched/`. **Use those as the primary feature specifications and candidate data.** This page is strictly a targeted *code-ownership/verification-gap ledger*, not a claim that other feature documentation is missing.

Examples: `reference/deep-cooking.md`, `reference/deep-farming.md`, `reference/deep-progression.md`, `reference/deep-slayer.md`, `reference/deep-summoning.md`, `data/researched/herblore-recipes.csv`, `data/researched/summoning-pouches.csv` and `data/experience-levels-1-120.csv`. Before writing new facts, reconcile against those resources and preserve their evidence tags. The phrase “no implementation demonstrated by targeted search” below is **not** evidence of absent implementation or absent documentation.

## Cross-cutting implementation and evidence checklist

For every skill record a complete implementation row with: requirement level; input items and counts; outputs and counts; XP and applicable bonus; probabilistic and deterministic stages; game tick duration; XP award point; failure/rollback behavior; inventory/equipment/skill/quest gating; client animation, sound, widget, CS2, varbit and message IDs; interruption; multiplayer visibility; persistence; unit/integration test anchors; historical evidence and confidence.

**Future follow-up strategy (not acceptance criteria for PR #522):** (1) export populated database definitions and joins, (2) link decoded revision-742 cache IDs, (3) verify historical formulas and interaction captures, (4) test happy-path, cancellation, depletion and rollback, (5) track missing functionality as focused issues. Do not replace source-backed historical values with data from modern gameplay variants. Avoid new abstractions before existing domain services have been inspected. Keep code modifications separate from this evidence pass.

## Known limitations of this pass

A targeted code search cannot establish the *absence* of a feature; routes may live outside conventional skill folders or be implemented dynamically. These rows are a work queue, not a completion certification. There is no accessible populated runtime database or complete decoded cache snapshot in this investigation, so exact recipe matrices, IDs, probabilities and timings remain unverified. The observed entrypoint file names are grounded in repository search results.

## Additional source extraction completed on this PR

- Smithing: [18 smelting ingredient links](data/catalogs/smithing-smelting-source-definitions.csv), [106 forging products](data/catalogs/smithing-forging-source-definitions.csv), [integrity notes](smithing-source-catalog-and-integrity.md) and issues [#524](https://github.com/frankvdb7/Hagalaz/issues/524), [#525](https://github.com/frankvdb7/Hagalaz/issues/525).
- Fletching: [90 exact source recipe variants](data/catalogs/fletching-source-recipes.csv) and [crosswalk](fletching-and-runecrafting-source-crosswalk.md); no claim of verified client registration.
- Runecrafting: source-backed mixed-essence eligibility bug tracked as [#527](https://github.com/frankvdb7/Hagalaz/issues/527).
- Farming: verified watering protection order defect tracked as [#526](https://github.com/frankvdb7/Hagalaz/issues/526).

**Scope closeout:** PR #522 delivers the source-anchored audit and normalized declaration catalogs listed above. Populated database exports and cache-fixture verification are expressly deferred; neither blocks completion of this documentation change. Actual historical parity and runtime repairs remain separate, tracked work. Avoid duplicating existing historical candidate CSVs.
