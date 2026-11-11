# Tasks

## 1. Server agreement fixtures

- [x] 1.1 Capture `object-collision-12850.json` (size, clip type, solid, gateway per distinct placed ID in `landscape-12850.dat`) and record its provenance in `TestSupport/Fixtures/maps/README.md`; verified by the file covering every placed ID
- [x] 1.2 Add a `Hagalaz.Services.GameWorld.Tests` test that loads region 12850 through `MapRegionLoader` from the pinned terrain, landscape and object-collision fixtures and asserts its collision equals `collision-12850.json`; verified by the test passing and asserting non-zero counts of floor, floor decoration, object, wall and solid bits

## 2. Object collision read

- [x] 2.1 Add `POST /types/objects/collision` (1 to 256 IDs, per-ID result) to `ObjectDefinitionsEndpoints`; verified by `Hagalaz.Services.Cache.Tests` for a mixed valid/not-found batch, an empty batch and an oversized batch
- [x] 2.2 Add the web model and a per-ID cached read in the admin cache service; verified by a service spec that repeated IDs are fetched once and batches are chunked at 256

## 3. Editor collision values

- [x] 3.1 Add `map-collision.ts` with `CollisionFlag` bit constants, the shape × rotation writer table and `buildCollisionMap`; verified by specs for each wall shape and rotation (origin and neighbour, all three layers), solid/gateway gating, clip type 0, a rotated 2×1 standard object, a ground decoration, a bridge column and a wall on a loaded and an unloaded region border
- [x] 3.2 Add a web spec feeding the three region 12850 fixtures to `buildCollisionMap` and matching `collision-12850.json`; verified by `pnpm run admin:test`. If it disagrees, stop per the proposal

## 4. Overlay

- [x] 4.1 Draw movement and projectile clipping from collision values in one overlay; verify distinct projectile blockers, both wall sides, objects without models, and unsaved edits
- [x] 4.2 Keep one Walkability toggle with a shared legend; verify geometry is reused and unavailable definitions are reported without hiding known collision
- [x] 4.3 Add the hovered-tile collision readout from the existing hover point; verified by viewport/page specs for a tile with a floor block and a north wall and for no readout when the overlay is off

## 5. Integration checks

- [x] 5.1 From `Hagalaz.Web.App` run `pnpm run admin:test`, `pnpm run lint:biome` and `pnpm run admin:build`; all pass
- [x] 5.2 Run `Hagalaz.Services.Cache.Tests` and `Hagalaz.Services.GameWorld.Tests` with an isolated `--artifacts-path`; both pass with no new warnings
- [x] 5.3 Run `openspec validate map-collision-view --strict`; it passes
- [ ] 5.4 Open region 12850 at `http://localhost:4410`, turn walkability on, check the shared legend, hover a wall and a solid object, move a placement and confirm the overlay follows, with a clean console; verified by a recorded live check

## Workflow follow-up

- Archive `map-tile-flags` before archiving this change.
