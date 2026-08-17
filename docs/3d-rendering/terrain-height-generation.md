# Implicit terrain height generation

This document records the exact plane-0 height path used when a terrain tile ends with opcode `0` in `Hagalaz.GameClient` revision `6eac3762cc46cec484131369691b5221fd1277bf`.

The algorithm is described entirely with semantic helper names. Current decompiled source locators are listed only in the final section and in [Rendering deobfuscation map](client-rendering-deobfuscation.md).

## 1. Tile-level rule

For terrain opcode `0`:

```text
if plane == 0:
    generated = GenerateBaseTerrainHeight(sourceTileX + 932731,
                                          sourceTileY + 556238)
    legacyHeight = -(generated * 32)
else:
    legacyHeight = heightBelow - 960
```

The client multiplies the generated plane-0 value by `-32` scene-height units.

The inputs to `GenerateBaseTerrainHeight` must be the same source-map tile coordinates used by the client. For an offline normal-region decoder, derive them from the region's absolute tile origin and verify that convention against a real cache/client fixture before making it part of a public contract.

## 2. Base-height function

`GenerateBaseTerrainHeight(x, y)` combines three interpolated noise samples:

```text
sample = InterpolatedNoise(x + 45365, y + 91923, 4) - 128
       + ((InterpolatedNoise(x + 10294, y + 37821, 2) - 128) >> 1)
       + ((InterpolatedNoise(x,         y,         1) - 128) >> 2)

height = 35 + truncateTowardZero(0.3 * sample)
height = clamp(height, 10, 60)
return height
```

Because the terrain decoder already adds `(932731, 556238)` before this function, the first two calls receive those source-coordinate offsets in addition to their own octave offsets.

Keep the helpers separated as the client does. That makes parity testing easier and prevents accidental pre-combination of constants from changing integer behavior.

C# `(int)(0.3 * sample)` has the required truncation-toward-zero behavior.

## 3. Interpolated noise

`InterpolatedNoise(x, y, scale)` performs two-dimensional cosine interpolation over four smoothed lattice values.

For the terrain scales (`1`, `2`, `4`), `scale` is a power of two:

```text
cellX = x / scale
fracX = x & (scale - 1)
cellY = y / scale
fracY = y & (scale - 1)

n00 = SmoothedNoise(cellX,     cellY)
n10 = SmoothedNoise(cellX + 1, cellY)
n01 = SmoothedNoise(cellX,     cellY + 1)
n11 = SmoothedNoise(cellX + 1, cellY + 1)

north = CosineInterpolate(n00, n10, fracX, scale)
south = CosineInterpolate(n01, n11, fracX, scale)

return CosineInterpolate(north, south, fracY, scale)
```

All normal RuneScape world-region coordinates used here are non-negative. If a future tool supports negative source coordinates, preserve Java integer-division/remainder behavior explicitly rather than assuming mathematical floor division.

## 4. Smoothed lattice noise

`SmoothedNoise(x, y)` combines deterministic samples from the four corners, four cardinal neighbors and the center:

```text
corners = RawTerrainNoise(x - 1, y - 1)
        + RawTerrainNoise(x + 1, y - 1)
        + RawTerrainNoise(x - 1, y + 1)
        + RawTerrainNoise(x + 1, y + 1)

sides = RawTerrainNoise(x - 1, y)
      + RawTerrainNoise(x + 1, y)
      + RawTerrainNoise(x, y - 1)
      + RawTerrainNoise(x, y + 1)

center = RawTerrainNoise(x, y)

return (sides / 8) + (corners / 16) + (center / 4)
```

Use integer division.

## 5. Raw deterministic noise

`RawTerrainNoise(x, y)` is:

```text
n = x + y * 57
n = (n << 13) ^ n
v = (1376312589 + (789221 + 15731 * (n * n)) * n) & 0x7fffffff
return (v >> 19) & 0xff
```

The client uses Java 32-bit signed integer overflow. A C# implementation must use `unchecked` arithmetic for multiplications, additions and shifts that can overflow.

Conceptually:

```csharp
private static int RawTerrainNoise(int x, int y)
{
    unchecked
    {
        var n = x + y * 57;
        n = (n << 13) ^ n;
        var value = (1376312589 + (789221 + 15731 * (n * n)) * n) & 0x7fffffff;
        return (value >> 19) & 0xff;
    }
}
```

Keep this implementation pure and deterministic; it is ideal for table-driven parity tests.

## 6. Cosine interpolation

`CosineInterpolate(a, b, offset, scale)` uses the client's fixed-point cosine lookup table:

```text
cosIndex = offset * 8192 / scale
weight = (65536 - cosineTable[cosIndex]) >> 1

return (((65536 - weight) * a) >> 16)
     + ((weight * b) >> 16)
```

This is fixed-point interpolation, not a generic floating-point easing function.

## 7. Trigonometry lookup tables

The client creates 16,384 entries:

```text
angleStep = 0.0003834951969714103  // 2*pi / 16384

for i = 0..16383:
    sineTable[i]   = truncate(16384.0 * sin(i * angleStep))
    cosineTable[i] = truncate(16384.0 * cos(i * angleStep))
```

