# Design

## Context

See proposal.md for motivation. Current state that shapes the approach:

- `MapRenderDecoder.DecodeTerrain` already decodes flags, `MapRenderReader` already puts them on `MapRenderPlane.Flags`, and the web model `MapRenderPlane.flags` already receives them. Nothing in the web app reads them.
- `MapTerrainPatcher` copies every token it does not replace, and `MapTerrainWriter.PatchIsExact` refuses any write in which a flag changed.
- The web editor has one edit path: `map-terrain-edit.ts` turns strokes into `TerrainEdits`, previews them with `applyTerrainEdits`, and reduces them to changed tiles in `mapTerrainSaves`.
- The object appearance DTO carries `sizeX`, `sizeY`, shape and rotation but not the definition's `ClipType`, `Solid` or `Gateway`.

### Phase 0 findings

Sources: client `Class274.java` (terrain decode, line 2247; `readGroundMapData`, line 401), `TerrainData.java`, `Map.java` (lines 655, 684, 1075), `Class95_Sub13.java`, `Class212.java`, `WorldMapAreaPart.java`, `Class431.java`, `Class562.java`, `Class166_Sub1.java`. Server: `MapProvider.EnumerateBlockedTiles`, `MapCodec.DecodeObjectData`, `MapRegionLoader`.

**Encoding.** A tile's token stream holds at most one meaningful flag token. Opcode `n` in 50 to 81 assigns the whole flag byte `n - 49` (1 to 32). It is an assignment, not an OR, so a later flag token replaces an earlier one. The client resets the byte to 0 before reading each tile, so value 0 is stored as no flag token.

| Value | Stored as | Notes |
| --- | --- | --- |
| 0 | no token | |
| 1 to 31 | one opcode, 50 to 80 | any combination of bits 0x01 to 0x10 |
| 32 | opcode 81 | bit 0x20 alone; no client reader, never used in the cache |
| 33 to 255 | no encoding | bit 0x20 combined with anything, and bits 0x40, 0x80 |

No value needs more than one opcode. No tile in the local cache has more than one flag token.

**Bits.**

| Bit | Editor name | Client effect |
| --- | --- | --- |
| 0x01 | Blocked | `readGroundMapData`: marks the tile unmovable on collision plane `z`, or `z - 1` when the column's plane-1 tile has 0x02; dropped when that gives a plane below 0. |
| 0x02 | Bridge | Read from plane 1: collision of that column's terrain and objects moves one plane down (`Class274` 401, `Map` 655 and 1075), and height sampling for the player, camera and effects uses the plane above (`Class431`, `Class562`, `Class166_Sub1`, `CpuData`). Read from plane 0 by the minimap as "always drawn". Read from plane 3 by the camera's roof test. No reader on plane 2. |
| 0x04 | Roof | `Class95_Sub13`, `Class212`: when the player or the camera's line of sight is on such a tile, connected roof tiles have the planes above them hidden. |
| 0x08 | Draw on plane 0 | `TerrainData.getHeight` returns plane 0, so the minimap draws the tile with the ground plane; `Class95_Sub13` also keeps such upper-plane tiles visible when roofs are hidden. The cache-side `WorldMapTileGenerator` uses 0x08 and 0x02 to pick upper world map layers. |
| 0x10 | Hidden on minimap | `TerrainData.method2786` returns false, so `WorldMapAreaPart` skips the tile, unless its plane-0 tile has 0x02. |
| 0x20 | (none) | No reader found. |

**Server.** `MapProvider.EnumerateBlockedTiles` applies the same rule as the client's `readGroundMapData`: bit 0x01 on plane `z` yields `CollisionFlag.FloorBlock` on plane `z`, or `z - 1` when plane 1 of the column has 0x02, and nothing when that is below 0. `MapRegionLoader` writes those as `FloorBlock`. `MapCodec.DecodeObjectData` lowers object planes by the same plane-1 test. The server ignores bits 0x04, 0x08 and 0x10. Server and client agree on terrain blocking; no server change is needed.

