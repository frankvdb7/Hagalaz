## 1. Direction-specific regression coverage

- [x] 1.1 Add focused size-one tests for all eight directions, matching blockers, and non-matching blockers.
- [x] 1.2 Add focused size-two tests for all eight incoming footprints and direction-specific blockers.
- [x] 1.3 Add size-three and size-four southwest/southeast queue-coordinate and reconstructed-path regressions.
- [x] 1.4 Add size-three and size-four positive-edge graph-boundary regressions for east, north, north-east, north-west, and south-east.

## 2. Smart pathfinder corrections

- [x] 2.1 Correct the size-one southeast mask, size-two west cells, and size-two north right-edge mask.
- [x] 2.2 Correct variable-size southwest and southeast queued anchors and positive-direction bounds.

## 3. Verification

- [x] 3.1 Confirm the focused regressions fail before the production correction and pass afterward.
- [x] 3.2 Run the GameWorld test project, solution build, strict OpenSpec validation, and `git diff --check`.
