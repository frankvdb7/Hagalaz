# RuneScape map cache decode and encode guide

This document is the implementation reference for reading and writing the cache data needed by Hagalaz's 3D map renderer. It deliberately separates the **cache container format**, **terrain payload**, **location/object-placement payload**, and the higher-level render projection. Mixing these layers is a common source of corrupt cache files.

Reference snapshots:

- `frankvdb7/Hagalaz` `main` at `ba134cbcd531a6cd3b35c19b20404084e926bbfc`.
- `frankvdb7/Hagalaz.GameClient` `main` at `6eac3762cc46cec484131369691b5221fd1277bf`.

Primary Hagalaz sources:

- `Hagalaz.Cache/Types/Providers/MapProvider.cs`
- `Hagalaz.Cache/Logic/Codecs/MapCodec.cs`
- `Hagalaz.Cache.Extensions/BigEndianMemoryStreamExtensions.cs`
- `Hagalaz.Cache/CacheApi.cs`
- `Hagalaz.Cache/Logic/CacheWriter.cs`
- `Hagalaz.Cache/Utilities/XTEADecryptor.cs`

Primary client sources:

- `src/main/java/RegionManager.java`
- `src/main/java/Map.java`
- `src/main/java/Class274.java`
- `src/main/java/ObjectDefinition.java`
- `src/main/java/ModelDefinition.java`

## 1. The four layers must stay separate

A region is not stored as the stream currently accepted by `MapCodec.Decode`.

The real cache contains two separately named index-5 files:

```text
m{regionX}_{regionY}   terrain/floor payload
l{regionX}_{regionY}   location/object-placement payload
```

`MapProvider` reads those two files independently, then creates this **Hagalaz-internal synthetic stream** before calling `MapCodec.Decode`:

```text
[int32 terrainLength, big endian]
[terrainLength bytes of mX_Y payload]
[remaining bytes of lX_Y payload]
```

The four-byte terrain-length prefix therefore exists only to multiplex two already-decoded payloads into the current `MapCodec` API. It is **not part of either RuneScape cache file**.

Never write the output of the current `MapCodec.Encode` directly to index 5 as either an `mX_Y` or `lX_Y` file.

A correct long-term design should make this separation explicit, for example:

```text
RegionCacheData
  TerrainPayload
  LocationPayload

TerrainCodec
  Decode(mPayload, regionCoordinates)
  Encode(terrain)

LocationCodec
  Decode(lPayload, terrainFlags)
  Encode(locations)
```

A compatibility adapter may continue to implement `IMapCodec` while older callers exist.

## 2. Region addressing

A region ID packs its region coordinates as:

```text
regionId = (regionX << 8) | regionY
regionX  = regionId >> 8
regionY  = regionId & 0xFF
```

The region-local tile domain is:

```text
plane: 0..3
x:     0..63
y:     0..63
```

Absolute tile coordinates for a normal region are:

```text
absoluteX = regionX * 64 + localX
absoluteY = regionY * 64 + localY
```

Absolute coordinates matter for plane-0 implicit terrain heights because opcode `0` uses the client's deterministic procedural height function. A decoder that knows only the raw `mX_Y` bytes but not the region coordinates cannot reproduce those heights correctly.

## 3. Reading the cache container

`MapProvider` resolves the index-5 file IDs through the reference table:

```text
terrainFileId  = GetFileId(5, $"m{regionX}_{regionY}")
locationFileId = GetFileId(5, $"l{regionX}_{regionY}")
```

### Terrain container

Terrain is currently read as:

```text
ReadContainer(5, terrainFileId)
```

The returned container's `Data` is the raw terrain opcode stream described below.

### Location container and XTEA

Location/object data is read as:

```text
ReadContainer(5, locationFileId, xteaKeys)
```

`CacheApi.ReadContainer(indexId, fileId, xteaKeys)` operates at the **container level**:

1. Read the raw encoded cache container.
2. If all four XTEA keys are zero, decode it normally.
3. Otherwise decrypt from raw container byte offset `5`.
4. Process only complete 8-byte XTEA blocks.
5. Decode/decompress the resulting container.
6. Return the container payload as `Data`.

Do not XTEA-decrypt the already-decompressed `lX_Y` payload. The encryption wraps the encoded container body, not the semantic location stream.

### XTEA algorithm

Use four 32-bit keys and 32 rounds. Values are big-endian 32-bit words. The conventional encryption form is:

