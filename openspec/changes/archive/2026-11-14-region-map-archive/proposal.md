# Proposal

## Why

The region map files in cache index 5 (terrain `m{x}_{y}`, landscape `l{x}_{y}`, underwater `um`/`ul`) have no owner. Six classes each know a part of the layout and the write rules, and the copies have drifted:

- **Index and names.** `MapTerrainWriter`, `MapLandscapeWriter` and `MapWorldMapRefresher` each declare `MapsIndex = 5`. `MapRenderReader`, `MapProvider` and the `GetRegions` endpoint use a bare `5`. File names are built inline in 12 places, in two styles.
- **Corrupt data.** There are three different "corrupt data" rules:
  - `MapRenderReader.IsDataError`, which does not include `InvalidDataException`;
  - the writers, which add `InvalidDataException`;
  - `MapWorldMapRefresher.IsReadError`, which also adds `ArgumentException`.
- **Write safety.** `MapTerrainWriter` writes with compare-and-swap (`ICacheAPI.TryReplaceContainer`) and retries. `MapLandscapeWriter` uses a plain `Write`, so its "only overwrite a readable landscape" check can be invalidated by a write that lands between the check and the write. It also writes, and so raises the file's version, even when the placements are unchanged, which makes the world map refresh regenerate the region after every retried save.
- **Duplicated lookups.** `MapDefinitionsEndpoints.GetRegions` reads the index-5 reference table and hashes region names itself. `MapWorldMapRefresher.SourceVersions` re-derives per-region file versions from the table.
- **Results.** `MapArchiveUpdateResult` (an enum in `MapLandscapeWriter.cs`) reports outcomes. The type-mutation change deferred moving it onto FluentResults to this change.

**Goal:** one Cache library module, the region map archive, owns region map file names, lookup, reads, the corrupt-data rule and the write rules. Terrain and landscape saves follow the same write rules.

## What Changes

- **Add `RegionMapArchive`** (`Hagalaz.Cache/Maps/`), a sealed class built from `ICacheAPI` behind `IRegionMapArchive`, registered in `AddGameCache` and injected into its callers. It owns:
  - **Layout:** index 5, a `RegionMapFile` kind (`Terrain`, `Landscape`, `UnderwaterTerrain`, `UnderwaterLandscape`), the name format, and `Locate(region, kind)`.
  - **Region listing:** every region with a terrain or landscape file, and the rule that index 5 must carry named reference metadata.
  - **Reads:** decoded terrain (with its atmosphere and light trailer), landscape (optionally with XTEA keys), underwater terrain and underwater landscape. Results are FluentResults.
  - **One corrupt-data rule:** `IOException`, `InvalidDataException`, `NotSupportedException`, `OverflowException` and SharpZipLib errors.
  - **Writes:** applying terrain tile changes and replacing landscape placements, both read from the stored file, verified, written with compare-and-swap and retried, keeping the stored compression, and skipped when nothing changes.
  - **Region revisions:** the terrain and landscape file versions of many regions, read from one reference-table snapshot.
- **Results:** `RegionMapFileAbsentError` and `RegionMapFileUnreadableError` (FluentResults) replace `MapArchiveUpdateResult`. `EndpointErrors.RegionMapFileFailure` maps them to the existing 404 and 422 responses with today's titles and details.
- **Landscape saves:**
  - they now use compare-and-swap, so a landscape is only ever overwritten when it is still the readable landscape that was checked;
  - a save whose encoding equals the stored landscape's canonical encoding writes nothing.
- **Callers moved onto the archive:**
  - `MapRenderReader` (file reads and the metadata check; it keeps halo, floor and warning composition);
  - `MapWorldMapRefresher` (terrain and landscape reads and revisions; its object-definition lookup keeps its own documented filter);
  - `MapDefinitionsEndpoints` (terrain, landscape, region listing);
  - `MapProvider` (file lookup only; it keeps reading and combining raw containers).
