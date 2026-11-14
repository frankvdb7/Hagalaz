# Design

## Context

See proposal.md, sections "Why" and "Stored layout evidence".

- **Decoder:** `ModelGeometryDecoder.Decode` parses the newer layout by rearranging its streams into the old layout and calling `DecodeLegacy`. It then adds:
  - texture mappings;
  - UV generation through `ModelTextureCoordinates.Generate`;
  - shading types;
  - attachments.
- **Production callers:** only `ModelEndpoints.ReadModel` and `ModelEndpoints.BuildModel`. Both pass `allowFirstTexelFallback: true`.
- **`ModelType`:** `Hagalaz.Cache/Types/ModelType` exists with no usages.

### Newer layout

Streams are read from the file start, followed by a 23-byte footer, with a version byte before the footer when flag 8 is set.

| Element | Detail |
|---|---|
| Mapping types | One byte per texture mapping |
| Per vertex | Vertex flags |
| Per face | Shading types (flag 0x01), strip codes, priorities (when global priority is 255), face skins, alpha |
| Per vertex | Vertex skins |
| Index deltas | Smart values |
| Per face | Texture IDs (when the textures switch is on) |
| Mapping pointers | One per textured face, when mappings exist |
| Per face | Colours |
| Coordinates | x/y/z deltas as smart values |
| Simple mappings | Short triangles |
| Complex mappings | Short triangles, then scales (6, 7 or 9 bytes by version), rotations, directions, offsets, and 2 extra bytes per box mapping |
| Attachments | Emitters and effectors (flag 0x02), billboards (flag 0x04) |

## Goals / Non-Goals

**Goals:**
- A field-based `ModelType` that re-encodes byte for byte.
- Geometry derivation moved out of the codec without changing any value.

**Non-Goals:**
- Supporting the old layout or extended skin tables.

## Decisions

### `ModelType` fields

| Field | Contents |
|---|---|
| `Version` | 12, 14 or 15 |
| `GlobalPriority` | 255 means per-face priorities |
| `HasAlpha`, `HasFaceSkins`, `HasVertexSkins`, `HasShadingTypes` | Presence switches |
| `HasTextureIds` | Presence switch |
| `UsesSmartBillboardGroups` | Flag 0x40 |
| `Positions` | Absolute x/y/z per vertex |
| `Indices` | Three per face |
| `StripCodes` | 1–4 per face |
| `FaceColors`, `FacePriorities`, `FaceAlpha`, `FaceSkins`, `VertexSkins`, `ShadingTypes` | Raw, with `null` when the switch is off |
| `TextureIds` | Raw stored value, 0 meaning none |
| `MappingPointers` | Raw stored value, 0 meaning none |
| `TextureMappings` | Type, the three raw shorts, and for complex mappings raw scales, rotation, direction, offsets, and box offsets |
| `Emitters`, `Effectors`, `Billboards` | Attachment value types with raw group values |

The interface is `IModelType` in Abstractions. The attachment and mapping value types (`ModelEmitterAttachment`, `ModelEffectorAttachment`, `ModelBillboardAttachment`, `ModelTextureMapping`) are record structs in Abstractions.

**Rejected alternative:** storing raw delta streams. That has the same byte identity but is unusable for callers. The survey shows the stored data is canonical, so absolute values are enough.

### Codec

`ModelTypeCodec` decodes streams in the order shown under Context, using a bounds-checked reader over the stream region.

**Encoding:**
- writes the same streams in the same order;
- computes each vertex flag bit as "delta is not zero";
- writes 1-byte smart values when the value fits;
- writes index deltas from the stored strip codes;
- computes the footer stream lengths;
- writes the version byte when the version is not 12;
- sets flags from the presence switches and attachment presence.

**Rejections:**
- An old-layout file (no FF FF footer) or flags 0x10/0x20 throw `NotSupportedException`.
- Truncated or overlapping streams throw `InvalidDataException`.

**Rejected alternative:** keeping the old layout. The cache has none, and its encoder couldn't be verified against real data.

### `ModelGeometryBuilder`

`ModelGeometryBuilder.Build(IModelType)` in `Logic/Rendering` returns `ModelGeometry`, today's `Geometry` record moved and renamed, with the same fields and values:

- vertex skin 255 → −1;
- texture IDs minus one;
- face texture vertices from mapping triangles or face indices;
- UVs through `ModelTextureCoordinates.Generate`, with the first-texel fallback always on;
- billboard group 255 → −1 when the groups are stored as bytes;
- projection positions and scales ×4 for version 12.

`ModelTextureCoordinates` moves unchanged to `Logic/Rendering`, apart from the `allowFirstTexelFallback` parameter, which is removed.

**Rejected alternative:** derived members on `ModelType`. UV generation is substantial rendering logic, and the user chose to keep render data out of Types.

### Callers

- `ModelEndpoints.ReadModel` and `BuildModel` read the container, decode with `codecs.Models`, and build geometry. `ModelCodecs` gains `ITypeCodec<IModelType> Models`.
- The attachment lists use the new value types.

### State transitions and ownership

This is read-only decode and encode. `ModelTypeCodec` owns the stored format, and `ModelGeometryBuilder` owns render derivation.

## Risks / Trade-offs

- **Flag 0x40 can't be checked against real data.** → A hand-built round-trip test covers it.
- **Builder parity with the old decoder.** → Before deleting the decoder, a temporary comparison over every cache model checks that the built geometry matches it. The existing projection tests move onto the builder.
- **Larger files decode differently.** Decoding no longer goes through the old-layout rearrangement. → The comparison and round-trip runs cover all 86,104 models.

## Migration Plan

This is an in-process refactor with no data migration. Rollback is a revert.