```text
for each complete 8-byte block:
    v0 = readUInt32BE()
    v1 = readUInt32BE()
    sum = 0
    delta = 0x9E3779B9

    repeat 32 times:
        v0 += (((v1 << 4) ^ (v1 >> 5)) + v1) ^ (sum + key[sum & 3])
        sum += delta
        v1 += (((v0 << 4) ^ (v0 >> 5)) + v0) ^ (sum + key[(sum >> 11) & 3])

    writeUInt32BE(v0)
    writeUInt32BE(v1)
```

Use unchecked 32-bit arithmetic. Decryption performs the inverse operations starting with `sum = delta * 32`.

The current repository has a decryptor but no symmetric XTEA-aware cache write API. Therefore **writing an encrypted `lX_Y` container is not yet safely supported by `ICacheAPI.Write`**. Add and test that capability before exposing map mutation that persists location data.

## 4. Terrain payload decoding (`mX_Y`)

The terrain stream is a sequence of tile opcode lists. There is no tile-count header. Decode exactly this traversal order:

```text
for plane = 0..3
    for localX = 0..63
        for localY = 0..63
            decode one tile until opcode 0 or 1 terminates it
```

Every tile consumes at least one byte because it must end with opcode `0` or `1`.

### Required render representation

Do not collapse terrain to one flag byte. Preserve at least:

```text
TerrainTile
  SourcePlane
  Height
  HeightEncoding       // Implicit or Explicit; useful for lossless/canonical writing
  ExplicitHeightByte?  // preserve when opcode 1 was used
  OverlayId
  OverlayShape
  OverlayRotation
  TerrainFlags
  UnderlayId
```

For region-edge mesh generation also retain or derive a 65×65 corner-height grid per plane. Do not invent the east/north border by duplicating the last in-region value; use adjacent-region data or a deterministic client-parity derivation.

### Tile decoder

Initialize the tile's optional fields to zero/none, then repeatedly read an **unsigned opcode byte**:

#### Opcode `0`: implicit/default height and end of tile

This terminates the current tile.

- Plane 0: compute the deterministic client terrain height using the tile's absolute coordinates and the exact client noise function.
- Plane 1..3: `height = heightBelow - 960` legacy scene-height units.

The exact plane-0 noise helper should be ported and covered by fixture tests before the renderer depends on opcode-0 height output. Do not substitute random noise or a generic Perlin implementation.

If byte-preserving editing is desired, record that this tile used implicit height even after deriving its numeric height.

#### Opcode `1`: explicit height byte and end of tile

Read one unsigned byte `heightByte`, then terminate the current tile.

The client treats encoded byte `1` as `0`.

For this client revision, explicit height uses a 32-scene-unit step:

```text
if heightByte == 1:
    heightByte = 0

if plane == 0:
    height = -(heightByte * 32)
else:
    height = heightBelow - (heightByte * 32)
```

Keep these legacy signed heights in the server transport/domain representation. Convert the sign exactly once in the web scene assembler.

#### Opcodes `2..49`: overlay

Read one overlay ID byte. The client stores the payload as a byte and later interprets IDs as unsigned where needed.

Decode shape and rotation as:

```text
overlayId       = readBytePayload()
overlayShape    = (opcode - 2) / 4
overlayRotation = (opcode - 2 + partRotation) & 3
```

For a normal source region `partRotation` is `0`.

For dynamic 8×8 assembly, `partRotation` is an assembly-time transform. Do not permanently write the dynamically rotated value back into the source region's `mX_Y` file.

The opcode range gives shapes `0..11` and rotations `0..3`.

#### Opcodes `50..81`: terrain flags

```text
terrainFlags = opcode - 49
```

This gives stored values `1..32`. Zero means no flag opcode occurred.

These flags include bridge/plane-related semantics but are not the height, material or collision map themselves.

#### Opcodes `82..255`: underlay

```text
underlayId = opcode - 81
```

Zero means no underlay opcode was present.

### Multiple non-terminal opcodes

A tile can contain overlay, terrain-flag and underlay opcodes before its final height opcode. The decoder must continue until `0` or `1`.

Do not assume the first byte describes the entire tile.

## 5. Terrain payload encoding

Define the goal first:

- **Semantic round trip**: `Decode(Encode(tile))` reproduces the same tile semantics. This should be mandatory.
- **Byte-identical round trip**: output bytes exactly match the original archive. This requires preserving original opcode ordering and encoding choices and is not necessary for the web renderer.

