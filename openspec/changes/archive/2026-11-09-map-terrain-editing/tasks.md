# Tasks

## 1. Terrain patching

- [x] 1.1 Add `MapTerrainPatcher` and verify by test that an empty patch reproduces the real-region terrain fixture byte for byte, and that patching keeps flags, automatic heights, token order of other tiles and trailing atmosphere bytes.
- [x] 1.2 Cover height, underlay and overlay set and clear, shape and rotation opcodes, repeated tokens and re-applying the same patch, verifying each by decoding with `MapRenderDecoder`.

## 2. Service endpoints

- [x] 2.1 Add `POST /types/maps/{id}/terrain`, verifying by endpoint tests that changes appear in a later render read and other archives stay byte-identical.
- [x] 2.2 Add validation and refusal paths, verifying by endpoint tests that each out-of-range value, stored height 1, an absent archive and an undecodable archive return their error and leave the store unchanged.
- [x] 2.3 Add `GET /types/maps/floors`, verifying by endpoint test that it lists underlays and overlays with colour and texture values, and update the route inventory test.

## 3. Editor preview

- [x] 3.1 Add per-region tile changes to `MapSceneDraft` and derive preview regions, verifying by tests the plane-above shift, the 32-step and limit rules, shared border corners, unloaded neighbours left alone, and that objects follow the new heights.
- [x] 3.2 Load the floor catalog and painted floor textures, verifying by store and geometry tests that a floor new to the region previews with its colour and texture.
- [x] 3.3 Add raise, lower, flatten, paint and clear brushes with brush size, plane and floor picker using Angular Material, verifying by component tests that one stroke commits one undo step and that dragging with a brush does not orbit.

## 4. Saving

- [x] 4.1 Add the terrain request and generalise the store's save sequence, verifying by store tests the terrain-then-landscape order, stop at first failure, and the saved and failed report.
- [x] 4.2 Build per-region tile changes from the draft and include them in **Save changes**, verifying by tests that unchanged tiles and regions are omitted, the flags note appears, success reloads, and failure keeps edits.

## 5. Integration

- [x] 5.1 Measure a stroke's rebuild time on a 3 by 3 selection in the live editor and record it; if it exceeds 250 ms, stop and raise the proposal's stop condition.
- [x] 5.2 Run focused cache and service tests, admin tests, `lint:biome`, `fallow:audit` and the admin build, and check the tools at desktop and phone widths without confirming a save.
- [x] 5.3 Exercise one real save only on a region and cache copy the user nominates, and confirm the terrain after reload; otherwise report this as unverified.

Manual check: The user confirmed the save and reload check passed on 2026-11-09; the region and cache copy were not identified in this message.

Stop condition raised by 5.1: rebuilding after a stroke measured 356 to 404 ms on one region (12850), over the 250 ms limit. It happens once on stroke release, not during the drag. The 3 by 3 measurement could not be completed. The user accepted this cost on 2026-11-08; faster rebuilds, if wanted later, are a separate change.