Only the cosine table is required for implicit terrain height generation, but the paired 14-bit sine/cosine convention is used elsewhere in camera/rendering code.

Generate the tables once and share them. Do not call floating-point `Math.Cos` for each terrain interpolation; aside from needless work, doing that makes it easier to drift from the client's integer lookup behavior.

## 8. Complete reference pseudocode

```text
function DecodeImplicitHeight(plane, sourceX, sourceY, heightBelow):
    if plane > 0:
        return heightBelow - 960

    baseHeight = GenerateBaseTerrainHeight(sourceX + 932731,
                                           sourceY + 556238)
    return -(baseHeight * 32)

function GenerateBaseTerrainHeight(x, y):
    value = InterpolatedNoise(x + 45365, y + 91923, 4) - 128
    value += (InterpolatedNoise(x + 10294, y + 37821, 2) - 128) >> 1
    value += (InterpolatedNoise(x, y, 1) - 128) >> 2

    height = 35 + truncateTowardZero(0.3 * value)
    return clamp(height, 10, 60)

function InterpolatedNoise(x, y, scale):
    cellX = x / scale
    fracX = x & (scale - 1)
    cellY = y / scale
    fracY = y & (scale - 1)

    n00 = SmoothedNoise(cellX,     cellY)
    n10 = SmoothedNoise(cellX + 1, cellY)
    n01 = SmoothedNoise(cellX,     cellY + 1)
    n11 = SmoothedNoise(cellX + 1, cellY + 1)

    a = CosineInterpolate(n00, n10, fracX, scale)
    b = CosineInterpolate(n01, n11, fracX, scale)
    return CosineInterpolate(a, b, fracY, scale)

function SmoothedNoise(x, y):
    corners = RawTerrainNoise(x-1,y-1) + RawTerrainNoise(x+1,y-1)
            + RawTerrainNoise(x-1,y+1) + RawTerrainNoise(x+1,y+1)
    sides = RawTerrainNoise(x-1,y) + RawTerrainNoise(x+1,y)
          + RawTerrainNoise(x,y-1) + RawTerrainNoise(x,y+1)
    center = RawTerrainNoise(x,y)
    return sides/8 + corners/16 + center/4

function RawTerrainNoise(x, y):
    unchecked int32:
        n = x + y*57
        n = (n << 13) xor n
        v = (1376312589 + (789221 + 15731*(n*n))*n) & 0x7fffffff
        return (v >> 19) & 0xff

function CosineInterpolate(a, b, offset, scale):
    index = offset * 8192 / scale
    weight = (65536 - cosineTable[index]) >> 1
    return (((65536-weight)*a) >> 16) + ((weight*b) >> 16)
```

## 9. Encoder implications

If a decoded tile preserves that its source height encoding was opcode `0`, an unchanged tile can simply re-emit opcode `0`; the writer does not need to reverse the noise function.

For a canonical semantic encoder that decides between implicit and explicit height:

1. Calculate the implicit height using this exact function.
2. If desired height equals implicit height, write opcode `0`.
3. Otherwise encode opcode `1` using the explicit 32-unit delta rules from `cache-map-codecs.md`.

This is why region/source coordinates belong in terrain codec context even though the encoded payload itself contains no region ID.

## 10. Required tests

Do not test only self-consistency. Capture client-parity expected values for fixed source coordinates.

At minimum test:

- `RawTerrainNoise` at several positive coordinate pairs including large world coordinates;
- `SmoothedNoise` against fixed expected integers;
- `CosineInterpolate` at offset `0`, midpoint and `scale-1` for scales `1`, `2`, `4`;
- `InterpolatedNoise` for every scale used by `GenerateBaseTerrainHeight`;
- `GenerateBaseTerrainHeight` values below/inside/above the clamp range;
- plane-0 final legacy height equals `-baseHeight * 32`;
- planes 1..3 implicit height exactly subtract `960` from the previous plane;
- known real `mX_Y` tiles ending in opcode `0` match the height observed through the client/scene fixture;
- region-boundary coordinates use the same absolute/source coordinate convention on both sides of a seam.

A test comparing the C# implementation only to a second C# copy of the formulas is insufficient. Keep a small table of expected constants captured from the client or independently decoded real cache fixtures.

## 11. Current source locators

These identifiers are listed solely to find the current decompiled implementation. They are **not** the names to use in new code or architecture.

| Semantic helper | Current GameClient source locator |
| --- | --- |
| terrain opcode decoder | `Class274.readLandscapeData(...)` |
| `GenerateBaseTerrainHeight` | `Class156.calculateHeight(...)` |
| `InterpolatedNoise` | `TextureLoader.method652(...)` |
| `SmoothedNoise` | `Class170.method2039(...)` |
| `RawTerrainNoise` | `AbstractQueue_Sub1.method6486(...)` |
| `CosineInterpolate` | `Class20.method466(...)` |
| `sineTable` | `Class257.anIntArray2683` |
| `cosineTable` | `Class257.anIntArray2684` |
| 14-bit angle conversion | `Class257.method2541(...)` |

Do not rename the entire miscellaneous containing classes solely because these static helpers are understood. Source-level deobfuscation should rename the methods first unless the rest of the containing class has independently been identified.