A canonical semantic encoder is easier to test and maintain.

### Canonical tile opcode order

A deterministic writer can emit:

```text
1. overlay, if present
2. terrain flag, if non-zero
3. underlay, if present
4. height terminator: opcode 0 or opcode 1 + byte
```

The client decoder does not require that particular order, but canonical output makes tests and diffs stable.

### Encode overlay

Given shape `s` and source rotation `r`:

```text
validate 0 <= s <= 11
validate 0 <= r <= 3
opcode = 2 + (s * 4) + r
writeByte(opcode)
writeByte(overlayId)
```

Only emit an overlay opcode when an overlay is semantically present. Validate that the ID fits the one-byte cache representation.

### Encode terrain flags

```text
if terrainFlags != 0:
    validate 1 <= terrainFlags <= 32
    writeByte(terrainFlags + 49)
```

### Encode underlay

```text
if underlayId != 0:
    validate 1 <= underlayId <= 174
    writeByte(underlayId + 81)
```

### Encode height

If the original `HeightEncoding` is preserved and the tile has not changed, re-emitting the same `0` or `1` encoding is safest.

For a canonical semantic writer:

1. Compute the height that opcode `0` would produce for this tile.
2. If the desired height equals that implicit value, emit `0`.
3. Otherwise derive an explicit byte.

For plane 0:

```text
delta = -height
```

For higher planes:

```text
delta = heightBelow - height
```

Then:

```text
require delta >= 0
require delta % 32 == 0
heightByte = delta / 32
require 0 <= heightByte <= 255
```

If `heightByte == 1`, it cannot be represented distinctly because the client normalizes encoded `1` to zero. Either use implicit encoding when semantically equivalent or reject the write rather than silently changing height.

Finally:

```text
writeByte(1)
writeByte(heightByte)
```

Do not silently clamp unrepresentable heights.

## 6. Location/object-placement payload decoding (`lX_Y`)

The location stream is delta encoded first by object ID and then by packed location.

### Smart integer primitives

Hagalaz's `ReadSmart` is:

```text
peek first unsigned byte
if first < 128:
    value = readUnsignedByte()
else:
    value = readUnsignedShortBE() - 32768
```

Valid values for this format are `0..32767`.

`ReadHugeSmart` chains smart values:

```text
total = 0
value = ReadSmart()
while value == 32767:
    total += 32767
    value = ReadSmart()
return total + value
```

### Object/location decoder

Start with:

```text
objectId = -1
```

Then:

```text
while true:
    idDelta = ReadHugeSmart()
    if idDelta == 0:
        break

    objectId += idDelta
    packedLocation = 0

    while true:
        locationDelta = ReadSmart()
        if locationDelta == 0:
            break

        packedLocation += locationDelta - 1

        localY = packedLocation & 0x3F
        localX = (packedLocation >> 6) & 0x3F
        sourcePlane = packedLocation >> 12

        attributes = readUnsignedByte()
        shape = attributes >> 2
        rotation = attributes & 0x3

        emit placement
```

Packed location is therefore:

```text
packedLocation = (sourcePlane << 12) | (localX << 6) | localY
```

### Source plane versus effective plane

This is critical for round-trip correctness.

The current Hagalaz decoder applies the bridge flag immediately:

```text
if (terrainFlags[1, x, y] & 0x2) != 0:
    z--
```

That adjusted plane is useful for gameplay placement, but it is **not the same thing as the source plane encoded in `lX_Y`**.

A full-fidelity render/edit representation must preserve both concepts, for example:

```text
ObjectPlacement
  Id
  SourcePlane       // raw packed plane; use this when re-encoding lX_Y
  EffectivePlane    // bridge-adjusted/runtime plane
  X
  Y
  Shape
  Rotation
```

If decode overwrites the source plane and encode later uses the adjusted plane, a decode/encode cycle moves bridged objects to a different raw plane.

## 7. Location/object-placement payload encoding

Canonical object encoding requires deterministic sorting.

### Required sort order

1. Group placements by object ID.
2. Sort groups by object ID ascending.
3. Within each group, compute the raw packed location from `SourcePlane`, X and Y.
4. Sort placements by packed location ascending.

The current `MapCodec.Encode` sorts groups by object ID but does **not** explicitly sort locations inside a group. That is unsafe if callers provide placements in arbitrary order because location deltas must never go backwards.

