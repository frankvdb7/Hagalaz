## Why

Region previews use fixed lighting, even when the cache specifies a different atmosphere.

## What Changes

- Expose the revision-742 terrain trailer's global lighting record on the map render API.
- Use the selected anchor region's light color, intensity, direction, and response in the map preview.
- Preserve existing preview lighting for regions without usable atmosphere data.

## Capabilities

### Modified Capabilities

- `web-3d-map-rendering`: apply cache-authored scene lighting to map previews.

## Impact

Cache terrain decoding, map render API, and the Angular map geometry pipeline. Fog, HDR, skyboxes, point lights, cast shadows, and camera-driven transitions are outside this change.