**Differences from the brief.**

- Phase 1's DTO and model work is already done; only the helper is missing.
- A flag value is one opcode carrying the whole byte, not one opcode per bit. Nothing "needs more than one opcode".
- The brief's "force-lowest-plane" is bit 0x08 and affects the minimap and roof hiding, not collision. Bit 0x10 hides a tile from the minimap.
- Bridge handling depends on the plane: only plane 1's bit shifts collision. The cache has the bit on plane 0 (about 163,000 tiles) and plane 3 (6,648 tiles) where it has other, non-collision meanings.
- The existing spec forbids any flag change in a terrain save and requires a confirmation note saying so; both must change.

**Cache usage** (`Source/Cache`, 2,622 terrain archives, 42,958,848 tiles; values not listed have zero use, including 15, 18, 19, 22, 23, 26 to 28 and 30 to 32):

| Value | Tiles | Example region (tiles there) |
| --- | --- | --- |
| 1 | 11,122,447 | 7010 (16,384) |
| 2 | 176,970 | 8770 (4,569) |
| 3 | 74,006 | 8002 (4,096) |
| 4 | 444,967 | 12610 (9,461) |
| 5 | 297,704 | 18773 (12,812) |
| 6 | 5,469 | 13722 (1,707) |
| 7 | 892 | 15967 (215) |
| 8 | 408,570 | 9029 (6,313) |
| 9 | 295,805 | 11154 (4,096) |
| 10 | 17,783 | 19777 (332) |
| 11 | 721 | 11151 (257) |
| 12 | 372 | 12598 (268) |
| 13 | 83 | 15148 (29) |
| 14 | 4 | 12850 (4) |
| 16 | 22,068 | 12597 (1,300) |
| 17 | 26,532 | 9046 (3,594) |
| 20 | 250 | 12338 (108) |
| 21 | 10,895 | 9046 (10,723) |
| 24 | 8 | 13105 (8) |
| 25 | 253 | 6989 (244) |
| 29 | 24 | 12850 (24) |

Region 12850, already pinned in `TestSupport/Fixtures/maps`, uses values 1, 2, 4, 5, 6, 8, 9, 13, 14, 16, 17, 21 and 29, so it exercises all five bits.

## Goals / Non-Goals

**Goals:**

- One definition of what each bit means on the web side, and one rule for "blocked on collision plane p" that matches `MapProvider`.
- Flag edits travel the existing edit, preview, undo and save path.
- The overlay is separate from terrain geometry so toggling it costs nothing in the terrain build.

**Non-Goals:**

- Re-encoding terrain, reordering tokens of untouched tiles, or normalising archives.
- Showing wall-decoration or dynamic (spawned, script-changed) collision.
- Reproducing the client's roof hiding or minimap in the viewport; the overlay only marks the tiles those features read.

## Decisions

### Flag meanings live in one pure module

New `map-tile-flags.ts` in the admin cache pages exports the bit constants, `tileFlagFacts(value)` returning `{ blocked, bridge, roof, drawOnGroundPlane, hiddenOnMinimap }`, `collisionPlane(planes, plane, tile)` returning the plane the tile's blocking lands on or `null`, and `isStorableFlags(value)`. The overlay, the brushes and the tests all import it. Alternative rejected: constants in `map-terrain-edit.ts`; the overlay geometry would then depend on the edit module.

### A tile change carries the whole flag value

`MapTerrainTileChange` gains `int? Flags` (web: `flags?: number`), the resulting value 0 to 32, not a bit delta. The brush computes it from the tile's current previewed value. Alternative rejected: set and clear masks on the wire; the patcher would need to decode before encoding and two requests could not be compared for equality. The whole value keeps "apply twice gives the same result".

### The patcher writes a flag token only when the value differs