### Encode object-ID group

Initialize:

```text
previousObjectId = -1
```

For each object-ID group:

```text
idDelta = objectId - previousObjectId
require idDelta > 0
WriteHugeSmart(idDelta)
previousObjectId = objectId
```

Then initialize:

```text
previousPackedLocation = 0
```

For each sorted placement:

```text
packed = (sourcePlane << 12) | (x << 6) | y
locationDelta = packed - previousPackedLocation + 1
require 1 <= locationDelta <= 32767
WriteSmart(locationDelta)
previousPackedLocation = packed

attributes = (shape << 2) | rotation
writeByte(attributes)
```

After the final placement for an object ID:

```text
WriteSmart(0)
```

After the final object-ID group:

```text
WriteHugeSmart(0)
```

### Correct huge-smart writer

The inverse of `ReadHugeSmart` should be:

```text
function WriteHugeSmart(value):
    require value >= 0

    while value >= 32767:
        WriteSmart(32767)
        value -= 32767

    WriteSmart(value)
```

The current `BigEndianMemoryStreamExtensions.WriteHugeSmart` should receive a regression test before it is trusted for arbitrary large deltas. Its current loop can overshoot the requested total for values just above `32767`, producing a negative remainder that is then passed to `WriteSmart`.

Required boundary tests include at least:

```text
0
1
127
128
32766
32767
32768
65534
65535
large real object-ID delta
```

## 8. Rebuilding cache files

### Terrain write

For an existing region:

1. Read the existing `mX_Y` container so its compression/version policy is known.
2. Encode only the raw terrain payload; do not prepend the Hagalaz synthetic terrain length.
3. Construct a replacement container using the existing compression type.
4. Write through the cache writer so version, CRC, Whirlpool digest and reference-table metadata are updated consistently.
5. Read the file back and decode it as verification.

### Location write

For `lX_Y`:

1. Encode only the raw location payload.
2. Build the cache container with the intended compression/version behavior.
3. XTEA-encrypt the encoded container body from byte offset `5`, using the region's four keys and complete 8-byte blocks only.
4. Compute/store CRC and digest from the **final encrypted container bytes**, because that is what the cache contains.
5. Persist the bytes and update the reference table atomically from the caller's perspective.
6. Read back through `ReadContainer(..., xteaKeys)` and verify semantic equality.

The existing `CacheWriter.Write` computes checksums and writes the unencrypted `container.Encode(false)` bytes. Therefore do not fake encrypted map writes by calling it and encrypting the store afterward: the reference-table checksum would describe different bytes. Add an XTEA-aware writer path that owns encode → encrypt → checksum/digest → store → reference-table update in that order.

## 9. Dynamic 8×8 chunks

Dynamic regions are an assembly operation over source cache regions, not a different `mX_Y`/`lX_Y` file format.

For a source 8×8 chunk:

- select source region, source plane and source chunk coordinates;
- rotate tile coordinates by `partRotation`;
- add `partRotation` to overlay rotation modulo 4;
- rotate object local coordinates using object footprint semantics;
- add placement rotation modulo 4;
- copy/derive height borders consistently.

Keep source cache data immutable during assembly. Do not encode an assembled dynamic chunk back into the source region merely because its rendered rotations differ.

## 10. Object definitions, floor definitions and models

Map placement data alone does not contain renderable models.

### Object definitions

Hagalaz already has `ObjectTypeCodec`; extend/reuse cache decoding rather than parsing object-definition opcodes in Angular. For rendering, expose a separate projection containing only verified render fields such as shape-to-model mappings, recolor/retexture pairs, scale, offsets, contouring and transform IDs.

Round-trip tests for any newly exposed opcode must follow the same rule:

```text
raw fixture -> decode -> assert semantic fields -> encode -> decode -> assert semantic fields
```

Unknown opcodes must never be guessed. Either preserve their raw representation in an editing codec or reject mutation that cannot be safely re-encoded.

### Floor definitions

`OverlayType` and the client underlay definition (`Class491`) are separate config formats from `mX_Y`. The terrain payload stores their IDs, not their RGB/texture bodies. Implement floor-definition codecs independently and resolve IDs after region decode.

### Models

`ModelDefinition` supports at least two binary model variants: its constructor selects a newer decoder when the final two bytes are `0xFF 0xFF`, otherwise it uses the legacy decoder. It contains delta-compressed vertex/triangle streams, optional face attributes and texture-triangle data.

