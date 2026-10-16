# Research reference catalog — revision 742

These **41 feature references** contain period-preserved mechanics, numbers, recipes, unlocks, behavior and target-specific extraction work. Read together with the implementation guides. Their historical sources are **candidates**, not fully verified per-packet or per-cache parity evidence.

| Feature | Guide | Period research |
|---|---|---|
| agility | [Implementation](../agility.md) | [Research](./agility.md) |
| auras and customization | [Implementation](../auras-and-customization.md) | [Research](./auras-and-customization.md) |
| banking | [Implementation](../banking.md) | [Research](./banking.md) |
| clans | [Implementation](../clans.md) | [Research](./clans.md) |
| combat | [Implementation](../combat.md) | [Research](./combat.md) |
| construction | [Implementation](../construction.md) | [Research](./construction.md) |
| cooking | [Implementation](../cooking.md) | [Research](./cooking.md) |
| crafting | [Implementation](../crafting.md) | [Research](./crafting.md) |
| daily challenges | [Implementation](../daily-challenges.md) | [Research](./daily-challenges.md) |
| death | [Implementation](../death.md) | [Research](./death.md) |
| dueling | [Implementation](../dueling.md) | [Research](./dueling.md) |
| dungeoneering | [Implementation](../dungeoneering.md) | [Research](./dungeoneering.md) |
| equipment | [Implementation](../equipment.md) | [Research](./equipment.md) |
| farming | [Implementation](../farming.md) | [Research](./farming.md) |
| firemaking | [Implementation](../firemaking.md) | [Research](./firemaking.md) |
| fishing | [Implementation](../fishing.md) | [Research](./fishing.md) |
| fletching | [Implementation](../fletching.md) | [Research](./fletching.md) |
| herblore | [Implementation](../herblore.md) | [Research](./herblore.md) |
| hunter | [Implementation](../hunter.md) | [Research](./hunter.md) |
| inventory | [Implementation](../inventory.md) | [Research](./inventory.md) |
| magic | [Implementation](../magic.md) | [Research](./magic.md) |
| marketplace | [Implementation](../marketplace.md) | [Research](./marketplace.md) |
| minigames | [Implementation](../minigames.md) | [Research](./minigames.md) |
| mining | [Implementation](../mining.md) | [Research](./mining.md) |
| money pouch | [Implementation](../money-pouch.md) | [Research](./money-pouch.md) |
| prayer | [Implementation](../prayer.md) | [Research](./prayer.md) |
| progression | [Implementation](../progression.md) | [Research](./progression.md) |
| pvp | [Implementation](../pvp.md) | [Research](./pvp.md) |
| quests | [Implementation](../quests.md) | [Research](./quests.md) |
| runecrafting | [Implementation](../runecrafting.md) | [Research](./runecrafting.md) |
| runespan | [Implementation](../runespan.md) | [Research](./runespan.md) |
| shops | [Implementation](../shops.md) | [Research](./shops.md) |
| slayer | [Implementation](../slayer.md) | [Research](./slayer.md) |
| smithing | [Implementation](../smithing.md) | [Research](./smithing.md) |
| social | [Implementation](../social.md) | [Research](./social.md) |
| summoning | [Implementation](../summoning.md) | [Research](./summoning.md) |
| thieving | [Implementation](../thieving.md) | [Research](./thieving.md) |
| trading | [Implementation](../trading.md) | [Research](./trading.md) |
| travel | [Implementation](../travel.md) | [Research](./travel.md) |
| woodcutting | [Implementation](../woodcutting.md) | [Research](./woodcutting.md) |
| world and interactions | [Implementation](../world-and-interactions.md) | [Research](./world-and-interactions.md) |

## Machine-readable data

[Candidate skill recipe/catch tables and level-to-XP thresholds](../data/README.md). Eight historical datasets and one computed XP curve are included; missing IDs and source disagreements are intentionally visible instead of guessed.

## How to turn a candidate into authoritative data

1. Record exact local client/cache revision, binary hashes and build date. The 742 series received updates within November 2012, so a revision number is not a content fingerprint.
2. Compare each entry with cache definitions, relevant Hagalaz tables, and an interaction capture from the matching client.
3. Note the authoring evidence, date, method and expected field units; never infer IDs, tick counts or locations from historical narrative.
4. Only then mark a rule as verified and implement it with tests for exact values and cancelled/failed actions.
5. Update `docs/revision-742/research-evidence.md` if material corrections affect another feature.

## Exact client evidence

[Companion client and pinned cache extraction handoff](client-cache-evidence.md) records the discovered direct 742 fixture, index identities and code-to-cache workflow. Use it before promoting a wiki-derived number or a guessed item/animation/widget ID.

