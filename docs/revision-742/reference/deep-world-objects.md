# Revision 742 — world objects, placements, interaction and collision

**Source:** Hagalaz source commit [`9c9f8622305d`](https://github.com/frankvdb7/Hagalaz/commit/9c9f8622305d18ca05fa7099848a0cdb01cf09b6) and companion-client cache investigation. **No 742 index-16 or index-5 binary records decoded in this pass**; exact world-wide object IDs, placements and XTEA keys are not verified.

## 1. Three layers must not be collapsed

| Layer | Authoritative source and representation | Implication |
|---|---|---|
| **Object definition** | `Hagalaz.Cache/Types/Data/ObjectTypeData.cs`, index **16**, group `id >> 8`, file `id & 255`; `ObjectTypeProvider` / `ObjectTypeCodec` | Size/shape-dependent models, action slots, colors/textures, collision flags, varp/varbit transformations, animations, quests and params. A definition is **not a placed object**. |
| **Static map placement** | `Hagalaz.Cache/Types/Providers/MapProvider.cs` index **5**: named `l{regionX}_{regionY}` object group; terrain `m{regionX}_{regionY}`; `MapCodec.DecodeObjectData` | Object ID + **shape type, rotation, local X/Y, plane**. Object group may require XTEA. A static map placement is per region and may not appear in DB `gameobject_spawns`. |
| **Database/runtime placement** | `GameobjectSpawn(spawn_id,gameobject_id,coord_x,coord_y,coord_z,face,type)` plus script-generated dynamic objects | DB placement overlays are separate from static map data. Runtime object transformations/animations may change identity or collision without altering either source record. |

`MapRegionLoader.PrepareAsync` decodes static map objects, queries `GameobjectSpawn` by bounds, combines both sets of IDs, obtains definitions once, constructs static placements with `AsStatic()` and DB placements with their stored face and shape. `ApplyPreparedRegion` flags terrain collision before adding all game objects, then ground items; failed region initialization rolls back created resources. This composition is already implemented — **do not build a duplicate region-loader or a second world-object registry**.

The correct per-placement key includes `world_dimension,region_id,source_kind,source_record_or_sequence,plane,absolute_x,absolute_y,shape,rotation`. Object definition ID alone is not unique: many doors/trees/rocks share the same type; different shapes/rotations occupy different clipping edges.

## 2. Index-16 decoding and transformation controls

The directly exported [68 object codec opcode constants](../data/catalogs/object-opcode-constants.csv) are source-owned from `ObjectTypeOpcodes.cs`; their presence in the codec **does not** mean every opcode actually occurs in the approved fixture. Important semantics:

- `1` models, `2` name, `14/15` size X/Y, `17/18/27` clipping/solid toggles, `19` interactability, `24/106` animation, `30–34` five option strings.
- `77` and `92` transform selectors; `ObjectType.TransformToIDs`, `VarpBitFileId`, `VarpFileId` must be exported as a graph and evaluated against **player/world config**. A transformation may yield `-1` (absent object); never force a display model for such a state. Retain base and resolved type separately.
- `82` hidden, `91` members, `160` quest IDs, `249` extras/params; `73/74/75` ground obstruction, gateway and item support; `69` surroundings, `107` map icon; ambience, textures and map sprite entries are separate.
- After decode `ObjectType.AfterDecode()` infers interactability from model shape/option slots, derives item support from clipping and treats `Gateway` as unclipped/non-solid. The explicit `ObjectTypeEventHook` runs `AfterDecode`. Document **post-decoding** effective fields; raw opcode presence alone is insufficient.

Export one `object-definitions-742.csv` row per populated cache record with source hash; `object-transform-edges-742.csv` one edge per selector branch; `object-placements-742.csv` one row per static/DB record; `region-collision-742.csv` only if reproducibly derived from actual terrain/object placement and existing collision semantics. Keep sparse entries absent; default `ObjectType` instances returned by `Get()` for non-existent IDs are not valid fixture evidence.

## 3. World-map and collision correctness

`MapProvider.DecodeRegion` decodes terrain and placed objects. `DecodePart` supports 8x8 chunks, rotation and bridge height adjustments, checking `terrainData[1]` bridge flags. `MapRegionLoader` calls `region.FlagCollision(..., CollisionFlag.FloorBlock)` for terrain restrictions. `MapRegionService` / `MapRegion` own object clipping, dynamic flags and region residency. World maps and dynamic instances must not be flattened to zero dimension.

Object shapes and rotation affect what is blocked (tile, side, corner, wall projection, projectile), not simply `Solid=true` => entire tile blocked. For doors, test both player pathing and NPC line-of-sight before/after a rotation, including diagonal/corner cases, ranged projectiles, a second client in range and scene update. The current `GameObjectService.UpdateGameObject` changes ID and publishes an add update; rotation changes explicitly unflag and reflag collision. Changing ID without recomputing shape/definition/clipping deserves a focused characterization test before refactoring.

Never assume a coordinate mentioned in a historical guide is an actual fixture placement; direct evidence is index-5 region decoding or a real `gameobject_spawns` row. Respect encrypted landscape groups and do not replace missing XTEA keys with all-zero keys as if those keys were confirmed.

## 4. Interaction owners and source-linked handler catalog

[321 numeric object script registrations](../data/catalogs/object-script-registrations.csv) were sourced from `GameObjectScriptMetaData` attributes. Each row records `definition_id,index=16,group,file,script,source_path,source_commit` and marks cache presence **unverified**. A script binding is **not** a static placement, working option or proof of revision-742 cache presence. Some scripts cover dozens of object IDs; some are bound dynamically or by source-specific factories. Verify `GameObjectClickType`, interaction distance, active transform, object shape, character states, script ownership and server packet handling.

Source-backed examples:
- `GameObjects/Doors/DoorObjectScript.rsobj.cs`: option 1 alternates rotation according to the object's original orientation using `GameObjectService.UpdateGameObject`; collision follows the rotation update. The implementation is not a generic open/close-object-ID replacement for all doors.
- `GameObjects/Banks/BankBooth.rsobj.cs`: options 1/2 open widget **762**. Script attribute includes bank booths and counters; do not infer their coordinates.
- `Skills/Thieving/Thieving.cs`: stall and chest source tables associate object IDs, level, XP and a loot-table reference; the object needs a `GameobjectDefinition.LootTableId` server join and must still exist at its clicked location.
- `Areas/*/GameObjects/*`: ladders, levers, portals, tunnels, cave entrances, guild gates, trap doors, stairs and shortcuts have per-script plane/coordinate and requirement rules. Preserve dimension and arrive destination rather than hardcoding a generic teleport.
- `GameObjects/Lodestone.rsobj.cs` and `GameobjectLodestone`: waypoint game-object ID, widget button, state and target location are DB/config-driven; examine activation, destination/arrival and unlock separately.
- One source anomaly: `Skills/Crafting/GoldBar.rsitem.cs` subclasses `ItemScript` but carries **`GameObjectScriptMetaData([CraftingSkillService.GoldBar])`**; this is not a valid object-handler registration. Record this before changing the attribute in an implementation PR.

## 5. Domain object joins

| Content | Persisted definition | Required world/behavior joins |
|---|---|---|
| Mining | `SkillsMiningRockDefinition(RockId,ExhaustRockId,OreId)` | Actual placed rock ID -> ore and tool; exhaustion transform and respawn; shared depletion vs per-character state; free inventory slots and cancellation |
| Woodcutting | `SkillsWoodcuttingTreeDefinition(TreeId,StumpId,LogId)` | Placed tree -> log/tool, successful chop chance, fall/stump transition, respawn; per-region collision updates |
| Farming | `SkillsFarmingPatchDefinition(ObjectId,Type)` and `CharactersFarmingPatch` | Patch instance/state, seeds, growth cycles, weeds, compost/watering, disease/death, harvesting and persistence |
| Doors/gates/shortcuts | Object script attribute + map placement | Shape/rotation, required skill/item/quest, collision, path and line-of-sight before/after interaction |
| Ladders/portals/levers | Area script + destination coordinate | Planes/world dimension, status gates, arrival collision and interaction range |
| Shops/banks/altars/interactive stations | Script + UI/CS2 + object ID | Operation 1..5 and item-on-object dispatch; dialogue, bank or crafting interface; complete atomic item/XP transfer |

`GameObjectService.FindGameObjectDefinitionsByIdsAsync` applies DB `GameobjectDefinition` examine and loot-table overlays without discarding decoded cache type; mining and woodcutting tables refer to `GameobjectId` as specializations. For world object drops (stalls/chests), object loot tables are **not NPC loot tables**, even though both share `LootGenerator`.

## 6. Exact export and acceptance tests

1. Verify pinned fixture SHA; enumerate index-16 populated IDs and decode with `ObjectTypeCodec`. Include `raw_sha256` and optional presence of model/animation/varp IDs.
2. For every accessible region in index 5, resolve named `l` and `m` group, apply known XTEA appropriately, decode all placements and terrain, record region keys/provenance without leaking private key material. Preserve region local vs global coordinates and plane and distinguish static from DB records.
3. Query authoritative `gameobject_definitions`, `gameobject_spawns`, `skills_mining_rock_definitions`, `skills_woodcutting_tree_definitions` and `skills_farming_patch_definitions` through a read-only snapshot. Flag orphan IDs and missing transformed targets. Do not automatically generate missing rows.
4. Join the numeric script catalog, quest/skill gates, widget components and server action routes; record `source_handler` and `client_operation` separately.
5. Test index shard boundaries 255/256, sparse IDs, 8x8 dynamic part rotation, bridge/plane, both sources placing an object on one tile, collisions across wall orientation, door rotation twice, expired/hidden transforms, failure/rollback and immutable static region source.

**Still missing:** complete index-16/object cache roster, full index-5 map and region placements, live DB spawns and world dimensions, decoded transformation graph, full collision maps, all agriculture/mining/tree content and 742 client click/scene packet parity. Data collection must precede any claim of full world-object completion.
