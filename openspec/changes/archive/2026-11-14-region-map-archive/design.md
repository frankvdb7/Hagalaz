# Design

## Context

See proposal.md, section "Why". The current state that shapes the approach:

- **Write primitive.** `ICacheAPI.TryReplaceContainer(index, file, expected, replacement)` compares the file's decoded bytes with `expected` and writes under the cache write lock. `MapTerrainWriter` loops on it: read, patch, verify, try, and repeat on `false`.
- **Versions.** `CacheWriter.Write` always increments the entry's reference-table version, including for a byte-identical replacement. Writers build `new Container(compression, data)` with no container version, so the reference-table version is the only revision signal. `MapWorldMapRefresher` relies on it to rebuild when a region was saved while it was generating tiles.
- **Survey of the local cache** (index 5, read-only, on a copy):
  - 7,906 entries: 2,622 `m`, 2,198 `l`, 1,577 `um`, 1,476 `ul`, plus 33 NPC spawn files `n{x}_{y}`;
  - the table has identifiers, and no names collide within the four map kinds;
  - every map file is Gzip and decodes without keys, and no corrupt-data exception of any type occurs;
  - every `l`/`ul` file equals `MapLandscapeEncoder.Encode(DecodeLandscape(stored))`, and every `m` file equals `MapTerrainPatcher.Patch(stored, [])`;
  - 424 regions have terrain without landscape, and none have landscape without terrain.
- **Exceptions the map code can throw:**
  - `MapRenderDecoder` throws `InvalidDataException` (plane > 3), `NotSupportedException` (floor opcodes) and `EndOfStreamException`. It throws `ArgumentOutOfRangeException` only for a region ID outside 0–65535, which every endpoint rejects first;
  - container reads add `IOException`, `OverflowException` and SharpZipLib errors;
  - `MapWorldMapRefresher.IsReadError` also catches `ArgumentException`, and also uses it for `ITypeProvider<IObjectType>.Get`.

## Goals / Non-Goals

**Goals:**
- One owner for region map file layout, reads, the corrupt-data rule and the write rules. That owner is also the single retry owner for region map file writes.
- Callers see region-level operations, not index numbers, names, containers or the compare-and-swap loop.

**Non-Goals:**
- Changing `ICacheAPI`, the map codecs or the world map refresh's own retry loop over `main` (index 23).

## Decisions

### `RegionMapArchive` is a deep module in `Hagalaz.Cache/Maps/`, behind `IRegionMapArchive`

`public sealed class RegionMapArchive(ICacheAPI cache) : IRegionMapArchive`. It owns `internal const int Index = 5` and the only name formatter:

| `RegionMapFile` | Name |
|---|---|
| `Terrain` | `m{x}_{y}` |
| `Landscape` | `l{x}_{y}` |
| `UnderwaterTerrain` | `um{x}_{y}` |
| `UnderwaterLandscape` | `ul{x}_{y}` |

The region ID is `(x << 8) | y`.

Public surface:

| Member | Purpose |
|---|---|
| `int? Locate(int region, RegionMapFile kind)` | File ID, or `null` when absent. |
| `bool HasNamedFiles` | Whether index 5's reference table carries identifiers. |
| `Result<IReadOnlyList<RegionMapEntry>> ListRegions()` | Every region with a terrain or landscape file: `(RegionId, X, Y, TerrainFile?, LandscapeFile?)`, built from one table read. Fails with `RegionMapNamesMissingError` when the table has no identifiers. |
| `Result<RegionTerrain> ReadTerrain(int region)` | Planes plus the decoded trailer (`Atmosphere`, `PointLights`, `TrailerTruncated`). |
| `Result<MapRenderPlacement[]> ReadLandscape(int region, int[]? xteaKeys = null)` | Surface placements. |
| `Result<TerrainRenderPlane> ReadUnderwaterTerrain(int region)` | Underwater depth plane. |
| `Result<MapRenderPlacement[]> ReadUnderwaterLandscape(int region)` | Underwater placements. |
| `Result ApplyTerrain(int region, IReadOnlyCollection<MapTerrainTileChange> changes, CancellationToken)` | Terrain tile save. |
| `Result ReplaceLandscape(int region, IReadOnlyCollection<MapRenderPlacement> placements, CancellationToken)` | Landscape save. |
| `IReadOnlyDictionary<int, RegionMapRevision> Revisions(IReadOnlyCollection<int> regions)` | `RegionMapRevision(int Terrain, int Landscape)`, `-1` for an absent file, from one table read. |