When `Flags` is present, `MapTerrainPatcher.WriteTile` first reads the tile's stored value (the last flag token). If it equals the submitted value the tile's flag tokens are copied as stored. Otherwise the new token `49 + value` takes the place of the tile's first flag token, any later flag token is dropped, and a tile that had none gets the token before its height token. Clearing (value 0) writes no token. Replacing in place keeps the diff to one byte for a tile that already had a flag. This keeps a no-effect save byte-identical even when a caller submits a tile's current value.

`MapTerrainPatcher` owns representability: it exposes `IsStorableFlags(int)` (0 to 32) and `Patch` throws before producing output if any change fails it. The endpoint's `IsValidTile` calls the same predicate and returns the existing validation error, so nothing is read or written for an invalid request. `MapTerrainWriter.TileIsExact` compares flags with `change?.Flags ?? old.Flags`.

Value 32 is accepted because the format stores it, although the editor's brushes never produce it.

### Flag brushes are two tools plus a chosen bit

`TerrainBrushTool` gains `"set-flag"` and `"clear-flag"`, and `TerrainBrush` gains `flag` (one of the five bit constants). `brushPatch` returns `{ flags: current | bit }` or `{ flags: current & ~bit }`, or no patch when the result is not storable (a tile already at 32). `TerrainTileEdit` gains `flags?`. `copyPlane` copies `flags`, `applyFloors` applies it (renamed to say it applies tile values), and `mapTerrainSaves` adds a flag change only when it differs from the stored region. Flag tools use tile snapping like floor tools. The brush panel shows the two tools and a "Flag" select when either is active. Alternative rejected: ten tools, one per bit and direction; the toolbar would not fit the panel.

`terrainStrokePreview` draws a set-flag stroke as untextured tiles in the overlay colour of the chosen bit and a clear-flag stroke as a pale wash, so the drag is visible whether or not the overlay is on. A clear stroke cannot use the flag's colour: over the overlay's own markers it would not show.

`applyTerrainEdits` now copies only the arrays an edit touches, and the terrain build compares the ground's actual inputs (heights, floors, floor definitions) instead of the region list's identity. A flag stroke therefore leaves terrain and object geometry as they were. `terrainEditNotes` stops claiming flags are unchanged and instead returns, when any edit has `flags`, "Tile flag changes reach a running game world only after it reloads the region."

### The overlay is its own geometry and its own mesh

A new pure module `map-walkability.ts` exports `buildWalkabilityOverlay(regions, visiblePlanes, objects)` returning positions and one colour per marker, built from the previewed regions (so it follows unsaved edits) and each plane's corner heights. It sits beside `map-object-shadows.ts` and `map-point-lights.ts` rather than inside `map-scene-geometry.ts`, which is already about 950 lines and whose build the overlay must stay out of. `ModelScene` gains `setTileOverlay(geometry | null)`, which owns one mesh and material, draws it slightly above the ground with polygon offset, and disposes both on replacement and in `dispose()`. The page computes the overlay only while the option is on. The overlay is not an input to `buildMapScene`, so toggling it cannot rebuild terrain; a test asserts the terrain geometry object is the same before and after.

The view option is a checkbox item in the layers menu directly after "Object shadows", off by default, labelled "Walkability (flags, drawn objects)".

Colours are fixed, saturated and semi-transparent so they read against any floor: blocked red, bridge blue, roof purple, draw-on-plane-0 teal, hidden-on-minimap grey, object collision orange. A tile with several bits is filled with the first in that order and carries a centred inset in the second. A short legend of these colours is shown in the brush panel while a flag tool is active. Alternative rejected: tinting terrain vertex colours; that would put the overlay inside the terrain build.

### Plane and bridge behaviour

A tile's marker is drawn on the plane where the flag is stored, and only while that plane is visible in the layers menu. Blocking uses `collisionPlane`: a blocked tile whose column is a plane-1 bridge blocks the plane below; a plane-0 blocked tile under a plane-1 bridge has no effect in game and is drawn in a dimmed blocked colour. Bit 0x02 is marked wherever it is stored, because it is a flag an administrator can set on any plane; only plane 1's bit takes part in the collision rule, and the brush panel says so.

