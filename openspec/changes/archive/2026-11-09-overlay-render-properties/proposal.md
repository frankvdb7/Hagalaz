## Why

The cache decoder discards overlay priority and water parameters, and the map preview never lets an overlay spread into neighbouring tiles, so overlay borders are harder than in the client.

## What Changes

- Decode and expose overlay opcodes 11, 12, 13, 14, 16, 20, 21 and 22.
- Apply opcode 11 priority and opcode 12 spreading to floor vertices in the map preview.

## Capabilities

### Modified Capabilities

- `web-3d-map-rendering`: blend spreading overlays into neighbouring tile perimeters.

## Impact

Cache floor decoding, the map render API and the Angular terrain geometry. The underwater pass that consumes the water parameters is the separate `underwater-terrain` change.
