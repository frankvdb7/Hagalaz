# Tasks

## 1. Flag meanings and server agreement

- [x] 1.1 Verify each flag bit, the opcode window and the server's terrain blocking against the client source and record them in design.md; verified when "Phase 0 findings" cites the reading code for every bit
- [x] 1.2 Count flag values across `Source/Cache` and name a region per value in use; verified by the usage table in design.md
- [x] 1.3 Add `map-tile-flags.ts` with the bit constants, `tileFlagFacts`, `collisionPlane`, `blockedPlane` and `isStorableFlags`; verified by a pure spec covering values 0, each single bit, 5, 29, 32, 33, and the bridge cases on planes 0 and 1
- [x] 1.4 Check in the flags and blocked-tile fixtures for region 12850 and add a `Hagalaz.Cache.Tests` test that `MapRenderDecoder` reproduces the flags fixture and `MapProvider.DecodeRegion` reproduces the blocked fixture; verified by that test passing
- [x] 1.5 Add a web spec that feeds the flags fixture to `blockedPlane` and matches the blocked fixture; verified by `pnpm run admin:test`. The two sides agree

## 2. Storing a flag value

- [x] 2.1 Add `Flags` to `MapTerrainTileChange` and make `MapTerrainPatcher` write the flag token for tiles whose value differs, with `IsStorableFlags` guarding `Patch`; verified by patcher tests: an edited flag round trips, a replaced and a cleared flag decode correctly, an unchanged tile is byte-identical, an empty save and a save of current values reproduce the archive, trailing data survives, and values -1, 33 and 255 throw with no output
- [x] 2.2 Make `MapTerrainWriter.TileIsExact` expect the submitted flag value; verified by an endpoint test that a flag save is stored and that a later height-only save leaves flags unchanged
- [x] 2.3 Reject unstorable flag values in the terrain endpoint's validation with an error naming the range 0 to 32; verified by a `Hagalaz.Services.Cache.Tests` test that the response is a validation error and the cache is not written
- [x] 2.4 Add a patcher test on `terrain-12850.dat` that changes one flagged tile and leaves every other tile's values, the archive length and the trailer identical, with one byte differing; verified by that test passing

## 3. Flag brushes

- [x] 3.1 Add `flags` to `TerrainTileEdit` and `MapTerrainTileChange` (web), the `set-flag` and `clear-flag` tools and the brush's `flag`, and apply, preview and save them in `map-terrain-edit.ts`; verified by specs that a set keeps other bits, a clear keeps other bits, an unstorable result leaves the tile alone, a tile returned to its stored flags is not submitted, and the stroke preview marks the brushed tiles
- [x] 3.2 Replace the "flags are not changed" confirmation note with the running-game-world note for flag edits; verified by a `terrainEditNotes` spec and the page spec for the confirmation text
- [x] 3.3 Add the two tools, the flag select and the colour legend to the brush panel; verified by component specs that choosing a tool and a flag updates the brush and that the select and legend appear only for flag tools

## 4. Walkability overlay

- [x] 4.1 Add `buildWalkabilityOverlay` in `map-walkability.ts` for terrain flags, honouring plane visibility and the collision-plane rule; verified by geometry specs that flagged tiles appear with the right colour and height, hidden planes are omitted, a bridge column dims dropped blocking, and edited flags change the output
- [x] 4.2 Add `ModelScene.setTileOverlay` owning the overlay mesh and its disposal; verified by scene specs that the mesh is added, refilled and removed, that its geometry and material are disposed on removal and on `dispose()`, and that nothing else is added to the scene
- [x] 4.3 Add the walkability view option, off by default, and wire the overlay from the previewed regions; verified by viewport and page specs that the option toggles the overlay, that toggling and a flag stroke keep the same terrain geometry, and that a flag stroke and an undo update it
- [x] 4.4 Add `clipType`, `solid` and `gateway` to the object appearance DTO and its web model; verified by a `Hagalaz.Services.Cache.Tests` test of the returned values
- [x] 4.5 Add object collision to the overlay with the server's footprint, rotation, wall and bridge rules; verified by geometry specs for a rotated 2 by 1 object, a non-blocking object, a ground decoration, each wall shape, and an object on a bridge column. The remaining limitations are in design.md and in the option's label

## 5. Integration checks

- [x] 5.1 From `Hagalaz.Web.App` run `pnpm run admin:test`, `pnpm test`, `npx vitest run shared/model-viewer`, `pnpm run lint:biome`, `pnpm run admin:build` and `pnpm run fallow:audit`; all pass except `fallow:audit`, which exits 1 on findings in existing functions of the touched files and attributes one to this change: the page template's existing complexity (see design.md, Risks)
- [x] 5.2 Collect coverage for the changed web files and report uncovered lines and branches; verified by the measured figures in the delivery report
- [x] 5.3 Run `Hagalaz.Cache.Tests` and `Hagalaz.Services.Cache.Tests` with an isolated `--artifacts-path`; verified by both passing with no new warnings
- [x] 5.4 Run `openspec validate map-tile-flags --strict`; verified by it passing
- [x] 5.5 With the cache service restarted on this change and a backup of the cache in place, open region 12850 at `http://localhost:4410`, toggle the overlay, paint a flag, save, reload and confirm it persists, with keyboard operation of the new tools and a clean console; verified live on 2026-11-11: 18 plane-0 tiles set to blocked, stored and present after a full reload, then cleared and saved, after which region 12850's terrain and landscape archives and two neighbours matched the backup. The brush panel was checked at phone width; painting by touch was not
- [ ] 5.6 Compare the overlay side by side with the revision-742 client in a flagged region (blocked tiles, a bridge and a roofed building); verified by a recorded comparison. Leave this task open when the change is delivered

## Workflow follow-up

- Leave the change unarchived until the live check and the client side-by-side task are closed.
