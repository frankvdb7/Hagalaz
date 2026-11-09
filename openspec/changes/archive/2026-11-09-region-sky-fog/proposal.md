## Why

Region previews always draw on the admin surface colour and never fade distant geometry, although the revision-742 environment record also authors a sky colour and a linear fog range.

## What Changes

- Decode the environment's sky/fog RGB and raw fog value instead of skipping them, and expose both on the optional map render atmosphere.
- Clear the map viewport to the selected anchor region's sky colour while region atmosphere is active.
- Add an optional linear fog preview that uses the client's eye-depth range.
- Decode the environment-map face texture IDs and use the resulting cube in material effects 7, 8 and 9.

## Capabilities

### Modified Capabilities

- `web-3d-map-rendering`: show cache-authored sky colour and linear fog in map previews.

## Impact

Cache terrain trailer decoding, the map render API, the shared model scene and the map viewport toolbar. Procedural skyboxes (opcode 128), the client-wide default environment map, HDR/bloom, camera-driven environment transitions and per-8x8 environment cells remain out of scope.
