# Revision 742 item and NPC numeric catalog: audited script registrations

**Scope:** this directory contains numeric identifiers literally registered in the Hagalaz server source at commit [`9c9f8622305d`](https://github.com/frankvdb7/Hagalaz/commit/9c9f8622305d18ca05fa7099848a0cdb01cf09b6). The source is the server's production script attributes, not the pinned binary cache. Entries therefore prove **which IDs a script declares**, not that the cache contains an entity, that an NPC is spawned, that the attribute was registered successfully, or that gameplay parity is correct.

## Machine-readable inventory

| Dataset | Rows | Meaning |
|---|---:|---|
| [Item script registrations](item-script-registrations.csv) | 281 | 281 distinct item IDs declared by 69 source files |
| [NPC script registrations](npc-script-registrations.csv) | 384 | 383 distinct NPC IDs declared by 123 source files |
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
