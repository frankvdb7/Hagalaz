# Design

## Context

See proposal.md for motivation. Index 23 holds a `details` file of 45 areas, and for each area a group with one `area` member. The client draws the world map from that data with the ordinary floor and object definitions; no images are stored.

An `area` member has two byte palettes (underlays, overlays), then blocks. Block kind 0 is `regionX, regionY` and 4,096 tiles in x-then-y order. Other kinds are 8 by 8 chunks. `main` has 754 full blocks in 6.25 MB and no chunks. A tile is one of:

- Simple, bit 0 clear: bit 1 selects underlay or overlay, and bits 2 to 7 give a palette index, 62 for empty or 63 for a literal byte. An overlay tile is followed by its underlay byte.
- Complex, bit 0 set: bits 1 and 2 give layers minus one, bit 3 says layers carry overlay and shape, bit 4 says they carry an object count. Each layer is underlay, optional overlay and info, optional count, then that many big-smart object IDs each with a data byte.

Info and object data bytes are `rotation << 6 | shape`.

Evidence from region 12850 against its map files:

- Base layer equals plane 0 on 4,096 of 4,096 tiles, including tiles under bridges.
- All 999 world map objects trace to stored placements.
- Plane 0 walls of shapes 0, 2, 3 and 9 with a non-zero clip type: 839 of 839 included. Shapes 1, 4 to 8, 12 to 21 and non-blocking walls: none included.
- Shape 10 and 22 objects are partly included (trees, stairs, ladders, plants). These likely carry a map icon or map scene. The second layer appears on 173 tiles, tied to plane 1 flags 8 and 2 and to content on that tile.

## Goals / Non-Goals

Goals and non-goals are in proposal.md. This design does not add new world map blocks or change area bounds.

## Decisions

**Prove the rules before building a write path.** A file-based C# script outside the repository runs the generator over a read-only copy of the cache for all of `main`, compares decoded tiles with stored tiles, and records the counts in this file. Thresholds: base floors at least 99.9% of tiles, object inclusion at least 98% of tiles, second-layer presence at least 95% of tiles. Decoded comparison is used because encoding choices such as palette or literal do not change the meaning. Rejected: a verification endpoint or a skipped test, which would be production or test surface kept only for a one-off check.

**Patch the block, not the file.** A new `WorldMapAreaCodec` splits an area member into its palettes and raw block spans, and re-encodes only requested blocks with the existing palettes, using literals for floors outside them. Rejected: re-encoding every block, which risks changing regions nobody edited.

**`MapWorldMapRefresher` regenerates from stored files, not from the client's draft.** The endpoint is `POST /types/maps/worldmap/refresh` with `{ regions: [ids] }` and returns refreshed and skipped regions. Reading the stored map files keeps one source of truth and makes retries idempotent. Rejected: having the client send tiles, which would be a second generator and could disagree with what was stored.

**One request for all regions.** The `main` file is written once per refresh rather than once per region, so a multi-region save costs one rewrite of about 0.8 MB stored.

**Object inclusion lives in one rule** in the generator, using shape, clip type and the definition's map icon and map scene values. The existing definition type exposes `MapIcon`; the map scene value is read the same way without changing how definitions are encoded.

**`main` only.** All four of its parts use plane 0, but one shifts 20 regions: real 1728,3072 is displayed at 2048,2752. Blocks are stored at display coordinates, so the refresher maps each region through its part. Other areas may also change planes and use partial blocks, so they are left alone and the confirmation says so.

**State owners.** `ICacheAPI.Write` owns persistence. The refresher owns deriving world map data. The store owns the request and its result, separately from `saveRegions`. The administrator owns retry through the menu item.

**The refresh runs after the map saves, and its failure stays separate.** Map edits are the primary result, so a refresh failure shows its own message after the scene reloads. Rejected: failing the whole save, which would report stored edits as lost.

**Verify before writing.** The patched member is decoded and each unrequested block compared byte for byte before calling `Write`.

## Verification results

Run on a copy of the cache, all 754 regions of `main`, none skipped, 641 identical in every respect:

| Measure | Result | Threshold |
| --- | --- | --- |
| Base floors | 99.985% of 3,088,384 tiles | 99.9% |
| Object lists per tile | 99.812% | 98% |
| Second-layer presence | 99.632% | 95% |
| Upper floors where both have a layer | 99.799% of 28,924 | none |
| Encode and decode round trip | 100% | none |

Final rule: walls of shapes 0, 2, 3 and 9 are included at any clip type, and shapes 1, 10, 11 and 22 are included when they have a map icon or map sprite. Upper layers appear on planes flagged 8 or 2 with content.

Remaining differences: 2,082 stored objects are not in the map files, which indicates world map data older than the maps. About 11,000 tiles have a stored upper layer without those flags, and about 500 the reverse; the rule is not exact there. Refreshing a region replaces such differences with generated data.

A full save and refresh also ran on a cache copy: object and terrain writes for region 12850, then a refresh of 12850, shifted region 6960 and off-map region 0. Exactly one of 754 blocks changed, 6960 was reported unchanged, region 0 was skipped, and a second refresh wrote nothing.

## Risks / Trade-offs

- [Jagex's stored world map may already differ from the map files] → Verification records this. Refreshing an edited region replaces its older data, which is the intent.
- [Inclusion rules fit most but not all tiles] → Thresholds gate the work, and remaining differences are recorded.
- [A rewrite of about 0.8 MB per save makes clients download the file again] → Accepted; the rebuild runs once per save.
- [Regions in underground areas stay stale] → Disclosed at confirmation; a follow-up change can cover them.
- [Live cache data is overwritten] → Same confirmation as map saves.

## Migration Plan

No data migration. The cache service must be restarted. Rollback is removing the endpoint and menu item; refreshed blocks stay valid because they use the format the client already reads.
