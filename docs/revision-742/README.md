# Revision 742 gameplay feature documentation

This is the version-scoped feature catalog for Hagalaz. **Only the 742 client/cache and the November 2012 gameplay baseline are authoritative for parity.** The list is intentionally broader than the current server implementation, so an entry may describe a missing system. Never interpret a linked class as proof that the feature works end-to-end.

## Historical boundary

- Version 742 was active in early to mid-November 2012. The major combat redesign launched on **20 November 2012**, around the next client revision. For version 742 implement the **earlier** combat system, not ability bars, dual wielding, or later balancing.
- By this point, the money pouch, tool belt, lodestone travel, Runespan, bonfires, clans/citadels, auras, daily challenges and Noticeboard existed; verify the *specific subset and exact state* available in the target cache.
- A later wiki page can silently describe modernized mechanics. Prefer 742 captures, dated 2012 primary material, and matching cache/codecs over present-day tables.
- Gameplay availability, UI layout, protocol packet, and server implementation are **separate** claims. Each must be established independently.

See [research evidence and source methodology](research-evidence.md) and [implementation conventions](implementation-conventions.md).

## Feature guides

### Combat and character

- [Combat](combat.md)
- [Pvp](pvp.md)
- [Magic](magic.md)
- [Prayer](prayer.md)
- [Summoning](summoning.md)
- [Progression](progression.md)
- [Auras And Customization](auras-and-customization.md)
- [Death](death.md)

### Items and economy

- [Inventory](inventory.md)
- [Equipment](equipment.md)
- [Banking](banking.md)
- [Money Pouch](money-pouch.md)
- [Trading](trading.md)
- [Dueling](dueling.md)
- [Shops](shops.md)
- [Marketplace](marketplace.md)

### Gathering and production

- [Mining](mining.md)
- [Woodcutting](woodcutting.md)
- [Fishing](fishing.md)
- [Firemaking](firemaking.md)
- [Smithing](smithing.md)
- [Crafting](crafting.md)
- [Cooking](cooking.md)
- [Fletching](fletching.md)
- [Herblore](herblore.md)
- [Farming](farming.md)

### Other skills and activities

- [Agility](agility.md)
- [Thieving](thieving.md)
- [Slayer](slayer.md)
- [Runecrafting](runecrafting.md)
- [Runespan](runespan.md)
- [Hunter](hunter.md)
- [Construction](construction.md)
- [Dungeoneering](dungeoneering.md)

### World and multiplayer

- [Travel](travel.md)
- [Quests](quests.md)
- [Daily Challenges](daily-challenges.md)
- [Social](social.md)
- [Clans](clans.md)
- [Minigames](minigames.md)
- [World And Interactions](world-and-interactions.md)

## Recommended build sequence

1. **Protect invariants:** inventory/equipment/banking/pouch, trading and dueling; close all item-loss/duplication paths before adding new economic features.
2. **Establish parity fixtures:** combat, magic/prayer, pathfinding and 742 client widget/packet mappings. Character skills and combat depend on these core rules.
3. **Finish present mechanics:** existing harvesting/production skills, familiar persistence, quests, travel and social state. Extend the files linked in each guide rather than rebuilding subsystems.
4. **Add missing features incrementally:** daily challenges and Noticeboard, Runespan, marketplace, Hunter, houses, dungeon instances and clan citadels. Each needs a bounded proposal because the runtime/persistence model may not yet support the feature.
5. **Expand per-feature content data:** skills, quests, NPC/object spawns, rewards, area rules and their ID maps once cache-backed fixture inventories are available.

## What to collect for every feature before implementation

