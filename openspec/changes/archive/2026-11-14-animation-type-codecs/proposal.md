# Proposal

## Why

Animation skeletons and frames are the last definitions still read by a static decoder. `AnimationFrameDecoder` returns derived records:

- skeleton type 6 is mapped to 2, and enabled bytes become booleans;
- frames get per-type defaults, rotation values ×4, implied pivots and tween flags;
- the frame header byte is dropped;
- slots with no flags are dropped.

It cannot encode, and decoding a frame requires its skeleton.

This is change 3 of the Type + Codec series (see `cache-type-codecs`).

**Goal:** skeletons (cache index 1) and frames (cache index 0) become lossless Types with registered codecs whose encodings are byte-identical for every stored entry, while animation frame responses stay unchanged.

## What Changes

- **Skeleton type:** `IAnimationSkeletonType`/`AnimationSkeletonType` with raw type bytes, raw enabled bytes, masks and groups. `ITypeCodec<IAnimationSkeletonType>` is registered in `AddGameCache`.
- **Frame type:** `IAnimationFrameType`/`AnimationFrameType` with:
  - the header byte;
  - the skeleton ID;
  - one raw flag byte per stored slot;
  - the raw x/y/z values for each set axis bit.

  `ITypeCodec<IAnimationFrameType>` is registered in `AddGameCache`. The frame ID is the frame set (archive) shifted left 16 bits, plus the member ID.
- **Frame codec:** it does not need the skeleton (decided with the user, replacing the earlier plan for a frame decode that takes it).
- **Encoding rule:** both codecs re-encode every stored entry byte for byte, with no fallback.
- **`AnimationFrameBuilder`** in `Hagalaz.Cache/Logic/Rendering` derives what `GetAnimationFrame` returns today:
  - `ResolveSkeleton` maps type 6 → 2 and enabled bytes to booleans;
  - `Resolve(frame, skeleton)` applies per-type defaults, rotation scaling, pivots and tween flags, skipping zero-flag slots;
  - it rejects frames with more slots than their skeleton.
- **Callers:** `ModelEndpoints.GetAnimationFrame` decodes through the codecs (added to `ModelCodecs`) and the builder.
- **Removal:** `AnimationFrameDecoder`, deleted with Rider Safe Delete.

**Reused:**
- `ITypeCodec<T>` and the `AddGameCache` registration;
- the `DefinitionReader`/`DefinitionWriter` helpers;
- the `ModelCodecs` bundle;
- the real-cache round-trip test.

### Stored layout evidence

A scan of every skeleton (3,669) and frame (533,611 in 4,257 frame sets) in the local cache found:

- **Both representations re-encode byte for byte:**
  - frames: header byte, skeleton ID, per-slot flag byte, raw values for set axis bits;
  - skeletons: raw types, raw enabled bytes, masks, groups.
- **The data is clean:**
  - no oversized smart values;
  - no trailing bytes;
  - no flag bits ≥ 0x20;
  - no frame with more slots than its skeleton;
  - every referenced skeleton exists.
- **Flag bytes must be stored, not derived:** 1.49M axis bits are set on zero values, and 453k slots carry tween bits without axis bits.
- **Frames store fewer slots than their skeleton** in 80% of cases, so the stored slot count is kept.
- **Raw values with no variation:** the header byte is always 1, type 6 never occurs, and enabled bytes are only 0/1. They are still stored raw.

### Non-goals

- Animation sequences (`AnimationType`), providers, write routes, or `TypeMutator` registration.
- Any change to animation playback or to the frame, transform and skeleton DTOs.

### Acceptance criteria

1. Skeleton and frame Types and their registered codecs exist.
2. Every skeleton and every frame in the local game cache re-encodes byte for byte.
3. `GET /models/animations/frames/{id}` returns the same values for the same cache.
4. Truncated data, trailing data, and frames with more slots than their skeleton are rejected as before.
5. `AnimationFrameDecoder` no longer exists, and `AnimationFrameBuilder` lives in `Logic/Rendering`.
6. Existing tests pass, including the real demon skeleton and frame fixture compared against the game client.

### Stop conditions

- **A stored entry can't round-trip.** If any stored skeleton or frame cannot round-trip byte for byte, stop and raise it.
- **A response would change.** If keeping a response value needs anything beyond deriving it from the Types, stop and raise it.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `cache-type-codecs`: adds animation skeletons and frames as definition types with byte-identical encoding.

## Impact

- **`Hagalaz.Cache.Abstractions`:** two interfaces.
- **`Hagalaz.Cache`:**
  - two Types and two codecs;
  - `AnimationFrameBuilder`;
  - `AnimationFrameDecoder` deleted;
  - `AddGameCache` updated.
- **`Hagalaz.Services.Cache`:** `ModelEndpoints.GetAnimationFrame` and `ModelCodecs`.
- **Tests:** `AnimationFrameDecoderTests` moves onto the codecs and builder, and the real-cache round trip gains skeletons and frames.
