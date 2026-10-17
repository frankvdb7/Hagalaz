# Revision 742 skills: executable reconciliation plan

Status: design/verification matrix only. No real cache or database dump has been evaluated in this PR. This document bridges the already-present candidate tables and the exact server runtime behaviors to verify. Keep the [source ledger](skill-mechanics-source-ledger.md) and [canonical index](data/README.md) as evidence classifications.

## Data lineage and match keys

| Data set | Candidate source | Runtime definition owner | Join that must be demonstrated |
| --- | --- | --- | --- |
| Mining ores | `data/mining-ores.csv` | `MiningService.FindAllOres()`, `FindAllRocks()`, `FindAllPickaxes()` | Ore ID -> rock object ID(s) -> object definition loot table -> actual ore item(s) |
| Woodcutting trees | `data/woodcutting-trees.csv` | `WoodcuttingSkillService`, `IWoodcuttingService` | Tree object -> tree DTO -> log DTO -> log item and fall/stump object |
| Fishing | `data/researched/barbarian-fishing.csv` plus Fishing reference | `FishingSkillService`, spot tables, `IFishingLoot` | NPC definition & clicked option -> spot method -> eligible weighted fish/item ID |
| Farming | `data/researched/farming-allotments-and-flowers.csv` | `FarmingPatchTickTask`, `IFarmingService` | Patch object -> patch type -> seed ID -> varbit, growth stages and harvest output |
| Production skills | `data/researched/cooking-fish.csv`, `smithing-forging-products.csv`, `herblore-recipes.csv` etc. | Existing skill scripts and data repositories | Input item definition IDs and output IDs, amounts, level requirements, XP, menu action |
| Progression | `data/experience-levels-1-120.csv` | `StatisticsConstants` and actual character statistics | XP threshold -> base level -> client-mapped skill update, including 99/120 boundaries |

**Match rule:** names are human-facing aliases, not primary keys. Do not declare matched solely because two names are equal; preserve exact source key, decoded cache index/group/file, snapshot SHA, and version evidence.

## Verification fixtures and regression matrix

| ID | Setup | Required invariant |
| --- | --- | --- |
| GATH-01 | Inventory becomes full between scheduling and reward, using predictable loot | No XP or success message unless item mutation committed; no item is lost |
| GATH-02 | Resource object replaced/disabled while action task is queued | Stale handle cannot produce inventory or XP |
| GATH-03 | Two characters target the same depleting tree/rock | Shared depletion consistent; reward transaction cannot use stale resource object |
| GATH-04 | Character interrupted during async definitions lookup | No task queued after interrupt; event subscriptions disposed |
| GATH-05 | Pickaxe/hatchet in equipped slot, inventory, both, neither, or above level | Highest *eligible* tool chosen; illegal tool excluded |
| MINE-01 | Loot table yields no item or inventory add fails | Current code appears to grant mining XP unconditionally; reproduce and fix independently of docs |
| FISH-01 | Exactly one bait, single catch, and multiple generated loot records | Bait policy chosen using historical evidence; award and consumption atomic at intended granularity |
| FISH-02 | Spot moved/disappeared or hidden in the same cycle | Fishing task stops before any next award |
| FARM-01 | Watered, fertilized and unmodified patches with controlled RNG | Watering effect tested against current condition-removal order |
| FARM-02 | Patch saved just before growth, rehydrated after offline interval | Exact stage advancement once, no duplicate task |
| FARM-03 | Two players sharing patch object ID | Server patch state and outgoing varbit remain character-specific |
| PROG-01 | XP just below/at levels 2, 99, 120, and XP cap | Threshold and level caps consistent with server and 742 client |
| PROG-02 | Skill boost/drain followed by XP gain, logout/relog | XP-derived base level unaffected by temporary level |

## Production skill follow-up

See [production skill source audit](production-skill-source-audit.md) for verified task sequencing, smelting multi-ingredient integrity, cooking burn formula, firemaking ignition math, and configured Runecrafting multiplier thresholds. The specific Smithing and Cooking mutation-result concerns require tests against actual container semantics before classifying as confirmed defects.

## Triage and implementation rules

1. Run the already-existing `tools/reconcile_catalogs.py` only with the approved hashed fixture and authorized read-only exports. It currently addresses item/NPC/spawn comparisons, **not** skill definition extraction; do not represent a successful run as gathering-skill verification.
2. For skills, add focused read-only exports only after inspecting actual EF entities and schema. Record raw row counts, duplicate identities, unmatched foreign keys and unknown values in a separate summary.
3. Produce a discrepancy file with `skill,entry,field,repository_value,candidate_value,fixture_value,status,evidence_uri`. When fixture value is unavailable, leave blank, not inferred.
4. Write deterministic, targeted tests for reproducible gameplay bugs. Keep those fixes in separate code changes rather than embedding runtime changes inside the documentation PR.
5. Never silently flatten historically distinct crafting methods, fishing click-options or tree/resource variants into one generic system.

## Current blockers

- Approved cache archive data not provided in this connection (see `tools/README.md`).
- Populated game database not connected for actual seed and loot export.
- Exact cache-to-runtime ID joins absent for these skills.
- This PR's evidence is source-backed and candidate-based, not a gameplay certification.
