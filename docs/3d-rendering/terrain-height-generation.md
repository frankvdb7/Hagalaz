# Implicit terrain height generation

This document records the exact plane-0 height path used when a terrain tile ends with opcode `0` in `Hagalaz.GameClient` revision `6eac3762cc46cec484131369691b5221fd1277bf`.

Relevant client methods:

- `Class274.readLandscapeData(...)`
- `Class156.calculateHeight(...)`
- `TextureLoader.method652(...)`
- `Class170.method2039(...)`
- `AbstractQueue_Sub1.method6486(...)`
- `Class20.method466(...)`
- `Class257` sine/cosine lookup initialization

The names below are human-readable names for the verified behavior, not claims that the client classes have already been renamed.

## 1. Tile-level rule

For terrain opcode `0`:

```text
if plane == 0:
    generated = CalculateBaseHeight(sourceTileX + 932731,
                                    sourceTileY + 556238)
    legacyHeight = -(generated * 32)
else:
    legacyHeight = heightBelow - 960
```

The client expression for plane 0 is equivalent to:

```text
-Class156.calculateHeight(...) * 8 << 2
```

which is `-generated * 32`.

The inputs passed to `CalculateBaseHeight` must be the same source-map tile coordinates used by the client. For an offline normal-region decoder, derive them from the region's absolute tile origin and validate the convention against a real client/cache fixture before freezing the public contract.

## 2. Base-height function

`Class156.calculateHeight(x, y)` combines three interpolated noise samples:

```text
sample = InterpolatedNoise(x + 45365, y + 91923, 4) - 128
       + ((InterpolatedNoise(x + 10294, y + 37821, 2) - 128) >> 1)
       + ((InterpolatedNoise(x,         y,         1) - 128) >> 2)

height = 35 + truncateTowardZero(0.3 * sample)
height = clamp(height, 10, 60)
return height
```

Because `readLandscapeData` already adds `(932731, 556238)` before this function, the first two calls have those additional offsets as well. Keep the functions separated exactly as the client does; it makes parity testing easier than pre-combining constants.

C# `(int)(0.3 * sample)` has the same truncation-toward-zero behavior needed here.

## 3. Interpolated noise

`TextureLoader.method652(x, y, scale)` performs two-dimensional cosine interpolation over four smoothed lattice values.

For the scales used by terrain (`1`, `2`, `4`), `scale` is a power of two:

```text
cellX = x / scale
fracX = x & (scale - 1)
cellY = y / scale
fracY = y & (scale - 1)

n00 = SmoothNoise(cellX,     cellY)
n10 = SmoothNoise(cellX + 1, cellY)
n01 = SmoothNoise(cellX,     cellY + 1)
n11 = SmoothNoise(cellX + 1, cellY + 1)

north = CosineInterpolate(n00, n10, fracX, scale)
south = CosineInterpolate(n01, n11, fracX, scale)

return CosineInterpolate(north, south, fracY, scale)
```

All world-region coordinates normally used here are non-negative. If a tool ever supports negative source coordinates, preserve Java integer-division/remainder semantics explicitly rather than assuming floor division.

## 4. Smoothed lattice noise

`Class170.method2039(x, y)` smooths the raw deterministic pseudo-random value using corners, cardinal neighbors and center:

```text
corners = RawNoise(x - 1, y - 1)
        + RawNoise(x + 1, y - 1)
        + RawNoise(x - 1, y + 1)
        + RawNoise(x + 1, y + 1)

sides = RawNoise(x - 1, y)
      + RawNoise(x + 1, y)
      + RawNoise(x, y - 1)
      + RawNoise(x, y + 1)

center = RawNoise(x, y)

return (sides / 8) + (corners / 16) + (center / 4)
```

Use integer division.

## 5. Raw deterministic noise

`AbstractQueue_Sub1.method6486(x, y)` is:

```text
n = x + y * 57
n = (n << 13) ^ n
v = (1376312589 + (789221 + 15731 * (n * n)) * n) & 0x7fffffff
return (v >> 19) & 0xff
```

The client uses Java 32-bit signed integer overflow. A C# implementation must use `unchecked` arithmetic for the multiplications/additions/shifts that can overflow.

For example, conceptually:

```csharp
private static int RawNoise(int x, int y)
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

Keep this implementation pure and deterministic; it is ideal for table-driven tests.

## 6. Cosine interpolation

`Class20.method466(a, b, offset, scale)` calculates an integer interpolation weight from the client cosine table:

```text
cosIndex = offset * 8192 / scale
weight = (65536 - cosine[cosIndex]) >> 1

return (((65536 - weight) * a) >> 16)
     + ((weight * b) >> 16)
```

The cosine table is `Class257.anIntArray2684`.

## 7. Cosine lookup table

`Class257` creates 16,384 entries:

```text
angleStep = 0.0003834951969714103  // 2*pi / 16384

for i = 0..16383:
    sine[i]   = truncate(16384.0 * sin(i * angleStep))
    cosine[i] = truncate(16384.0 * cos(i * angleStep))
```

Only the cosine table is needed for implicit terrain height generation.

Generate the table once and share it. Do not call floating-point `Math.Cos` separately for every terrain interpolation; besides needless cost, doing so makes it easier for implementations to drift from the client's integer lookup behavior.

## 8. Complete reference pseudocode

```text
function DecodeImplicitHeight(plane, sourceX, sourceY, heightBelow):
    if plane > 0:
        return heightBelow - 960

    baseHeight = CalculateBaseHeight(sourceX + 932731,
                                     sourceY + 556238)
    return -(baseHeight * 32)

function CalculateBaseHeight(x, y):
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

    n00 = SmoothNoise(cellX,     cellY)
    n10 = SmoothNoise(cellX + 1, cellY)
    n01 = SmoothNoise(cellX,     cellY + 1)
    n11 = SmoothNoise(cellX + 1, cellY + 1)

    a = CosineInterpolate(n00, n10, fracX, scale)
    b = CosineInterpolate(n01, n11, fracX, scale)
    return CosineInterpolate(a, b, fracY, scale)

function SmoothNoise(x, y):
    corners = RawNoise(x-1,y-1) + RawNoise(x+1,y-1)
            + RawNoise(x-1,y+1) + RawNoise(x+1,y+1)
    sides = RawNoise(x-1,y) + RawNoise(x+1,y)
          + RawNoise(x,y-1) + RawNoise(x,y+1)
    center = RawNoise(x,y)
    return sides/8 + corners/16 + center/4

function RawNoise(x, y):
    unchecked int32:
        n = x + y*57
        n = (n << 13) xor n
        v = (1376312589 + (789221 + 15731*(n*n))*n) & 0x7fffffff
        return (v >> 19) & 0xff

function CosineInterpolate(a, b, offset, scale):
    index = offset * 8192 / scale
    weight = (65536 - cosine[index]) >> 1
    return (((65536-weight)*a) >> 16) + ((weight*b) >> 16)
```

## 9. Encoder implications

If a decoded tile preserves that its source height encoding was opcode `0`, an unchanged tile can simply re-emit opcode `0`; the writer does not need to reverse the noise function.

For a canonical semantic encoder that decides between implicit and explicit height:

1. Calculate the implicit height using this exact function.
2. If desired height equals implicit height, write opcode `0`.
3. Otherwise encode opcode `1` using the explicit 32-unit delta rules from `cache-map-codecs.md`.

This is why region/source coordinates belong in the terrain codec context even if the encoded payload itself contains no region ID.

## 10. Required tests

Do not test only self-consistency. Capture client-parity expected values for fixed source coordinates.

At minimum test:

- `RawNoise` at several positive coordinate pairs including large world coordinates;
- `SmoothNoise` against fixed expected integers;
- `CosineInterpolate` at offsets `0`, midpoint and `scale-1` for scales `1`, `2`, `4`;
- `InterpolatedNoise` for each scale used by `CalculateBaseHeight`;
- `CalculateBaseHeight` values below/inside/above the clamp range;
- plane-0 final legacy height equals `-baseHeight * 32`;
- planes 1..3 implicit height exactly subtract `960` from the previous plane;
- known real `mX_Y` tiles ending in opcode `0` match the height observed through the client/scene fixture;
- region-boundary coordinates use the same absolute/source coordinate convention on both sides of the seam.

A test that only compares the C# implementation to another C# copy of these formulas is not sufficient. Keep at least a small table of expected constants captured from the client or independently decoded real cache fixture.