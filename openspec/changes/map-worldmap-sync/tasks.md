# Tasks

## 1. Format and generator

- [x] 1.1 Add `WorldMapAreaCodec` and verify by tests on a real `main` sample that parsing covers every byte, and that re-encoding a block decodes to the same tiles, including palette, literal, empty, overlay, multi-layer and object tiles.
- [x] 1.2 Use the object definition's map sprite and map icon values, which `ObjectType` already exposes, so no definition encoding changes.
- [x] 1.3 Add the tile generator with its inclusion rule, and verify by unit tests the base floors, the wall shapes and clip types, and excluded shapes.

## 2. Verification gate

- [x] 2.1 Run the generator over all 754 regions of `main` in a script outside the repository on a cache copy, apply `main`'s area part mapping, and record tile match counts and the most common mismatches in design.md.
- [x] 2.2 Check the recorded results against the design's thresholds; if any is missed, stop and report before starting section 3.

## 3. Refresh endpoint

- [x] 3.1 Add `MapWorldMapRefresher` and `POST /types/maps/worldmap/refresh`, verifying by endpoint tests that a refreshed region decodes to the generated tiles, other blocks are byte-identical, and no difference writes nothing.
- [x] 3.2 Add refusal paths, verifying by endpoint tests that a region not in `main`, unreadable map files, no regions and an invalid ID are reported or rejected without writing, and update the route inventory test.

## 4. Editor

- [x] 4.1 Add the refresh request and its state to the store and service, verifying by store tests the request, the skipped report and failure handling.
- [x] 4.2 Run the refresh once after all map saves succeed, verifying by page tests that it is sent once, not sent after a failed save, that its failure is reported separately, and that the confirmation discloses other areas.
- [x] 4.3 Add **Rebuild world map** to the more actions menu behind the confirmation dialog, verifying by page tests the confirm and cancel paths.

## 5. Integration

- [x] 5.1 Run focused cache and service tests, admin tests, `lint:biome`, `fallow:audit` and the admin build, and check the menu in the browser without confirming a write.
- [ ] 5.2 Exercise one real refresh only on a region and cache copy the user nominates, and confirm the change in the client's world map; otherwise report this as unverified.