| Artifact | Why it matters |
|---|---|
| Exact 742 cache identity: item/NPC/object IDs, interface components, config bits, animations/graphics, map placement | Prevents newer revisions leaking into 742 behavior |
| Entry point and outgoing result: player click, packet decoder/handler, widget script, authoritative domain owner | Keeps one implementation path |
| Gameplay rules: gates, timings, rewards, interruptions, anti-duplication constraints | Makes behavior reproducible |
| Lifecycle: logout, death, area change, cancellation, server restart | Defines item and state conservation |
| Storage: source of truth and hydrator/dehydrator or database schema | Prevents an in-memory-only feature |
| Tests: positive path, rejection, cancellation, capacity, concurrency, restart, client UI parity | Separates functional evidence from partial code presence |

The listed file paths were audited against the repository's `main` tree on 15 October 2026. This is a **documentation and architecture inventory**, not a completed game-content dataset; a pinned 742 cache fixture exists in the companion client repository, but its LFS bytes were not downloaded or exhaustively decoded during this research; no server integration run was performed.

## Researched gameplay data (supplement)

The initial feature guides are now supplemented with [41 per-feature historical research references](reference/README.md) and [48 machine-readable gameplay datasets](data/README.md). The reference pages include concrete 2012-era values, restrictions, recipe examples and cited sources; **not all details are verified against a specific 742 cache build**. Use the evidence gate in [research provenance](research-evidence.md) before implementing exact IDs, rates or packet behavior.

## Deeper implementation-grade research