Hagalaz currently has no server-side model codec. For the static-object milestone:

1. Rename/decompose the client's `method1199` and `method1201` into documented legacy/new format layouts.
2. Build immutable `DecodedModel` fixtures from real cache models covering untextured, textured, alpha, multiple texture-render types and combined models.
3. Implement the decoder first and compare every vertex/index/material field with the client.
4. Do not implement a model encoder merely by reversing observed reads inline.
5. Define a canonical writer per model version only after every optional section offset and footer flag is understood.
6. Add semantic round-trip plus client-load tests before allowing cache model mutation.

The web map viewer does not need model **encoding** to render static objects, so model-writing support should remain a later capability unless an actual editor requires it.

## 11. Validation and corruption guards

Every decoder should reject or explicitly report malformed data rather than returning a half-valid region.

Validate at minimum:

- unexpected end-of-stream while decoding a tile;
- missing tile terminator;
- fewer/more than 4×64×64 terrain tiles when bytes remain in an impossible state;
- overlay shape/rotation outside representable bounds when encoding;
- terrain flag/underlay IDs outside byte-format bounds;
- source plane outside `0..3`;
- local X/Y outside `0..63`;
- object shape that does not fit `attributes >> 2` expectations;
- rotation outside `0..3`;
- non-monotonic object IDs or packed locations during encoding;
- smart/huge-smart overflow;
- XTEA key count other than four;
- invalid decrypted/decompressed container;
- checksum/digest mismatch after persisted writes where validation data is available.

Never catch a malformed map exception and silently substitute flat terrain. A visible failure is safer than a plausible but incorrect scene.

## 12. Required test matrix

### Terrain byte fixtures

Cover individual and combined tile sequences:

- opcode `0` on plane 0 with known absolute coordinates;
- opcode `0` on planes 1..3 and exact `-960` spacing;
- opcode `1` with height byte `0`, `1`, a normal value and `255`;
- overlay shapes `0` and `11`, all four rotations;
- terrain flags `1` and `32`;
- minimum and maximum representable underlay IDs;
- overlay + flag + underlay + explicit height on one tile;
- canonical encode/decode semantic equality.

### Region fixtures

Use real cache fixtures for:

- a flat/simple region;
- strongly varying terrain;
- bridges;
- multiple planes;
- many overlay shapes/rotations;
- adjacent east/north/north-east regions for border-height tests;
- an XTEA-protected location archive.

### Location fixtures

Cover:

- one object;
- multiple placements of the same ID;
- multiple object IDs;
- unsorted input passed to the encoder;
- largest local coordinate and plane;
- bridge tile preserving `SourcePlane` while deriving `EffectivePlane`;
- huge-smart object-ID boundaries;
- decode → encode → decode semantic equality.

### Persisted-write verification

For any future map editor test cache:

```text
read original container
-> decode
-> modify one known field
-> encode
-> persist
-> reopen cache from disk
-> decode again
-> verify intended change
-> verify unrelated region semantics unchanged
-> verify reference-table CRC/version/digest
```

Never test destructive write paths against the canonical development cache fixture.

## 13. Current Hagalaz codec gaps to fix before calling it a full map codec

The existing implementation is useful for gameplay, but it is not a full-fidelity cache round-trip codec:

1. `MapCodec.DecodeTerrainData` discards explicit height, overlay, overlay shape/rotation and underlay data.
2. Plane-0 procedural heights are not represented.
3. `MapCodec.Encode` can only recreate the reduced terrain-flag model; it cannot recreate a real `mX_Y` payload faithfully.
4. `MapCodec.Encode` returns Hagalaz's synthetic combined stream, not one actual cache file.
5. Location decode mutates the raw plane for bridge tiles and therefore loses source-plane information needed for correct re-encoding.
6. Location encoding does not explicitly sort placements by packed location inside an object-ID group.
7. `WriteHugeSmart` needs boundary regression coverage/correction before arbitrary object-ID deltas are safe.
8. Cache writing has no XTEA-aware symmetric path for encrypted `lX_Y` containers.
9. Existing round-trip tests prove only the reduced in-memory model round trips through itself; they do not prove parity with real terrain/location cache bytes.

The 3D rendering foundation should address decoding first, preserve enough source semantics to make later encoding possible, and add cache mutation only behind separate explicit tests and APIs.