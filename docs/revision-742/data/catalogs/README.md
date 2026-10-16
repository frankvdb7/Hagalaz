# Revision 742 item and NPC numeric catalog: audited script registrations

**Scope:** this directory contains numeric identifiers literally registered in the Hagalaz server source at commit [`9c9f8622305d`](https://github.com/frankvdb7/Hagalaz/commit/9c9f8622305d18ca05fa7099848a0cdb01cf09b6). The source is the server's production script attributes, not the pinned binary cache. Entries therefore prove **which IDs a script declares**, not that the cache contains an entity, that an NPC is spawned, that the attribute was registered successfully, or that gameplay parity is correct.

## Machine-readable inventory

| Dataset | Rows | Meaning |
|---|---:|---|
| [Item script registrations](item-script-registrations.csv) | 281 | 281 distinct item IDs declared by 69 source files |
| [NPC script registrations](npc-script-registrations.csv) | 384 | 383 distinct NPC IDs declared by 122 active source files |
| [Registration review](registration-review.csv) | 3 | Abnormal or symbolic attributes requiring review |
| [NPC overlaps](npc-registration-overlaps.csv) | 1 | Same definition ID registered by more than one NPC script; inspect runtime ownership |

The numeric ID is not a world **instance** ID. The `script` column is the source class/file name, **not a decoded cache `name`**. This distinction is important for multiple NPC variants, noted items, charged equipment and NPC transformations. The `fixture_verified=no` field is intentionally repeated on every row to prevent automatic promotion to authoritative definitions.

## Deterministic 742 address calculation

- Item definitions: index `19`, group `id >> 8`, subfile `id & 255` (256 per group).
- NPC definitions: index `18`, group `id >> 7`, subfile `id & 127` (128 per group).
- This mapping is supported by the real Hagalaz `ItemTypeData` and `NpcTypeData` classes in `Hagalaz.Cache/Types/Data/`, not guessed from a nearby version.
- Tests of shard boundaries: item 255 -> group 0/file 255, 256 -> group 1/file 0; NPC 127 -> 0/127, 128 -> 1/0.

## Data owners and decode path

**Items:** `Hagalaz.Cache/Types/Providers/ItemTypeProvider.cs` reads the index-19 reference table and a group subfile, decoded by `Hagalaz.Cache/Logic/Codecs/ItemTypeCodec.cs`. `Hagalaz.Services.GameWorld/Services/Cache/ItemDefinitionProvider.cs` overlays the mapped `Hagalaz.Data/Entities/ItemDefinition.cs` database row when present, otherwise returns cache-derived fields. The index holds client-type flags, item appearance and options, while the DB adds server-specific `TradePrice`, `LowAlchemyValue`, `HighAlchemyValue`, `Tradeable`, `Examine` and `Weight`. Do not mistake cache `Value` for validated marketplace trade price. Normal item transfer and persistence still live in existing item container ownership.

**NPCs:** `Hagalaz.Cache/Types/Providers/NpcTypeProvider.cs` reads index 18 with `NpcTypeCodec.cs`. `Hagalaz.Services.GameWorld/Services/Cache/NpcDefinitionProvider.cs` overlays `Hagalaz.Data/Entities/NpcDefinition.cs`: attack/defence/death animations, respawn, attack cadence, rewards/loot references and Slayer requirements. Spawn placement belongs to world/entity services and map/script spawn records, not NPC definition identity.

**Important sparse-index behavior:** both providers create a default type for missing reference-table entries. `ArchiveSize` and `GetAll()` therefore do **not** prove every integer from zero to size has a populated definition. A full fixture dump MUST enumerate **present** reference-table group/file entries first, then decode those IDs. No fabricated `name=null` rows for gaps.

## What a complete cache-backed export must add

For each populated item index-19 record: `fixture_sha256,cache_definition_present,raw_sha256,id,name,model_id,stackable_type,members_only,ground_options,inventory_options,equip_slot,equip_type,note_id,note_template_id,lend_id,lend_template_id,bought_item_id,bought_template_id,stack_ids,stack_amounts,quest_ids,param_map`. Preserve null/sentinel versus a real 0. Join `ItemDefinition` DB source row with explicit `db_row_present`, `db_revision`/migration marker, and stored examine/prices/tradeability/weight. `Stackable`, `Noted` and degradation must follow existing `ItemTypeLogic`, not a numeric guess from `stackable_type`.

For each populated NPC index-18 record: `fixture_sha256,cache_definition_present,raw_sha256,id,name,combat_level,size,actions,model_ids,head_models,transform_ids,varp_id,varbit_id,visible_on_minimap,clickable,render_animation_id,idle_animation_id,run_animation_id,map_icon,quest_ids,param_map`. Keep server-only stats/loot/respawn, map spawn identity and live instance script as **separate joins**. NPC transform config can result in different effective appearance/actions; do not flatten to one static name. Loot IDs are not item drops: export actual weighted tables as a separate feature dataset.

**No cache-derived rows are asserted in these CSVs.** The approved 607,410,012-byte LFS fixture is documented in [client/cache evidence](../../reference/client-cache-evidence.md); its bytes were not available through this repository connector session. The source data is still actionable for tracing script ownership and planning exact definition parity.

## Validation and reconciliation policy

1. Acquire the approved pinned fixture through the companion client's documented LFS workflow in a trusted checkout and validate its checked-in SHA-256. Do not repackage/rebuild the source archive.
2. Use the existing Hagalaz C# cache type codecs or companion-client read-only fixture inspector; enumerate **populated** group/file coordinates. Export a build-identified raw/decoded manifest deterministically, never a guessed contiguous range.
3. Full-outer-join this registration catalog with fixture definitions by `(revision,index,id)`. Classify: source-mapped and fixture-present; source-mapped but missing; fixture-present without source script; multiple scripts; definition/DB mismatch.
4. Join the server EF rows separately; note which entity fields override client cache fields and keep the source for each field. Record missing gameplay rows, not just cache names.
5. For NPCs, separately join world/map spawn records and active transformations, combat, interaction option handlers and loot tables; for items, join equipment slot, trade restrictions, recipes, note variants and charge semantics.
6. Save representative client widget/packet traces, compare them against the server handler pipeline and add focused regression tests. Fix absent/misaligned registrations only after proven; do not change runtime systems as a side effect of data documentation.

## Known source anomalies and review gates

- `Hagalaz.Game.Scripts/Areas/Edgeville/Npcs/Wilfred/Wilfred.rsnpc.cs` derives from `NpcScriptBase` but has **`[ItemScriptMetaData([4906])]`**, not the NPC attribute. The ID is **not** listed as an item or claimed to be a working NPC script. Check registration and correct in a focused code change.
- The abstract `AuraItemScript` declares an empty array; that is not an item ID or an exception.
- `SilverBar.rsitem.cs` uses a symbolic constant; it was resolved at source commit from `CraftingSkillService.SilverBar = 2355`.
- NPC IDs 5362 appear under multiple source script registrations; determine if this is intentional or ambiguous before making one authoritative handler.
- The source inventory is **not exhaustive** of every 742 item/NPC or every hardcoded use in the repository. Scripts without literal metadata, database-driven definitions, resource files and runtime spawns require an independent inventory. An ID can be referenced by multiple features even if registered to only one script.

## Explicit remaining gates

See [the feature completeness matrix](../../reference/feature-completeness-matrix.csv) and [remaining data plan](../../reference/items-npcs-remaining-work.md) for a prioritized, bounded backlog. Don't introduce a new central item/NPC data storage system: use the existing `ItemTypeProvider`, `NpcTypeProvider`, EF repositories and feature-specific services.

## Coverage by source family and dynamically registered NPCs

The [source-family summary](source-family-coverage.csv) groups existing registrations by the **directory of their script implementation**, not by cache type or actual in-world availability. The 281 item IDs and 383 NPC IDs do not represent an exhaustive 742 content census.

**Additional registration mechanisms are NOT included in the numeric CSVs** because the necessary database rows were not available:

- `Hagalaz.Game.Scripts/Skills/Fishing/FishingSpotScriptFactory.cs` obtains NPC IDs dynamically from `IFishingService.FindAllSpots()` and each definition's `NpcIds`.
- `Hagalaz.Game.Scripts/Skills/Slayer/SlayerMasterScriptFactory.cs` obtains NPC IDs from `ISlayerService.FindAllSlayerMasterTables()`.
- `Hagalaz.Services.GameWorld/Factories/FamiliarScriptFactory.cs` consumes discovered NPC types and selects familiar scripts.
- `Hagalaz.Services.GameWorld/Providers/NpcScriptProvider.cs` registers all `INpcScriptFactory` outputs with `TryAdd`; a duplicate logs a warning, and **the earlier registration wins**. Therefore NPC 5362, registered on both `BrutalDragon` and `StandardDragon`, is not guaranteed to resolve to the specialized script. Both classes share the same inherited implementation at present; nonetheless, this is an ambiguous ownership path and should be clarified rather than auto-overwritten.
- `Hagalaz.Services.GameWorld/Providers/ItemScriptProvider.cs` likewise uses `TryAdd` for item script IDs.
- `Wilfred` is declared using an item attribute on a class deriving `NpcScriptBase`. `NpcScriptMetaDataFactory` only reads `NpcScriptMetaDataAttribute` for concrete NPC scripts, so that attribute **does not supply an NPC mapping**. This is a concrete registration gap unless another factory supplies the ID.

The runtime provider plus all registered factory outputs are authoritative for **which script actually gets selected**; static metadata counts alone cannot establish that mapping. A complete audit should dump `(id,selected script type,origin factory)` during a controlled startup, then compare it against the CSVs and actual cache definitions.

## Spawn origin classes (source audited)

[Source-backed spawn ownership inventory](npc-spawn-sources.csv) separates persistent database spawns from temporary summoning, boss/minigame, emote and admin-generated NPCs. These are **source-backed spawn mechanisms, not a list of actual persisted instances**. In particular, entity script builders must not be counted as NPC spawns when they only call Build. Dynamic spawns are instance/scenario owned, not rows in npc_spawns.

## New P1 source-extracted datasets

| File | Records | Scope and constraints |
|---|---:|---|
| [Item equipment degradation stages](equipment-degradation-script-stages.csv) | 140 | Seven script families, four equipment parts by five source-declared stages, exact next-ID rules, 22500 noninitial ticks and 0.80 death factor; not cache verified |
| [Potion dose stages](potion-dose-script-stages.csv) | 104 | 26 named four-ID sequences; dose transitions based on base `Potion` script, last dose to vial ID 229; no claim that all drinks/actions are historically validated |
| [Equipment script coverage review](equipment-script-registration-gaps.csv) | 10 | Five source-chain IDs absent from each of two equipment attributes; may be intentional specialized ownership, requires runtime check |
| [NPC pickpocket source rules](npc-pickpocket-source-rules.csv) | 104 | 18 archetypes, level/XP/fail damage and extra-loot thresholds; values are server source, not historic proof |
| [World object script registrations](object-script-registrations.csv) | 321 | Numeric object IDs and index-16 shard coordinates registered in 116 object-script files; not actual placements |
| [Object opcode names](object-opcode-constants.csv) | 68 | Decoder constant names/values, not observed fixture opcodes |

The object-script scanner excluded the `GoldBar.rsitem.cs` class because its declared `GameObjectScriptMetaData` sits on an `ItemScript`, not a working object handler. It is documented as a source anomaly in [world objects](../../reference/deep-world-objects.md). Caveat: search-derived registrations can miss dynamic registrations and future source files; the CSVs should be compared against reflection-based startup inventories and the pinned cache before being called exhaustive.

## NPC source correction and boss scripts

The [boss source audit](npc-boss-script-audit.csv) classifies 11 representative files by **active compiled script**, **inherited/no-override script**, **commented-out class**, or **runtime encounter controller**, and lists the relevant hooks. A previous search-snippet pass falsely counted the **commented-out** NPC 13447 class and omitted source-declared cave goblin 5768; both errors have been corrected in the numeric NPC registration CSV. The unique static NPC IDs remain **383**, but their composition changed and the number of contributing active source files is **122**. The [registration review](registration-review.csv) records both changes. No boss gameplay parity has been inferred from class names.

## P1 #9 character combat source tables

| Dataset | Rows | Verified scope |
|---|---:|---|
| [Special weapon energy](combat-special-energy-script-registrations.csv) | 49 | Item-script IDs and raw energy cost for 19 explicit source equipment scripts; not a complete weapon list |
| [Prayer/curse source levels and drain](combat-prayer-source-constants.csv) | 50 | 30 normal / 20 curse enum-indexed server constants; base milliseconds per prayer point (gear and tick adjustments excluded) |
| [Ancient combat spell parameter arrays](combat-ancient-spell-script-rules.csv) | 20 | Blood/Ice/Miasmic/Shadow/Smoke: 4 ordered variants each; levels, XP, base damage, rune names/counts and autocast config; DB and client verification pending |
| [Combat code review findings](combat-mechanics-review-findings.csv) | 14 | Source-derived risk/test hypotheses, not passing runtime test outcomes |

The [full combat dossier](../../reference/deep-combat-character-mechanics.md) specifies how item-definition speeds, hit rolls, magic, familiars and PvP permissions interact. Distinguish source rows from historical candidates or physically present binary resource IDs; do not import undocumented default values as revision-specific content.

## P1 #10 — interface, CS2 and protocol cross-repository data

| Dataset | Rows | Source check and coverage |
|---|---:|---|
| [client-server-outbound-packet-crosswalk.csv](client-server-outbound-packet-crosswalk.csv) | **108** | Client-to-server opcode 0..107; client vs server body framing **108/108 exact matches**, **75** server-registered decoder opcodes; remaining 33 require intent review, not automatic implementation |
| [server-to-client-encoder-crosswalk.csv](server-to-client-encoder-crosswalk.csv) | **71** | All **59** concrete server encoder classes; 67 distinct opcode values, 70 source framing-kind matches and one clear mismatch at **162**; payload bytes and runtime path not proved |
| [interface-cs2-var-state-source-calls.csv](interface-cs2-var-state-source-calls.csv) | **75** | Literal CS2, standard varp, varbit or global CS2 calls from **17 contributing source files (25 inspected)**, not exhaustive dynamic calls or client-cache verified |
| [client-protocol-integration-findings.csv](client-protocol-integration-findings.csv) | **8** | Prioritized source findings and focused runtime test expectations |

These are Hagalaz-versus-GameClient **source reconciliation records**, not new client opcode authorities; consult the linked [dossier](../../reference/deep-client-interface-cs2-protocol-integration.md). One confirmed declaration error is the enclosed zone update's server Fixed framing vs client variable-short (162). The nested opcode-writer metadata path also requires direct tests.
