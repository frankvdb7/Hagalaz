## Context

The client reads an opcode-zero lighting record after the terrain tiles. It selects an atmosphere globally for the scene.

## Decisions

Decode the lighting record separately from terrain so a damaged trailer cannot invalidate tiles. Skip known unrelated trailer records using the client layout. Keep the API field optional.

The map preview uses the anchor region as a stable substitute for the game camera's atmosphere lookup. Pass directional response to the existing model lighting routine and apply a linear color and intensity multiplier to map vertex colors, including blended terrain. This is a preview approximation of the hardware client. Individual model previews retain their isolated defaults.