## Vertical-slice specifications

| System | Deep implementation dossier |
|---|---|
| Construction | [Rooms, furniture, house ownership and guest rules](deep-construction.md) |
| Magic | [Spellbooks, targeting, cast validation and effects](deep-magic.md) |
| Slayer | [Assignment, streaks, master points, shop and co-op](deep-slayer.md) |
| Item economy | [Trade, bank, pouch, duel, marketplace and durability](deep-economy.md) |
| Gathering | [Mining, trees, fishing, cooking and firemaking task behavior](deep-gathering.md) |
| Farming | [Patch state, cycles, disease, harvest and offline persistence](deep-farming.md) |
| Summoning | [Pouches, familiars, inventory/point consumption and recovery](deep-summoning.md) |
| Progression | [Skill XP, caps, boosts and experience ownership](deep-progression.md) |

## Concrete data and Hagalaz schema

- [Expanded structured historical datasets](../data/researched/README.md): 213 additional curated rows in 12 new tables.
- [Current data ownership crosswalk](repository-data-crosswalk.md): the actual C# EF entity fields and cache index join targets.
- [41-feature data requirements and missing evidence](feature-completeness.md) and [CSV](feature-completeness-matrix.csv).

These additions complement the original feature-by-feature pages and do **not** replace their exact revision-fixture validation requirements.

### Added deep skill guides

| Feature | Implementation dossier |
|---|---|
| Prayer/curses | [All 20 curse families, activation and drain](deep-prayer.md) |
| Runecrafting | [Standard/combination outputs, multipliers and rune consumption](deep-runecrafting.md) |
| Hunter | [Method catalog, trap limits, placement and reward ownership](deep-hunter.md) |
| Cooking | [Recipes, burn probability and source-lifetime task code](deep-cooking.md) |

### More complete reference tables

- [Standard prayers (30)](../data/researched/standard-prayers.csv) and [Prayer item XP (23)](../data/researched/prayer-offering-xp.csv), including the drain bonus formula in [prayer implementation notes](deep-prayer.md).
- [Fish Cooking (23)](../data/researched/cooking-fish.csv), see [cooking task analysis](deep-cooking.md).
- [Farming allotments and flowers (13)](../data/researched/farming-allotments-and-flowers.csv), see [patch lifecycle](deep-farming.md).

## Deeper 2012 source-led implementation dossiers (additional)

- [Smithing: full 72-product candidate forging matrix, resource accounting and batch iteration](deep-smithing.md).
- [Crafting: gold jewellery/gem cutting recipes and atomic transformations](deep-crafting.md).
- [Thieving: 22 pickpocket families, ten doors, stun/loot/bonus semantics](deep-thieving.md).
- [Agility: course requirements, route-sequenced XP, 31 shortcut candidates and collision](deep-agility.md).

All values are candidate period data; no numeric IDs have been claimed without the 742 build fixture.

## Expanded Fletching and dungeon reference studies

- [Standard Fletching and projectile production stages](deep-fletching.md): 68 source-linked candidates, quantity/XP conflicts, quest-gated operations and atomic batch semantics.
- [Dungeon-only production resources and session isolation](deep-dungeoneering.md): 60 candidates, five resource families, instance lifecycle and anti-leak tests.


## Item and NPC numeric registration audit

- [Existing script-to-ID catalog, decoder/DB ownership and provenance](../data/catalogs/README.md)
- [Prioritized evidence required to complete item and NPC parity](items-npcs-remaining-work.md)

## Focused P1 content dossiers

- [Item relations and equipment/consumable states](deep-item-relationships.md)
- [NPC combat, loot, interactions and pickpocketing](deep-npc-gameplay.md)
- [Object definitions, terrain placements and collision](deep-world-objects.md)
- [P1 data/verification handoff and unresolved findings](p1-content-remaining-work.md)

Source-derived numeric rows are under [data/catalogs](../data/catalogs/README.md), separate from the historical candidate research tables.

## Combat and character mechanics (P1 #9)

- [Source-traced combat, prayers, special attacks, NPCs, magic, familiars, death and PvP](deep-combat-character-mechanics.md)
- [34 deterministic and integration test scenarios, acceptance criteria and evidence gates](combat-verification-matrix.md)
- [14 prioritized source-review findings](../data/catalogs/combat-mechanics-review-findings.csv) requiring focused test confirmation and owner-local fixes
- [Special weapon costs](../data/catalogs/combat-special-energy-script-registrations.csv), [prayer book level/drain constants](../data/catalogs/combat-prayer-source-constants.csv), and [ancient spell variants](../data/catalogs/combat-ancient-spell-script-rules.csv) — **repository source only**
