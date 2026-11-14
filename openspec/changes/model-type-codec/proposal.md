# Proposal

## Why

Models are the largest definition still read by a static decoder. `ModelGeometryDecoder` returns a render-ready `Geometry` record, which contains several kinds of derived data:

- resolved positions (with version-12 projection positions scaled ×4);
- texture IDs minus one;
- generated texture UVs and first-texel faces;
- remapped skin and billboard values.

It cannot encode, and nothing describes the stored model format as a Type. The existing `Hagalaz.Cache/Types/ModelType` has no usages.

This is change 2 of the Type + Codec series (see `cache-type-codecs`).

**Goal:** model files (cache index 7) become a lossless `ModelType` with a registered codec whose encoding is byte-identical for every stored model, while model responses stay unchanged.

## What Changes

- **Type:** `IModelType` in `Hagalaz.Cache.Abstractions` and a mutable `ModelType` in `Hagalaz.Cache/Types`, replacing the unused class. It holds the stored values:
  - absolute vertex positions;
  - triangle indices with the per-face strip code;
  - raw face colours, priorities, alpha, face and vertex skins, and shading types;
  - texture IDs as stored (0 meaning none);
  - raw texture mapping parameters;
  - emitter, effector and billboard attachments;
  - the header switches: version, global priority, and the alpha, face-skin, vertex-skin and shading presence flags.

  Per-axis vertex flag bits and stream lengths are derived when encoding.
- **Codec:** `ModelTypeCodec : ITypeCodec<IModelType>`, registered in `AddGameCache`. `Encode(Decode(stored))` is byte-identical for every model in the cache, with no canonical fallback.
- **Layouts:** the newer layout (FF FF footer) is supported, including flag 0x40 (billboard groups as two-byte values).
  - The old layout is rejected as not supported.
  - Flags 0x10/0x20 (extended skin tables) stay rejected.
- **Render data:** `ModelGeometryBuilder` in `Hagalaz.Cache/Logic/Rendering` builds today's geometry from an `IModelType`, including version-12 projection scaling, texture IDs minus one, UV generation, and first-texel faces with the fallback always on. `ModelTextureCoordinates` moves to `Logic/Rendering`. The attachment records become value types in Abstractions.
- **Callers:** `ModelEndpoints` decodes models with the injected codec and builds geometry with the builder.
- **Removals:** `ModelGeometryDecoder` and the `allowFirstTexelFallback` flag.

**Reused:**
- `ITypeCodec<T>` and the `AddGameCache` registration;
- the `DefinitionReader`/`DefinitionWriter` helpers;
- the `ModelCodecs` bundle in `Hagalaz.Services.Cache`;
- the existing UV generation logic.

### Stored layout evidence

A scan of all 86,104 model files in the local cache found:

| Finding | Detail |
|---|---|
| Layout | All use the newer layout. None use the old layout, and none set 0x10, 0x20 or 0x40. |
| Versions | 12 (no version byte), 14 or 15 |
| Vertex flags | No axis bit set with a zero delta, and no bits outside 1/2/4 |
| Smart values | No value stored in an oversized smart form |
| Stream lengths | All exact |
| Padding | None |
| Strip codes | Only 1 to 4. Storing them avoids ambiguity on 3,557 degenerate faces. |

Presence switches and the global priority can't be derived from content, so they are stored.

### Non-goals

- Animation skeletons and frames (change 3).
- A model provider, write routes, or `TypeMutator` registration.
- Old-layout models and extended skin tables.
- Any change to model assembly, rendering, or JSON contracts.

### Acceptance criteria

1. `IModelType`, `ModelType` and a registered `ITypeCodec<IModelType>` exist, and the unused `ModelType` is replaced.
2. `Encode(Decode(stored))` is byte-identical for every model in the local game cache.
3. Model, item, NPC and object model responses, model images and item icons return the same values for the same cache.
4. Old-layout and extended-skin models fail to decode with a not-supported error. Flag 0x40 billboards round-trip.
5. `ModelGeometryDecoder` no longer exists. `ModelTextureCoordinates` and the geometry builder live in `Logic/Rendering`.
6. Existing tests pass. Old-layout-only tests are removed, and tests that expected the fallback to be off assert the fallback's output instead.

### Stop conditions

- **A stored model can't round-trip.** If any stored model cannot round-trip byte for byte from the agreed fields, stop and raise it instead of adding a fallback.
- **A model response would change.** If keeping a response value needs anything beyond deriving it from `ModelType`, stop and raise it.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `cache-type-codecs`: adds models as a definition type with byte-identical encoding and the supported layouts.

## Impact

- **`Hagalaz.Cache.Abstractions`:** `IModelType` and the attachment value types.
- **`Hagalaz.Cache`:**
  - `ModelType` replaced;
  - `ModelTypeCodec` added;
  - `ModelGeometryBuilder` added, and `ModelTextureCoordinates` moved;
  - `ModelGeometryDecoder` deleted;
  - `AddGameCache` updated.
- **`Hagalaz.Services.Cache`:** `ModelEndpoints` and `ModelCodecs` gain the model codec.
- **Tests:** `ModelGeometryDecoderTests`, `ModelParticleTests` and `ModelParticleEndpointsTests` move onto the codec and builder, and the real-cache round-trip test gains models.
