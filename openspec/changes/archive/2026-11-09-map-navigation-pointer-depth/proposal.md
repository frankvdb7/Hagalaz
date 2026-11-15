## Why

Perspective map views currently scale wheel zoom and right-button drag by the distance to the orbit focus. The far side of the same region responds less than the near side even when the renderer is running smoothly.

## What Changes

- Scale map wheel zoom and right-button drag using the depth under the pointer.
- Keep cursor anchoring and the existing orbit, keyboard and touch controls.
- Verify both gestures on near and far tiles and preserve the existing controls when the pointer has no usable ground depth.

## Capabilities

### Modified Capabilities

- `web-3d-map-rendering` gains depth-aware mouse navigation.

## Impact

Only the admin map's camera input changes. Completion requires a focused navigation regression and an admin build.
