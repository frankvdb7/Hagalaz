# Proposal

## Why

Item, NPC, object and the other classic definitions are modelled as a Type (`I…Type` plus a mutable `…Type` class) with a Codec (`ITypeCodec<T>`: decode and encode). The definitions behind model particles, billboards, render animations and materials are not. They are read by static `…Decoder` classes in `Hagalaz.Cache/Logic/Codecs`:

- The decoders return immutable records named `Definition` or `Material`.
- They mix stored values with derived ones (scaled speeds and lifetimes, `65535` → `-1`, attenuation `0` → `1`).
- They drop the opcodes and columns they do not need.
- None of them can encode.

These definitions can therefore not be reused like `ItemType`, cannot be written back, and have no single place where their stored format is described.

**Goal:** model particle emitters, particle effectors, model billboards, render animation sets and the material table as Types with lossless Codecs registered in `AddGameCache`. Existing Cache API responses stay unchanged.

This is the first of three changes; models and animation skeletons/frames follow separately.

## What Changes

- **Types:** for each of particle emitter, particle effector, billboard, render animation set and material table, add an `I…Type` interface in `Hagalaz.Cache.Abstractions` and a mutable `…Type` class in `Hagalaz.Cache/Types` holding exactly the stored fields:
  - every opcode or column, including those the current decoders skip or ignore;
  - raw stored values, not scaled or remapped ones.
- **Codecs:** add an `ITypeCodec<T>` for each one in `Hagalaz.Cache/Logic/Codecs`, with real `Decode` and `Encode`, registered in `AddGameCache`.
  - **Material table:** the codec covers the whole table and must reproduce the stored table byte for byte. The Type offers a lookup of one material by texture ID.
  - **Opcode formats** (emitter, effector, billboard, render animation): the codec keeps every stored value, and encode → decode → encode must be byte-stable. Opcode order is normalized; see "Stored layout evidence" below.
- **Derived values** that endpoints show today (scaled emitter speeds, rates, lifetimes and sizes, the unsupported-opcode list, normalized attenuation, billboard half sizes, render-animation sequence IDs, material fields) move to derivation members beside each Type. Every value in today's JSON responses stays the same.
- **Callers move onto the codecs:** `ModelEndpoints`, `TextureEndpoints` and `ItemIcons` take them from DI.
- **Removals:** delete `ParticleEmitterDecoder`, `ParticleEffectorDecoder`, `ModelBillboardDecoder`, `RenderAnimationSetDecoder` and `MaterialDefinitionDecoder`, with no wrappers left behind.
- **Rename:** `TextureImageDecoder` becomes `TextureImageConverter` and moves from `Logic/Codecs` to `Logic/Rendering`. It converts texture pixels, so it is not a codec. Its behaviour is unchanged.

**Reused, not added:**
- `ITypeCodec<T>`, `IType` and the `AddGameCache` registration pattern;
- the existing stream extensions (`ReadBigSmart` and friends);
- the cache read paths the endpoints already use.

No new dependency or provider.

### Stored layout evidence

A survey of every stored entry in the local game cache shows that the four opcode formats can't round-trip byte for byte without recording each entry's opcode order:

| Format | Entries | Entries not in ascending opcode order | Repeated opcodes |
|---|---|---|---|
| Billboards | 406 | all | none |
| Effectors | 19 | all | none |
| Emitters | 900 | all, in 142 different orders | 80 entries |
| Render animation sets | 2,687 | 929 | 28 entries, some with different values per repeat |

The client keeps the last value of a repeated opcode. Following the agreed fallback, these four formats use a canonical encoding like `ItemType`: every stored value is preserved, the opcode order is normalized, and a repeated opcode keeps its last value. The material table has fixed columns and nothing after them, so it stays byte-identical.

### Non-goals

- Model geometry (`ModelGeometryDecoder`, `ModelType`) and animation skeletons/frames. These are follow-up changes 2 and 3.
- Providers that remove cache index IDs from endpoints.
- Write routes or `TypeMutator` registrations for these types.
- Changes to emitter simulation, billboard or material rendering, or any JSON contract.

### Acceptance criteria

1. Each of the five definitions has an `I…Type` interface, a mutable `…Type` class and a registered `ITypeCodec<T>`.
2. On every entry of the local game cache:
   - for all five codecs, decoding `Encode(Decode(stored))` gives the same values as decoding the stored bytes, and encode → decode → encode is byte-stable;
   - the material table's `Encode(Decode(stored))` is byte-identical to the stored table.
3. All Cache API responses that expose these definitions return the same JSON for the same cache, including warnings for unavailable definitions:
   - model particles, billboards and effectors;
   - NPC render-animation sequences;
   - material, HDR and texture images;
   - item icons.

   Two exceptions follow from no longer recording stored opcode order, and the user accepted both:
   - An emitter's `unsupportedOpcodes` list is ascending without repeats.
   - An NPC's render-animation sequence IDs follow canonical opcode order: op 1 (stand, then walk) first, then the other opcodes ascending. For an NPC without its own idle animation, the default preview animation can become the stand sequence.
4. The five static decoders no longer exist. `TextureImageConverter` lives in `Logic/Rendering`.
5. Existing tests pass, and each codec has round-trip tests.

### Stop conditions

- **Stored values are lost.** If any stored value of an opcode format can't be preserved through decode and encode (for example an opcode whose payload meaning or length is unknown), stop and raise it rather than dropping it.
- **Preserving a response needs more than derivation.** If keeping any current JSON value requires behaviour beyond deriving it from the stored fields, stop and raise it instead of changing the response.

## Capabilities

### New Capabilities

- `cache-type-codecs`: lossless decode and encode of cache definition types, starting with particle emitters, particle effectors, model billboards, render animation sets and the material table.

### Modified Capabilities

None. Model, particle, texture and item-icon responses keep their current requirements.

## Impact

- **`Hagalaz.Cache.Abstractions`:** five `I…Type` interfaces.
- **`Hagalaz.Cache`:**
  - five Types and five Codecs;
  - `AddGameCache` registrations;
  - five decoders deleted;
  - `TextureImageDecoder` moved and renamed to `TextureImageConverter`.
- **`Hagalaz.Services.Cache`:** `ModelEndpoints`, `TextureEndpoints` and `ItemIcons` inject the codecs.
- **Tests:** the decoder tests move to codec tests (`MaterialDefinitionDecoderTests`, `ModelParticleTests`, `RenderAnimationSetDecoderTests`, `TextureImageDecoderTests`, the billboard cases in `ModelGeometryDecoderTests`), plus round-trip tests.
- **Verification:** byte-identical round trips are checked against the local game cache at `Source/Cache`.