- **Render behaviour change:** a landscape, terrain or underwater file that throws `InvalidDataException` while decoding is reported as failed with a warning, as other corrupt data already is, instead of failing the whole render request.
- **Removal:** delete `MapTerrainWriter`, `MapLandscapeWriter` and `MapArchiveUpdateResult`, after a temporary real-cache comparison of old and new behaviour.

**Reused, not added:** `ICacheAPI.TryReplaceContainer` and its write lock, `MapRenderDecoder`, `MapLandscapeEncoder`, `MapTerrainPatcher`, FluentResults 4.0.0 (already in `Hagalaz.Cache`), the existing problem responses. No new worker, lock, cache or retry owner.

### Non-goals

- Optimistic concurrency between editors (an expected revision or `If-Match` from the client). Compare-and-swap protects the readability check, not a whole-landscape save from replacing another editor's save.
- NPC spawn files (`n{x}_{y}`) in index 5. Nothing reads them.
- Providers for other definition types, member-ID reads on `ICacheAPI`, or changes to the map editor frontend.
- Changing `MapProvider`'s decoding, XTEA handling or exceptions, or game-server map loading.
- Changing the map codecs or the world map tile generation.
- Changing map endpoint routes, request DTOs or JSON responses, apart from the render case above.

### Acceptance criteria

1. Index 5 and the region map file names appear only in `RegionMapArchive` (tests excepted). `MapTerrainWriter`, `MapLandscapeWriter` and `MapArchiveUpdateResult` no longer exist.
2. One corrupt-data rule decides "unreadable" for every region map file read and save.
3. A landscape save never overwrites a landscape that changed after it was checked, and retries against the new landscape instead.
4. A landscape save whose placements equal the stored placements performs no cache write and still responds `map-landscape-updated`.
5. Terrain and landscape saves report a missing file as 404 and an undecodable file as 422, with today's titles and details, and write nothing.
6. The world map refresh reads region revisions only through the archive, and keeps its current retry behaviour.
7. On the local cache, a temporary comparison shows the old and new code agree for the region listing, render output of every region, revisions, and the bytes written for a sample of terrain and landscape saves.
8. Existing map endpoint, writer and refresher tests pass, apart from moving to the new module, and `dotnet build Hagalaz.sln` adds no warnings.

### Stop conditions

- **Stored files are not canonical.** If a stored landscape's canonical encoding differs from its stored bytes in a way that loses placements, stop: the skip rule would need a codec change. The survey found every `l` and `ul` file canonical.
- **Old and new code disagree.** If the real-cache comparison finds a difference not explained by the documented render change, stop and report it before deleting the old code.
- **The game server needs more than lookup.** If moving `MapProvider` onto `Locate` would change game-server behaviour, keep its lookup as it is and record a follow-up.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `cache-map-landscape-mutations`: a concurrent change between the readability check and the write is never overwritten, and a save equal to the stored landscape writes nothing.
- `web-3d-map-rendering`: corrupt region map data of any kind is reported per file instead of failing the region request.

## Impact

- **`Hagalaz.Cache`:**
  - new `Maps/RegionMapArchive.cs` with `RegionMapFile`, `RegionMapRevision`, `RegionTerrain` and the errors, and `Maps/IRegionMapArchive.cs`;
  - `AddGameCache` registers `IRegionMapArchive`;
  - `MapRenderReader`, `MapWorldMapRefresher` and `MapProvider` changed;
  - `MapTerrainWriter.cs` and `MapLandscapeWriter.cs` deleted.
- **`Hagalaz.Services.Cache`:** `MapDefinitionsEndpoints` and `EndpointErrors`.
- **Tests:**
  - `MapTerrainWriterTests` becomes `RegionMapArchiveTests`, with new landscape compare-and-swap, skip, listing and revision cases;
  - endpoint tests are kept.
- **Docs:** CONTEXT.md gains *Region map file*, *Region map archive* and *Region map revision*.
- **HTTP:**
  - unchanged, except that the render case above returns `failed` with a warning instead of an error;
  - unchanged landscape saves no longer raise the file's version.
