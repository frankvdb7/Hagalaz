# Design

## Context

See proposal.md for motivation. Each `m{x}_{y}` archive holds, for four planes of 64 by 64 tiles, a run of opcodes per tile, followed by atmosphere and light records:

- `0` ends the tile with an automatic height: terrain noise on plane 0, or 960 above the plane below on higher planes.
- `1` plus a byte ends the tile with an explicit height: that byte times 32 above the plane below. A byte of 1 is read as 0, so 32 cannot be stored.
- `2` to `49` plus a byte sets an overlay; the opcode carries shape and rotation.
- `50` to `81` sets flags. `82` and above sets an underlay of `opcode - 81`.

The render response gives corner heights as absolute values with up as negative, and includes neighbours' rows. `MapCodec.Encode` keeps only flags, and the decoded render values lose whether a height was automatic, so terrain cannot be regenerated from decoded data. `region.floors` only describes floors already in use.

## Goals / Non-Goals

Goals and non-goals are in proposal.md. This design adds no general terrain encoder and no tile flag editing.

## Decisions

**Patch tiles in the original bytes instead of re-encoding the terrain.** A new `MapTerrainPatcher` walks the stored opcodes, copies every unedited tile verbatim, rewrites only the height, underlay or overlay tokens of edited tiles, and copies everything after the last tile verbatim. This is the only way to keep automatic heights, flags, unusual token order and atmosphere exactly. Rejected: decoding and re-encoding all tiles, which turns automatic heights into explicit ones and needs a full atmosphere encoder.

**Tile changes are absolute stored values.** The endpoint is `POST /types/maps/{id}/terrain` with `{ tiles: [{ plane, x, y, height?, underlay?, overlay?, overlayShape?, overlayRotation? }] }`. An omitted field stays as stored and 0 clears a floor. Absolute values make retries idempotent, as with landscape saves. Rejected: sending height deltas, which would double-apply on retry.

**The client converts corner heights to the stored byte.** Heights are stored relative to the plane below, so editing plane z by a delta shifts that plane and every plane above it in the preview, matching how the client decodes them. A stroke clamps to 0 at the plane below, 255 steps above it, and steps of 32, and skips the value 32. An automatic tile becomes explicit only when a stroke edits it, and is rounded to a storable step then; the preview shows the rounded value.

**Corner ownership follows the format.** Corner (x, y) belongs to tile (x, y) of the region containing it. Row or column 64 belongs to the neighbour, so the stroke edits it only when that neighbour is loaded, and the preview updates both regions' shared corners.

**Terrain edits live in `MapSceneDraft`.** It gains per-region tile changes, written through the existing `commitEdit`, so undo, reset and region change need no second owner. A stroke commits once on pointer release, giving one undo step, while a hover outline marks the brush. Applying the draft produces derived region data for `buildMapScene`, so objects follow the ground through the existing contour path.

**Painted floors join the scene through the floor catalog.** `GET /types/maps/floors` returns every underlay and overlay using the existing `DecodeFloor`. The page merges definitions for painted IDs into the derived regions, and painted floor textures load through the same visibility path as `previewPlacements`.

**Brush input uses the existing surface pick.** `ModelScene.pickSurface` limited to terrain vertices gives the world position, converted to corner or tile by region origin. While a brush tool is active a left drag paints and does not orbit, using the existing pointer capture approach; other mouse buttons still move the camera.

**State owners.** The page owns the draft. The store owns the save sequence, generalised from `saveLandscapes` to an ordered list of terrain and landscape saves, each region's terrain first. `ICacheAPI.Write` owns persistence. The administrator owns retry.

**The save verifies before writing.** The patched bytes are decoded and compared for every edited value and every unedited tile, and the bytes after the last tile are compared, before calling `Write`.

## Risks / Trade-offs

- [Rebuilding terrain after each stroke may be slow on a 3 by 3 selection, since `MapScenePreparation` caches by region identity] → Rebuild only on stroke commit and measure on the largest selection; slower than 250 ms triggers the proposal's stop condition.
- [Height edits can make ground look walkable or blocked in ways collision does not match] → The confirmation states flags are unchanged; flag editing is a separate change.
- [Editing a border corner changes how an unloaded neighbour's edge looks] → This is how the format works. Only the owning region's tile is stored.
- [Plane edits move higher planes with them] → Matches the client's decoding and is shown in the preview.
- [Automatic heights lose precision when first edited] → Limited to edited tiles and visible in the preview.
- [Two administrators edit one region and the later save wins per tile] → Accepted, as with other cache saves.
- [Live cache data is overwritten with no undo after saving] → The confirmation warns of this; backups remain an operational concern.

## Migration Plan

No data migration. The cache service must be restarted to expose the endpoints. Rollback is removing the endpoints and tools; saved terrain stays valid because it uses the format the client already reads.