The read methods also take a `CancellationToken`. Reads fail with `RegionMapFileAbsentError(region, kind)` or `RegionMapFileUnreadableError(region, kind, cause)`. Cancellation is never turned into an error.

The terrain trailer is decoded inside `ReadTerrain`, so the render reader keeps its "lighting data is truncated; using default lighting" warning without reading the file twice. A truncated trailer (`EndOfStreamException` in `DecodeTerrainTrailer`) sets `TrailerTruncated` and is not an error, as today.

**Alternatives rejected:**
- **Keep the writers and add a generic `Rewrite(region, kind, rebuild)`.** Every caller would still assemble the absent, unreadable, verify, compression and retry steps around it, which is the drift this change removes.
- **One generic `Read<T>(region, kind, Func<Stream, T>)`.** That would leak which decoder goes with which kind to every caller.

### Callers depend on `IRegionMapArchive`, registered in `AddGameCache`

At the maintainer's request, callers depend on the interface rather than the class, so that region map access is composed through DI like the other cache providers (for example `IMapProvider`). `ICacheAPI` is still the only adapter.

- **Interface location:** `IRegionMapArchive` lives next to the class in `Hagalaz.Cache/Maps`, not in `Hagalaz.Cache.Abstractions`, because its signatures use `Hagalaz.Cache` types (`MapRenderPlacement`, `TerrainRenderPlane`, `MapTerrainTileChange` and the trailer records).
- **Members:** only the public members above. `IsCorruptData` and `Index` stay `internal static` on the class.
- **Registration:** `AddGameCache` registers `IRegionMapArchive` → `RegionMapArchive` as transient, like `MapProvider`; the archive holds no state.
- **Callers:**
  - the map endpoints take it as a handler parameter;
  - `MapProvider` takes it as a constructor dependency;
  - `MapRenderReader` and `MapWorldMapRefresher` take it in their constructors, and `MapWorldMapRefresher.Create` passes it through.
- **Tests:** `RegionMapArchiveTests` tests the class against an in-memory index. Caller tests construct a real `RegionMapArchive` over their mocked or in-memory cache, so they keep pinning the stored layout instead of mocking the interface.

**Alternative rejected:** constructing the class where needed, with no interface. That was the original design; it was replaced at the maintainer's request.

### One write path and one retry owner

`ApplyTerrain` and `ReplaceLandscape` share one private loop:

1. Locate the file, or return `RegionMapFileAbsentError`.
2. Read the container and keep its bytes and compression.
3. Decode it. A corrupt-data exception returns `RegionMapFileUnreadableError`, without writing.
4. Build the replacement (see the per-kind steps below). A replacement equal to the unchanged form returns `Result.Ok()` without writing.
5. Verify the replacement. A failure throws `InvalidOperationException` without writing, because it means a codec defect, as in `TypeMutator`.
6. Check the cancellation token, then call `TryReplaceContainer` with the stored bytes. If it returns `false`, go back to step 2.

Per kind:

- **Terrain:** `MapTerrainPatcher.Patch(stored, changes)`. "Unchanged" means the patched bytes equal the stored bytes. Verification is today's `PatchIsExact`: bytes after the last tile are identical, and every tile equals its stored or requested value. This moves unchanged from `MapTerrainWriter`.
- **Landscape:** `MapLandscapeEncoder.Encode(placements)`. "Unchanged" means the bytes equal `Encode(DecodeLandscape(stored))`, the canonical encoding. Verification: decoding the encoding gives `Canonicalize(placements)`.

The archive is the only owner of the retry. There is no second lock, queue or retry anywhere.

**Alternative rejected:** keep a plain `Write` for landscape because a save replaces the whole file. That leaves the readability check and the write unsynchronized, and gives landscape a second write rule.

### Compare-and-swap does not provide editor-level optimistic concurrency

Two whole-landscape saves of the same region still end with the later one stored. The second one simply re-checks and writes. Preventing that would need a client precondition (an expected revision or `If-Match`), which is a frontend and API change and a non-goal. The landscape spec delta states the guarantee exactly.

### One corrupt-data rule, owned by the archive

`internal static bool IsCorruptData(Exception error)` covers `IOException` (including `EndOfStreamException` and `FileNotFoundException`), `InvalidDataException`, `NotSupportedException`, `OverflowException` and `SharpZipBaseException`. It replaces `MapRenderReader.IsDataError` for every region map file read and save.

`ArgumentException` is excluded. Map decoders throw it only for an invalid region ID, which is a caller bug, not stored data.

