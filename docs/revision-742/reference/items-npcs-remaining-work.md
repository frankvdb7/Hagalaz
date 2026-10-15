# Remaining work after item/NPC registration inventory — revision 742

This plan distinguishes documented source references from **missing direct cache/gameplay evidence**. Current inventory: 281 unique script-registered item IDs and 383 unique script-registered NPC IDs, neither independently validated against the LFS cache.

## P0 — complete the exact 742 cache definition export

1. Pull the companion-client pinned `tests/fixtures/cache742/cache.zip` via Git LFS from an authorized local checkout. Check the SHA-256 sidecar, record client commit and cache hashes and decode from an immutable local copy.
2. Enumerate **actually populated** index 19 and index 18 group/file entries using the existing C# cache codecs or the client read-only inspector; `GetAll()` is capacity-based, not a populated manifest.
3. Export *all* populated items and NPCs, with stable `(revision,index,id)`, names, actions, item note/stack/equip/members/params; NPC combat level, transforms, option slots, models, animations and flags; hash each raw definition. Record total group counts, populated definition counts, decode failures and excluded/sparse records.
4. Reconcile every registration CSV row against the cache, reporting missing and extra IDs; promote to `fixture_verified=yes` only after positive cache evidence. Record each conflicting definition with source field and test fixture.

## P0 — server authority, correctness and data joins

5. Dump Hagalaz item/NPC EF rows and presence flags: item trade values, alchemy, tradeability and weight; NPC combat/respawn, loot, pickpocket loot, attack animations and Slayer. Treat a missing server row and a cache-only definition differently. Confirm `ushort` keys can represent all populated IDs.
6. NPC spawn inventory: enumerate spawns from authoritative world map/spawn data with `npc_id,world_id,region_id,plane,x,y,rotation,movement_area,spawn_source`; find orphan spawns and multiple instances. No fixed spawn positions were asserted in this pass.
7. Finish item identity links: notes, bank placeholders if period-supported, charge/degrade/lend/bought/stack variants, equipped slot conflict, trade restrictions, drop/reclaim/quest use, and each relevant item/recipe catalog.

## P1 — actionable gameplay parity

8. NPC click option resolution for shopkeepers, Slayer masters, pickpocket targets, banking, familiar interactions, quest and boss NPCs. Map actual 742 actions, client widget/CS2 and outgoing packets to server handlers. A registered `NpcScript` does not prove its menu option is correctly wired.
9. Combat coverage: NPC stat/attack style/speed, hit chances, pathfinding/LOS, death and respawn, looting, hostile aggression, attack/defence sequences, quest conditions and drops with probability weights.
10. Item content coverage: every recipe input/output ID, runes/ammunition/equipment stats, tool-belt eligibility, consumables, item-on-object/NPC/item, shop/market offers and persistence rules; verify no duplication, item loss or ghost XP on canceled tasks.
11. Investigate NPC registration overlap(s), and the Wilfred wrong-attribute issue in [registration review](../data/catalogs/registration-review.csv), with focused tests before code changes.

## P1 — client capture and tests

12. Client/packet fixtures for NPC option 1..5, item action options, noted/unnoted presentation, charge display, inventory swaps, pickpocket/stun, familiar interactions, shop/bank UI, target selection, combat/death and region loading.
13. Deterministic export diff and CI tests: fixture hash, populated index counts, mapping arithmetic/shard boundaries, ID/source uniqueness, forbidden title audit, invalid note/lend links, missing server overrides, NPC transform cycles, spawn orphan records and protocol-action mismatch.
14. Add per-feature completion gates (cache, server DB, world spawns, script, live trace, tests, persistence) and reject claims of full 742 parity until each relevant gate has evidence.

## P2 — subsequent non-item/NPC datasets

15. Index-16 world objects + placements/collision, including farming, mining, trees, doors and quest interactions.
16. Index-3 widgets, index-12 CS2 and directional protocol packet payloads/options for each feature.
17. Index-20/21 sequences/effects, model/material assets, index-22 varbits, quests, regions and map/nav meshes.
18. Full content (shops, drops, boss mechanics, minigames, houses/dungeons, task/quest graphs and remaining skill tables), then server/browser functional and restart testing.

The order matters: a missing cache/durable data source is not solved by inventing an item/NPC ID in a script. Use existing domain owners; do not build a replacement catalog framework just for documentation.

## Dynamic registration census (P0 prerequisite)

Before declaring this static inventory complete, query the actual EF-backed `FishingSpotScriptFactory` and `SlayerMasterScriptFactory` results and perform a controlled `NpcScriptProvider.LoadAsync` initialization. Export selected script type and all duplicate warnings; source metadata alone omits database-derived NPC IDs. NPC 5362 is a static duplicate where registration order determines which script wins. Verify any special handling for familiar script selection and NPC 4906 Wilfred's malformed attribute with focused integration tests.

## Spawn origin audit

The [NPC spawn source inventory](../data/catalogs/npc-spawn-sources.csv) identifies nine independently scoped spawn paths, including persistent DB, familiar summon/restore, minigame waves, Glacor minions and administrative commands. Real counts/coordinates remain unavailable without database rows and live instance captures. Never merge event-generated transient NPCs into npc_spawns. The region loader projects persistent database NPC spawn coordinates into dimension 0.