### Object collision

The appearance DTO gains `clipType`, `solid` and `gateway`, read from the placed definition as the server's `IGameObject.Definition` is. The overlay then applies the server's rules from `CollisionMethods`:

- Standard objects (shapes 9 to 21) with `clipType != 0` or `gateway`: every tile of the footprint, with `sizeX` and `sizeY` swapped for rotations 1 and 3, marked "object".
- Ground decorations (shape 22) with `clipType == 1`: the tile, marked "object".
- Walls (shapes 0 to 3) with `clipType != 0` or `gateway`: an edge strip on the blocked side or corner, following `FlagWallObject`'s shape and rotation table.
- Object planes drop by one on plane-1 bridge columns, as in `MapCodec.DecodeObjectData`.

Objects whose appearance has not loaded yet contribute nothing, and the overlay rebuilds as appearances arrive. Object markers follow unsaved object edits and are left out while the "Landscape objects" layer is off.

Limitations, which the label "drawn objects" states:

- An object with no model for its placed shape has no appearance (the endpoint answers not found), so an invisible blocker is not marked.
- The footprint uses the appearance's sizes, which come from the transform variant when the definition has one; the server uses the placed definition's sizes.
- Only movement blocking is shown. The server's projectile flags (`Solid`, the `Blocked*` and `AllowRange` values) and collision added at run time by scripts are not.

### Server agreement is pinned, not live

A cross-language check uses two small checked-in fixtures derived from `terrain-12850.dat`: the decoded flags, and the blocked tiles per collision plane. A `Hagalaz.Cache.Tests` test asserts that `MapRenderDecoder` reproduces the flags fixture and that `MapProvider.DecodeRegion` over the same archive reproduces the blocked fixture. A web test feeds the flags fixture to `collisionPlane` and asserts the same blocked fixture. Either side drifting fails its own test. Alternative rejected: a harness that runs both runtimes; it would need a live cache and cross-process tooling in the normal test run.

## Ownership

| Responsibility | Owner |
| --- | --- |
| Flag encoding, representability | `MapTerrainPatcher` |
| Verifying a patch changed only what was asked | `MapTerrainWriter` |
| Request validation and HTTP errors | `MapDefinitionsEndpoints` |
| Bit meanings and the collision-plane rule (web) | `map-tile-flags.ts` |
| Edits, strokes, undo steps, saves | `map-terrain-edit.ts` and the page's existing edit signals |
| Terrain and object geometry, and what invalidates them | `map-scene-geometry.ts` |
| Overlay geometry | `map-walkability.ts` |
| Overlay mesh, material, disposal | `ModelScene` |
| Retry | none added; a failed save keeps local edits as today |

## Risks / Trade-offs

- [A wrong flag makes ground unwalkable or walkable through walls in game] → The overlay shows the result before saving, the confirmation still gates the write, and live verification runs against a copy of the cache.
- [Bit names are inferred from decompiled code] → Each name in the table cites the reading code; 0x20 is left unnamed and not offered as a brush.
- [Changing 0x02 or 0x08 changes the world map] → The existing post-save world map refresh already covers edited regions.
- [The overlay adds up to 16,384 quads per region] → It is built only while enabled and only for visible planes.
- [Object collision widens the appearance DTO] → Three additive fields; the stop condition in proposal.md bounds the work.
- [`fallow:audit` reports the page template's existing complexity as introduced] → Any edit to `maps-detail.page.html` brings its existing finding (27 cyclomatic, 50 cognitive) into the audit's scope; this change adds one menu item to it. Splitting the template is separate work.
- [A running game world keeps old collision] → Stated in the save confirmation; pushing changes is a non-goal.

## Migration Plan

Additive. Requests without `flags` behave as before. Rollback is reverting the change; archives written with new flag tokens remain valid for the client, the server and the previous editor.
