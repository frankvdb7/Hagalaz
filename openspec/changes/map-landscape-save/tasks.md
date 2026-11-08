# Tasks

## 1. Landscape encoding

- [x] 1.1 Fix `WriteHugeSmart` for values of 32,767 and above and verify with regression tests in `BigEndianMemoryStreamExtensionsTests` at 32,766, 32,767, 32,768, 65,534 and 70,000.
- [x] 1.2 Add `MapLandscapeEncoder` and verify by test that decoding its output returns the input for unsorted placements, duplicate locations, ID gaps of 32,767 or more, every plane, and an empty list.

## 2. Save endpoint

- [x] 2.1 Add `POST /types/maps/{id}/landscape`, verifying by endpoint tests that a save is returned by a later render read, that an empty list clears placements, and that terrain and other archives are byte-identical afterwards.
- [x] 2.2 Add validation and refusal paths, verifying by endpoint tests that each out-of-range field, an invalid region ID, an absent archive and an undecodable archive return their error and leave the store unchanged.

## 3. Editor save

- [x] 3.1 Add the landscape request to `CacheMapsService` and a sequential save with `saving` and result state to `CacheMapSceneStore`, verifying by store tests the request order, stop at first failure, the saved and failed region report, and that `saving` resets.
- [x] 3.2 Build per-region placement lists from the draft on the map detail page, verifying by tests that only edited regions are included, with moves, rotations, removals and additions applied and scale omitted.
- [x] 3.3 Add **Save changes** to the more actions menu behind `CacheWriteDialogComponent`, verifying by page tests availability, that cancelling writes nothing, the scale disclosure, success clearing edits and reloading, and failure keeping edits with its message.
- [x] 3.4 Keep underwater objects out of saved surface landscapes and disclose edits to them, verifying by tests of the per-region lists and confirmation notes.

## 4. Integration

- [x] 4.1 Run the focused cache and service tests, admin tests, `lint:biome`, `fallow:audit` and the admin build, and check in the browser that the menu item enables and that cancelling the dialog leaves the edits in place.
- [ ] 4.2 Exercise one real save only on a region and cache copy the user nominates, and confirm the placements after reload; otherwise report this as unverified.
