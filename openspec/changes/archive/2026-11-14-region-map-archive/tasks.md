# Tasks

## 1. Region map archive in Hagalaz.Cache

- [x] 1.1 Add `Hagalaz.Cache/Maps/RegionMapArchive.cs` with `RegionMapFile`, the name formatter, `Locate`, `HasNamedFiles`, `IsCorruptData` and the errors `RegionMapFileAbsentError`, `RegionMapFileUnreadableError` and `RegionMapNamesMissingError`. Verify with `RegionMapArchiveTests` (xUnit, mocked `ICacheAPI`): each kind looks up its literal name (`m50_50`, `l50_50`, `um50_50`, `ul50_50`), and an absent file gives `null`.
- [x] 1.2 Add `ListRegions` (`RegionMapEntry`, one table read, fails without identifiers) and `Revisions` (`RegionMapRevision`, `-1` for absent, one table read). Verify with tests: a region with only terrain is listed with a null landscape file; a table without identifiers gives `RegionMapNamesMissingError`; revisions match the entries' versions and read the table once for several regions.
- [x] 1.3 Add `ReadTerrain` (with `RegionTerrain` and the truncated-trailer flag), `ReadLandscape` (optional XTEA keys), `ReadUnderwaterTerrain` and `ReadUnderwaterLandscape`. Verify with tests: absent and unreadable files give the matching error with region and kind; an `InvalidDataException` from decoding is unreadable; a truncated trailer still returns the terrain with `TrailerTruncated`; cancellation propagates.
- [x] 1.4 Add the shared write loop and `ApplyTerrain`, moving the patch and verification logic from `MapTerrainWriter`. Move both `MapTerrainWriterTests` cases into `RegionMapArchiveTests` (retry after a concurrent write, unreadable writes nothing) and verify they pass. Also verify that an unchanged patch writes nothing.
- [x] 1.5 Add `ReplaceLandscape` on the same loop, with the canonical-encoding skip. Verify with tests:
  - a concurrent landscape write between the read and `TryReplaceContainer` causes a re-check and a retry, and the save is stored;
  - a landscape replaced by undecodable data before the write gives `RegionMapFileUnreadableError` and writes nothing;
  - re-submitting the stored placements calls neither `TryReplaceContainer` nor `Write`;
  - the stored compression is kept.

## 2. Callers use the archive

- [x] 2.1 Add `EndpointErrors.RegionMapFileFailure` and move the terrain and landscape endpoints and `GetRegions` onto the archive. Verify that the existing `MapRenderEndpointTests` terrain, landscape and region-listing cases pass unchanged, and add an endpoint test that an unchanged landscape save responds `map-landscape-updated` without writing.
- [x] 2.2 Move `MapRenderReader` onto the archive's reads and `IsCorruptData`, keeping statuses and warning texts. Verify that the existing render endpoint tests pass, and add a test that a landscape decoding to plane > 3 returns `landscapeStatus: "failed"` with a warning instead of an error response.
- [x] 2.3 Move `MapWorldMapRefresher` onto `ReadTerrain`, `ReadLandscape` and `Revisions`, delete `SourceVersions` and its map index, and rename and document its object-definition filter. Add a test that pins which exception `ITypeProvider<IObjectType>.Get` throws for an unknown object ID, and keep `ArgumentException` in that filter only if the test needs it. (Result: the existing `TypeProviderSnapshotTests` fallback cases already pin that a missing object archive or member decodes as a default object without throwing, and placement IDs are never negative, so the object lookup uses the archive's corrupt-data rule and no `ArgumentException`.) Verify that `MapWorldMapRefresherTests` and the refresh endpoint tests pass.
- [x] 2.4 Move `MapProvider` onto `Locate`. Verify that `MapProviderTests`, `MapTerrainBlockingAgreementTests` and the `Hagalaz.Services.GameWorld.Tests` map loader tests pass unchanged.

## 3. Old/new comparison and removal

- [x] 3.1 Add a temporary real-cache comparison test, working on a copy of `Source/Cache`, for the four comparisons in design.md (listing, render JSON for every region, world map tiles and revisions, and bytes and versions written by sample saves). Verify that it reports no differences other than the documented unchanged-landscape skip. If any other difference appears, stop and report it.
- [x] 3.2 Delete the comparison test, then remove `MapTerrainWriter`, `MapLandscapeWriter` and `MapArchiveUpdateResult` with Rider Safe Delete, and `git rm` any emptied files. Verify that a text search finds no `MapsIndex`, bare map index `5`, or inline `m`/`l`/`um`/`ul` region name outside `RegionMapArchive` and the tests.

## 4. Interface and registration

- [x] 4.1 Add `IRegionMapArchive` next to the class, register it in `AddGameCache`, and inject it into the map endpoints, `MapProvider`, `MapRenderReader` and `MapWorldMapRefresher`; caller tests construct a real `RegionMapArchive` over their cache. Verify with `AddGameCache_RegistersTheRegionMapArchiveForMapCallers` (resolves `IRegionMapArchive` as `RegionMapArchive`, and `IMapProvider`), and that the suites in 5.1 pass.

## 5. Verification

- [x] 5.1 Run `Hagalaz.Cache.Tests` and `Hagalaz.Services.Cache.Tests` through Rider, run `dotnet build Hagalaz.sln`, check `get_file_problems` for the changed files, and review the diff for leftover helpers and new warnings. Confirm that acceptance criteria 1–8 in proposal.md are met.
