# Tasks

## 1. Stroke overlay

- [x] 1.1 Derive stroke preview tiles from the stroke's points so far, verifying by unit tests the resulting heights, floors, cleared overlays and that tiles outside the brush are absent.
- [x] 1.2 Add `ModelScene.setStrokePreview` and verify by scene tests that it draws, replaces, hides and releases its GPU resources.
- [x] 1.3 Show the preview during a drag and remove it after the release is drawn, verifying by viewport and page tests the live updates, cancellation with no edit, and hand-over on release.
- [x] 1.4 Measure overlay work per sample at the largest brush on region 12850 and record it in design.md; it must be under 16 ms.

## 2. Separate surfaces

- [x] 2.1 Capture before screenshots of region 12850 and a water region, at the same camera, to compare against.
- [x] 2.2 Move per-mesh state in `ModelScene` into one class with no behaviour change, verifying by the existing scene tests and that the single-model preview still renders.
- [x] 2.3 Return terrain and objects separately from `buildMapScene`, with object-relative vertex ranges, verifying by updated geometry tests.
- [x] 2.4 Draw and pick the two surfaces, verifying by tests of picking, selection bounds, object drop, wireframe and image capture.
- [x] 2.5 Compare after screenshots with 2.1, including water and translucent surfaces; on a mismatch that cannot be matched, stop and report.

## 3. Cheaper release

- [x] 3.1 Reuse object geometry across paint strokes, verifying by a test that a floor edit does not rebuild or re-upload objects.
- [x] 3.2 Re-fit only placements on changed corners after a height stroke, verifying by tests that affected objects move and others keep their built vertices.

## 4. Integration

- [x] 4.1 Measure release times on region 12850 and record them in design.md; paint must be at most 250 ms and height at most 350 ms, otherwise stop and report.
- [x] 4.2 Run admin tests, model-viewer tests, `lint:biome`, `fallow:audit` and the admin build, and exercise brushes, undo, save confirmation and selection in the browser at desktop and phone widths without confirming a save.
