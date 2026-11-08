## Context

`OverlayType` reads opcode 11 as a priority (default 8, combined after decoding as `priority << 8 | id`) and opcode 12 as a flag. `Class274.method2698` lets flagged overlays claim the eight perimeter points of surrounding tiles, using `aBooleanArrayArray2813` for shape coverage. Overlay faces use a claimed point only when it outranks them; underlay faces use any claimed point.

Opcodes 13, 14, 16, 20, 21 and 22 reach only `Class356`'s per-tile arrays and `Class77`. Those are read back while building the second, underwater `Class274` pass from the `um`/`ul` archives (`aBoolean8388`, `Class49_Sub2`). There, opcode 13 is the fog colour, and depth fog is `clamp(depth * 256 / opcode14 + opcode16 / 255)`. The default bias of 255 would fully tint any surface tile. Opcode 14's value is `byte << 5` because its getter multiplies by 8.

## Decisions

- Group the values in nullable overlay-only `OverlayRenderProperties`, keeping raw values. Opcodes 20-22 keep numbered names because no proven physical meaning exists.
- Port the claim order exactly: diagonals unconditionally, then edges north, south, west, east, then the tile itself, each with `<=`.
- Reuse the terrain layer path per corner so a claimed point can change colour and texture. Surfaces with special materials keep their own texture and take only the colour.
- The client's edge subdivision (`bools`) is not ported, so midpoints exist only on shapes that already define them.
- Do not apply water fog to surface tiles. The `underwater-terrain` change uses it on the underwater layer.
