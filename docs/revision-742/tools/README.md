# Item / NPC / spawn reconciliation for revision 742

This folder contains a small read-only standard-library Python comparer and SELECT-only MySQL queries. **It is not an item/NPC cache decoder.** No complete cache or populated database exports are included in the repository. All conclusions from its output require independently verified source data.

## Source ownership

- Items: index 19, group = id >> 8, file = id & 255. Hagalaz.Cache/Types/Providers/ItemTypeProvider.cs and ItemTypeCodec.cs decode records.
- NPCs: index 18, group = id >> 7, file = id & 127. NpcTypeProvider.cs and NpcTypeCodec.cs decode records.
- Overlay: Hagalaz.Services.GameWorld/Services/Cache/ItemDefinitionProvider.cs and NpcDefinitionProvider.cs read optional database overrides.
- Spawns: Hagalaz.Data/Entities/NpcSpawn.cs and Hagalaz.Services.GameWorld/Data/MapRegionLoader.cs, using NpcSpawnRepository and NpcProfile. Persisted rows are for dimension 0; region id is ((x >> 6) << 8) | (y >> 6).
- Dynamic NPC registrations: FishingSpotScriptFactory and SlayerMasterScriptFactory query DB definitions. NpcScriptProvider.LoadAsync uses TryAdd; duplicates cannot safely be interpreted from one static source file alone.

## Inputs required from an authorized checkout

1. Use the approved companion-client Git LFS fixture at tests/fixtures/cache742/cache.zip. The fixture sidecar checksum is 2674f30b06bb7831fd51711b769e62208ae8236bde399f3fee1346ef03c891b1. Never use a default writable cache or repack the fixture.
2. Export **all populated** cache index-19 and index-18 definitions using the existing Hagalaz cache codecs or the companion-client read-only inspector. Do not treat ArchiveSize or GetAll as proof of populated IDs. The two normalized CSVs MUST include the columns id,index,group,file,name,raw_sha256,fixture_sha256, with actual source hashes for each raw definition and the approved full-archive hash in every row. Additional decoded columns are welcome: item options/note/stack/equip/members/params, NPC actions/models/transforms/combat/animation/params. Empty and missing references must not be written as fabricated valid records.
3. On a trusted machine, use a **read-only MySQL connection** to export db-items.tsv, db-npcs.tsv and npc-spawns.tsv using the corresponding queries in read-only-export.sql. Use an approved MySQL client (batch/raw output with column headers); keep credentials out of commands and logs. Further queries cover NPC statistics, bonuses, loot and dynamic fishing/Slayer registrations. Database dumps might contain data subject to local repository policies: review before publication.
4. The already committed input files are data/catalogs/item-script-registrations.csv and npc-script-registrations.csv. These are source declarations, not necessarily active runtime registrations.

Run the following from docs/revision-742 using Python 3.11+:

~~~shell
python3 tools/reconcile_catalogs.py \
  --fixture-archive /trusted/cache742/cache.zip \
  --expected-sha256 2674f30b06bb7831fd51711b769e62208ae8236bde399f3fee1346ef03c891b1 \
  --cache-items /trusted/cache-items.csv \
  --cache-npcs /trusted/cache-npcs.csv \
  --db-items /trusted/db-items.tsv \
  --db-npcs /trusted/db-npcs.tsv \
  --npc-spawns /trusted/npc-spawns.tsv \
  --item-registrations data/catalogs/item-script-registrations.csv \
  --npc-registrations data/catalogs/npc-script-registrations.csv \
  --out /trusted/results
~~~

The outputs are items.csv, npcs.csv, npc-spawns.csv, issues.csv and summary.json. The utility rejects wrong fixture hashes, incorrect cache group/file locations, missing raw-definition hashes, and duplicate primary keys. It reports missing cache definitions, different server display names, multiple script registrations, invalid NPC spawn boundaries, orphans and suspicious planes. No data is modified.

**Evidence boundary:** a valid archive SHA and matching string in a supplied CSV do not prove that the CSV was actually decoded from that archive. Confirm the decoding process independently with fixture tests. Also, source registration without an existing DB override is normal, and a valid spawn bounding rectangle is not evidence of correct collision or real startup registration.

## Current status

Source schemas and joins: audited. Export/reconciliation CLI: authored, pending execution with real approved inputs. **The first three P0 tasks are not completed as data extraction tasks in this environment:** the 607 MB cache LFS object and the populated MySQL database are unavailable through the active connection. No verified total definition counts, actual DB row counts, region spawn records, or runtime registration order can be claimed. Avoid generating illustrative records and labeling them fixture-backed. Continue with the explicit handoff in reference/items-npcs-remaining-work.md after those data sources are accessible.

## Source-only spawn mechanisms

The [nine known spawn-source categories](../data/catalogs/npc-spawn-sources.csv) are independently listed. The Python comparer audits *persistent database* rows only. Temporary wave/healer/familiar/encounter entities are runtime events and require separate event or session captures; the database count is not the total number of world NPC instances.
