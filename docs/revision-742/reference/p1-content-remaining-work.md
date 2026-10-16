# P1 content completion handoff — items, NPCs, world objects

**Updated:** 16 October 2026. Exact 742 runtime fixture and populated MySQL rows unavailable through this connection. The three domains have source-backed guides and non-exhaustive numeric catalogs, but **do not yet meet complete-content acceptance**.

## Evidence delivered

| Domain | Direct repository data | Dossier | Why source coverage is not completion |
|---|---|---|---|
| Item relationships | 140 equipment degradation stages, 104 potion-dose stages, 281 preexisting script IDs and 10 attribute/chain gaps | [Item relationships](deep-item-relationships.md) | Complete note/lending/bought/stack edge graph, all equipment constraints and consumable effects need populated index 19 plus DB, inventory and runtime observation |
| NPC gameplay | 104 pickpocket NPC rows (18 archetypes), 383 preexisting NPC script IDs, source-verified kill and loot generator hooks | [NPC gameplay](deep-npc-gameplay.md) | No populated NPC loot weights, complete boss/dial/shops/quest records, actual NPC 18 cache, dynamic spawns and live battle captures |
| World objects | 321 object-script registrations, 68 opcode constants, index-16 shard rules and static/DB composition ownership | [World objects](deep-world-objects.md) | No populated index-16 definitions, index-5 placement/collision manifest, XTEA verified regions, complete object transformations, world DB placements or in-game options |

## P1 — Highest-value remaining execution tasks

1. **Physical cache extraction:** on an approved machine, pull the exact pinned client fixture (Git LFS) and validate checksum. Enumerate real present group/file entries for index **19**, **18**, **16** and index-5 `lX_Y`/`mX_Y` regions. Decode using Hagalaz providers/codecs, preserve hashes and missing/sparse files.
2. **Item edge verification:** export note/template/lend/bought/stack-model edges and `ExtraData` skill/quest requirements per item. Validate all referenced destinations and unstack/noted conversions. Record charge maps using exact instance extra data; don't guess static fields.
3. **Equipment coverage test:** investigate the [ten Guthan/Karil attr omissions](../data/catalogs/equipment-script-registration-gaps.csv), full worn-model slots, degradation stage terminators, logout persistence and owner-specific equipment script selection. A correct-looking ID array is not proof a callback runs.
4. **All consumables:** reconcile [26 potion families](../data/catalogs/potion-dose-script-stages.csv) against cache and server item-effect scripts; add food/drink, healing, antipoison, run restore, energy, item charges, production/combination and on-use restrictions, with event/mutation tests.
5. **Actual NPC loot:** dump `npc_definitions`, `npc_loot`, `npc_loot_items` and child relationships; join weighted kill loot vs pickpocket loot, test `Always`, count ranges and seeded weights. Audit that NPC `MaxResultCount` is populated in `LootStore`; the current initializer appears not to copy DB count unlike the item/object branches.
6. **Boss combat and dialogue:** one contract per NPC ID/phase, including attack styles, aggression exceptions, targets, death, respawn, player proximity, quest stage, shop ID and option packet. Include registered factory precedence and source conflicts.
7. **NPC pickup/stun:** reconcile the [104 source pickpocket rows](../data/catalogs/npc-pickpocket-source-rules.csv) and historical candidates; verify level/success/XP, 255 sentinel, timed stun, loot and cancel in controlled client tests.
8. **Object placements:** combine all decoded map static objects with `gameobject_spawns`, preserving `shape,rotation,x,y,z,dimension,region`. Query updated transforms, map collision and source scripts; do not treat script registration as a spawn.
9. **Object interaction domains:** verify doors, gates, ladders, portals, lodestones, mining rocks, woodcutting trees, farming patches, shop/bank/altar stations, dynamic instance geometry and world transport, each with required skill, quest, item and movement/LOS.
10. **End-to-end parity gate:** build repeatable client action captures and server tests for item transactions, equip/charge/degrade/dose, NPC roll/drop/aggro/quest/shop, object transformation/door/collision, logout/restart and dynamic instances. Check errors and cancellation paths, not merely happy-path output.

## Required completion report per domain

