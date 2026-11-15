## Context

`RegionManager` loads `um{x}_{y}` and `ul{x}_{y}` without XTEA keys into a second one-plane `Map`. Tile records use the surface format, except that height opcode 1 stores `32 * byte` with no special case for 1, and opcode 0 stores 0. `Class274.method2690` then adds surface plane 0 heights across the scene. A real-cache probe decoded all 1,577 `um` archives with no leftover bytes.

`Class49_Sub2`'s ground program computes `max(distanceTerm, (depth - 1) * 256 / scale + bias / 255)` and interpolates towards the fog colour after texturing. Fog parameters come from the surface overlay on the same tile (`Class356.method4141`). `OverlayType` opcode 14's getter multiplies by 8, so its value is `byte << 5`.

## Decisions

- Keep the API layer separate from the four surface planes. The browser appends it as plane slot 4, so terrain, spreading, smoothing, object placement and texture loading reuse existing per-plane code.
- Apply fog through vertex colours. Fully fogged triangles drop their texture and draw the flat fog colour, matching the client's result. Partly fogged textured floors are an approximation because vertex colours multiply the texture.
- Tiles without a surface overlay get no fog. The client's zeroed parameters divide by zero there, which is not meaningful to reproduce.
- Use the OpenGL program's formula. The shader backend's `HeightFogPlane` has a 256x smaller slope, but real data uses bias 255 throughout, which saturates under both.
- Real water materials are opaque (alpha mode 0, effect 4) and cover the seabed, so the preview offers a water surface toggle. It selects floors by material effect rather than texture ID.
- The distance term, and the separate object fog (`Class77(0x182838, 40, 127)`), are not ported.