**Floor definitions (index 2).** `MapRenderReader` also reads floor definitions from index 2 and today uses the same rule. It now uses the archive's `IsCorruptData`, since it is the same "stored cache data cannot be decoded" decision. Floor reads stay in `MapRenderReader`, because they are not region map files.

**Object definitions.** `MapWorldMapRefresher` keeps a separately named, documented filter for object-definition lookups (`ITypeProvider<IObjectType>.Get`). A test pins which exception an unknown object ID really throws. The filter keeps `ArgumentException` only if that test shows it is needed.

### Results and HTTP mapping

`RegionMapFileAbsentError`, `RegionMapFileUnreadableError` and `RegionMapNamesMissingError` are FluentResults `Error`s next to the archive. Each carries the region and kind (`CausedBy` keeps the exception for the unreadable error).

`EndpointErrors.RegionMapFileFailure(ResultBase result, int id)` reproduces today's responses for each kind:

| Error | Response |
|---|---|
| Terrain absent | 404 "Region {id} has no terrain archive." |
| Landscape absent | 404 "Region {id} has no landscape archive." |
| Terrain unreadable | 422 "Terrain cannot be changed" / "Region {id}'s stored terrain could not be decoded." |
| Landscape unreadable | 422 "Landscape cannot be replaced" / "Region {id}'s stored landscape is encrypted or corrupt, so saving would lose its placements." |
| Anything else | Throws, like `TypeMutationFailure`. |

`GetRegions` maps `RegionMapNamesMissingError` to today's 422 "Region listing unavailable".

`MapRenderReader` maps read errors to its existing statuses (`absent`, `failed`) and warnings. A warning's text keeps the decoder's exception message, taken from the error's cause. When `HasNamedFiles` is false it still throws `NotSupportedException`.

### Callers

| Caller | Change (each receives `IRegionMapArchive`) |
|---|---|
| `MapRenderReader` | Uses the archive for its terrain, landscape, underwater and neighbour reads, and for the named-files check. Keeps halo composition, the underwater plane, floors and warnings. |
| `MapWorldMapRefresher` | Generates tiles from `ReadTerrain` and `ReadLandscape`: an absent landscape gives no placements, and an absent or unreadable terrain or an unreadable landscape skips the region. Uses `Revisions` for its source check. `SourceVersions` and the map `MapsIndex` are deleted. The refresher keeps its `main` (index 23) compare-and-swap loop. |
| `MapDefinitionsEndpoints` | Terrain and landscape saves, and `GetRegions` via `ListRegions`. Paging stays in the endpoint, and the JSON is unchanged. |
| `MapProvider` | Uses `Locate` for `m`/`l`. It keeps its own `ReadContainer` calls, XTEA handling, logging and exceptions. |

### Old/new comparison before deletion

A temporary `Hagalaz.Services.Cache.Tests` test, run on a copy of `Source/Cache`, compares the old and new code:

1. `GetRegions` output, old against new, for all pages;
2. `MapRenderReader.Read` JSON for every region;
3. world map tiles generated for every region on `main`, and revisions against `SourceVersions`;
4. on the cache copy, the bytes and versions written by old and new terrain and landscape saves, for a sample of regions (changed tiles, unchanged tiles, empty landscape, re-submitted landscape). Expected differences are documented: an unchanged landscape now writes nothing, so its version does not change.

The test is deleted before `MapTerrainWriter` and `MapLandscapeWriter` are removed.

## Risks / Trade-offs

- **World map generation exceptions are no longer swallowed.** `MapWorldMapRefresher.Generate` previously caught read errors, including `ArgumentException`, around `WorldMapTileGenerator.Generate` too. After the change only map-file and object-lookup errors are handled. → Comparison step 3 runs generation for every region on `main`. If generation throws on real data, stop and report it.
- **The render change** (an `InvalidDataException` becomes `failed` instead of an error response) could mask a decoder bug. → The warning keeps the exception message, and no region in the local cache hits the case.
- **A stored landscape that is not canonical** would be rewritten even when its placements are unchanged. → Correct but noisy. The survey found every stored file canonical.
- **Moving `MapProvider` onto the archive touches game-server map loading.** → Only the name and lookup code changes. `MapProviderTests`, `MapTerrainBlockingAgreementTests` and the GameWorld map loader tests keep their literal `GetFileId(5, "m50_50")` setups, which pin the stored layout.

## Migration Plan

Code-only change. Nothing is migrated in the cache. Rollback is a revert.