- [Expanded 2012 data pack](data/researched/README.md) — **705 curated candidate rows across 39 researched CSVs** covering room costs/portals, spells, potions, pouches, Slayer rewards, firemaking methods, fishing variants and bonuses. The earlier 9 CSVs remain available.
- [Complete 41-feature evidence requirements](reference/feature-completeness.md) and [machine-readable feature coverage matrix](reference/feature-completeness-matrix.csv) — explicit data still required to claim full behavior parity.
- [Audited Hagalaz data ownership and schema crosswalk](reference/repository-data-crosswalk.md) — actual existing EF property names, expected cache joins, persistence owners and missing fields.
- [Deep runtime specifications](reference/README.md#vertical-slice-specifications) — Construction, Magic, Slayer, economy, gathering, Farming, Summoning and progression, with failure cases and existing method behavior.

**Limit:** even this larger data pack is not exhaustive. No pinned LFS cache was downloaded, decoded or replayed with live server tests in this documentation change. Full item/NPC/object/UI/CS2/packet mappings and complete content tables remain a separate evidence collection effort.

## Further skill research expansion

- [20 ancient curses](data/researched/ancient-curses.csv), requirements and period drain intervals — [full activation and combat-effect dossier](reference/deep-prayer.md).
- [Six combination runes](data/researched/combination-runes.csv), ingredients, altar-based XP and historical binding rules — [runecrafting state and transaction dossier](reference/deep-runecrafting.md).
- [Hunter training methods](data/researched/hunter-methods.csv), [trap limits](data/researched/hunter-trap-capacity.csv) and the [trap lifecycle dossier](reference/deep-hunter.md).
- [Ten meat-cooking recipes](data/researched/cooking-meat-products.csv) with XP/healing and an [audit of actual cooking task rules](reference/deep-cooking.md).

**Coverage note:** the expanded research catalog now contains **705 candidate rows across 39 researched tables**. The pinned client cache has still not been decoded for exact numeric resource and interface IDs.

### Expanded Prayer, Farming and Cooking catalogs

- [30 standard prayers](data/researched/standard-prayers.csv) plus [20 ancient curses](data/researched/ancient-curses.csv); [23 bones and ashes](data/researched/prayer-offering-xp.csv), including burial/scatter XP and distinct altar offering multipliers.
- [23 cooked-fish definitions](data/researched/cooking-fish.csv), with level, stop-burning level, XP and healing, complementing meat production.
- [13 allotment/flower crops](data/researched/farming-allotments-and-flowers.csv), with growth cycles, XP, protection and eligibility.

All are historically sourced candidate values, pending the exact 742 cache and Hagalaz database contents.

## Content-expansion pass: 15 October 2026

Added **223 source-linked historical candidate records in eight more machine-readable datasets**, extending the existing 354 researched records, together with [Smithing](reference/deep-smithing.md), [Crafting](reference/deep-crafting.md), [Thieving](reference/deep-thieving.md) and [Agility](reference/deep-agility.md) execution contracts. See [expanded data inventory](data/researched/README.md).

These additions cover production recipes, skill thresholds, output quantities, repeat-action cancellation, NPC and door permissions, stun and reward rules, obstacle sequencing and map collision. **The complete content catalog is still not ready**: exact index-16/18/19 object and item IDs, live widget and opcode mappings, complete drop weights, success rates and map locations require the pinned 742 cache and repeatable captures. Do not label individual feature parity complete based on this data.

## Additional Fletching and dungeon resources

Added **128** more historical candidate records across ten CSV datasets, taking the curated research expansion to **705** candidate rows. The [Fletching](reference/deep-fletching.md) and [dungeon resource](reference/deep-dungeoneering.md) dossiers provide per-stage XP, material quantities, session boundaries and failure tests. These are not verified cache exports, and cannot be loaded as authoritative runtime values.

## Item and NPC server registration index (source-backed, not cache verified)

- [281 concrete registered item IDs](data/catalogs/item-script-registrations.csv) and [383 concrete registered NPC IDs](data/catalogs/npc-script-registrations.csv), each with script source path, exact numeric cache group/subfile coordinates, source commit and evidence grade.
- [Manifest methodology, data owner crosswalk and anomalies](data/catalogs/README.md); [item/NPC prioritized remaining work](reference/items-npcs-remaining-work.md).
- This is not the complete 742 client cache roster; the pinned LFS bytes are not present and no exhaustive JS5 definitions or server DB rows were decoded here. Do not confuse declared metadata with verified fixture contents.

The [source-family coverage table](data/catalogs/source-family-coverage.csv) separates script-registered IDs by subsystem; dynamically generated fishing/Slayer registrations and complete runtime startup mappings still require database-backed extraction.

The [NPC spawn-origin inventory](data/catalogs/npc-spawn-sources.csv) distinguishes persistent database spawns, summoned followers, encounter creatures and administrative or emote-only creations. No private spawn table rows have been invented.


## First three P0 evidence tasks

The [source-to-data reconciliation workflow](tools/README.md) includes a deterministic Python comparer, SQL exports, DB-only integrity checks and synthetic unit tests. The [P0 task status table](reference/items-npcs-remaining-work.md#p0-status-after-this-work-16-october-2026) records why complete item/NPC definition, EF and spawn parity cannot yet be claimed without the approved LFS asset and populated MySQL data.

## P1 item relationships, NPC gameplay and world objects (source-backed)

- [Item identity/variants, equipment, lending, degradation and consumables](reference/deep-item-relationships.md), with [140 degradation-stage transitions](data/catalogs/equipment-degradation-script-stages.csv), [104 potion-dose stages](data/catalogs/potion-dose-script-stages.csv) and [10 script attribute/chain mismatches](data/catalogs/equipment-script-registration-gaps.csv).
- [NPC combat, weighted loot, pickpocketing, shops, dialogue and bosses](reference/deep-npc-gameplay.md), including [104 source-backed pickpocket NPC rules](data/catalogs/npc-pickpocket-source-rules.csv).
- [World-object definitions, static/DB placements, transformations, collision and feature joins](reference/deep-world-objects.md), with [321 script-registered object IDs](data/catalogs/object-script-registrations.csv) and [68 codec opcode constants](data/catalogs/object-opcode-constants.csv).
- [Remaining evidence and runtime acceptance gates](reference/p1-content-remaining-work.md). These are **source inventories and implementation dossiers**, not a fully decoded revision-742 client cache, live world DB or gameplay-complete content pack. The earlier 705 historical candidate records remain independent of these script-source catalog records.