- Population counts and checksums for decoded IDs/placements, source branch/commit and DB snapshot identity.
- Links from every exported ID/row to source evidence. A candidate historical page is not a cache hash.
- Matched/unmatched rows after full-outer joins; missing target ID and orphan counts; explicit exception list.
- Correct runtime handler choice after dynamic factories; no silent double registrations.
- Representative client 742 packet/widget/CS2 trace and deterministic regression validation.
- Explicit **complete / partial / blocked** flag for cache, DB, scripting, client, tests and persistence.

**Current status of all three: partial/source-audited, not fully complete.** No direct archive bytes, populated MySQL data or live runtime execution were available, and none are invented in these documents.

### NPC catalog correction / boss gate

A full-source audit corrected the static script inventory: removed inactive/commented ID 13447 and restored actual pickpocket registration ID 5768. There remain 383 distinct static NPC IDs. The [boss script status matrix](../data/catalogs/npc-boss-script-audit.csv) separates 9 active concrete scripts, one wholly commented-out class and one runtime encounter controller. Verify compiled plugin registration and each boss phase with live data; do not infer implementation completeness from historical names or commented code.

## P1 #9 combat and character mechanics — source pass 16 October 2026

**Source documentation added:** [deep combat dossier](deep-combat-character-mechanics.md) and [34-scenario acceptance matrix](combat-verification-matrix.md). The directly extracted input tables cover 49 weapon special costs, 50 prayer/curse levels and base drains, and 20 ancient spell variants (levels, damage, XP, rune costs, autocast configs). [14 source-audit observations](../data/catalogs/combat-mechanics-review-findings.csv) are recorded as *hypotheses and concrete regression test cases*, not as proven production incidents.

**Highest priority before code changes:** source protection-prayer damage-type operator precedence; special-energy debit before unsupported handler; `StandardCombatSpell` calling max-hit adjustment twice. More gates: attack roll and max-hit accuracy compared with the approved 742 fixture/client; NPC wall/line-of-sight and boss overrides; prayer mutual exclusion/drain/deflection; rune debit and multi-target spells; familiar lifespan/scroll/inventory/hydration; death item ownership/skulls and Wilderness/duel familiar restrictions. Verify before fixing and keep logic in current combat/area/equipment owners; no second combat engine.

**Remaining data:** populated equipment-speed cache params, NPC combat DB records, spell DB table, familiar summon records, character death states and target client packet captures. Without executable .NET tests and these inputs, P1 #9 is **source-audited but not gameplay-complete**.

## P1 #10 interface / CS2 / varbit / packet integration — source pass 16 October 2026

Added [source-traced client/server integration](deep-client-interface-cs2-protocol-integration.md) and [32 executable-scenario specifications](client-protocol-integration-verification.md). Extracted and reconciled **108 client-to-server opcodes** with all 108 declared server payload lengths (zero mismatches), **75 registered decoder opcodes** with 33 requiring product intent classification, **71 static/conditional server encoder branch entries** across all 59 concrete encoder classes (67 distinct numeric opcodes; one declaration discrepancy: **162**), plus **75 literal CS2/varp/global-state calls from 17 contributing files (25 inspected)**. [Eight tracked findings](../data/catalogs/client-protocol-integration-findings.csv) include the conditional nested zone writer mutation and client-known player option 10 opcode 46 without server decoder.

The client remains the canonical authority for its own [164 incoming](https://github.com/frankvdb7/Hagalaz.GameClient/blob/main/docs/protocol/packet-catalogue.md) and [108 outgoing](https://github.com/frankvdb7/Hagalaz.GameClient/blob/main/docs/protocol/outgoing/registry-audit.md) packet ledgers. **Highest-value next tests:** enclosed map packet 162 non-null group, framed vs embedded encoding and opcode/header ownership; signed/transformed component clicks and stale queued widget; player menu option 10 reachability; full fixed-body byte counts and ISAAC framing; CS2 script ID/signature and onOpen/callback order, and varbit parent varp/bit validation. Full completeness blocked on pinned cache ZIP (indices 3/12/22), client/server execution and live database/runtime observation. No runtime changes or tests were executed in this documentation pass.

**Additional varbit source evidence:** [29 `SendBitConfiguration` call sites](../data/catalogs/client-varbit-source-calls.csv) across 12 contributing character/area/UI files (18 literal occurrences, 14 distinct numeric IDs; 11 dynamic expressions) complement the 75 UI CS2/varp/global call sites. They must be full-outer-joined to decoded index-22 definitions; computed IDs are not automatically enumerated or treated as physical cache presence.
