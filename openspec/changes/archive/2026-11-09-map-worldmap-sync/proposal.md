# Proposal

## Why

Saving map edits changes the game world but not the full-screen world map, so a new building or reshaped coast stays invisible there. The world map stores per-tile data derived from the same terrain and object files, so it can be regenerated.

## What Changes

Goal: after a map edit is saved, the edited regions' entries in the `main` world map are regenerated from the stored map files.

In scope:

- A generator that builds one region's world map tiles from its stored terrain and objects.
- A one-off, read-only verification that regenerates every region of `main` and compares the result with the stored data, proving the inclusion rules before any write path exists.
- A cache service endpoint that replaces the given regions' blocks in the `main` area file and leaves every other block byte for byte.
- A refresh at the end of **Save changes**, once for all saved regions, and a **Rebuild world map** item in the more actions menu for retrying it.

Non-goals:

- World map areas other than `main`. Underground and instanced areas map coordinates and planes through area parts, and some use partial blocks.
- Adding a region that `main` does not already contain, or changing area bounds.
- World map labels, static elements and the `details` file.
- Rendering or image generation, because the client draws the world map itself.
- Pushing changes to running clients beyond the cache's normal update path.

Acceptance criteria:

- Across all 754 regions of `main`, the generator meets the verification thresholds in the design, with the mismatches recorded.
- After a refresh, the region decodes to the generated tiles and every other block is byte-identical.
- A refresh that produces no difference writes nothing.
- Regions that `main` does not contain, or whose map files cannot be read, are reported and cause no write.
- A failed refresh never loses or misreports saved map edits, and can be retried from the menu.

Stop conditions: stop before building the write path if verification misses its thresholds, and report what was learned. Raise a follow-up change if keeping other areas in sync is required.

Reused mechanisms: `ICacheAPI.Write`, `MapRenderDecoder`, `ITypeProvider<IObjectType>`, the byte-preserving patch approach from `map-terrain-editing`, `CacheMapSceneStore` saving, and the admin cache authorization policy. No new worker, queue, lock or dependency.

This builds on `map-landscape-save` and `map-terrain-editing`.

## Capabilities

### New Capabilities

- `cache-world-map-sync`: how a region's world map entry is regenerated from its stored map files and refreshed after map edits.

### Modified Capabilities

None.

## Impact

- `Hagalaz.Cache`: world map codec, tile generator and object definition field access.
- `Hagalaz.Services.Cache`: new `POST /types/maps/worldmap/refresh` endpoint under the existing authorization.
- `Hagalaz.Web.App/src-admin`: maps service, map scene store, save flow and viewport menu.
- Each refresh rewrites the `main` file, 6 MB uncompressed and about 0.8 MB stored, so clients download it again after an edit.
