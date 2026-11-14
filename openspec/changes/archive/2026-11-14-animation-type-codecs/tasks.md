# Tasks

## 1. Types and codecs

- [x] 1.1 Add the `IAnimationSkeletonType`/`AnimationSkeletonType` and `IAnimationFrameType`/`AnimationFrameType` field models from design.md. Verify that `dotnet build Hagalaz.Cache` succeeds.
- [x] 1.2 Implement both codecs with canonical smart values, raw flag bytes, and trailing-data and truncation rejection, and register them in `AddGameCache`.

  Verify with codec tests:
  - hand-built skeletons and frames round-trip, including a tween-only slot, an axis bit with value 0, a 2-byte smart, and fewer slots than the skeleton;
  - truncated and trailing data are rejected;
  - a registration test resolves both codecs.

## 2. Builder

- [x] 2.1 Add `AnimationFrameBuilder` with `ResolveSkeleton` and `Resolve` and the `AnimationSkeleton`/`AnimationFrame`/`AnimationTransform` records in `Logic/Rendering`. Verify by moving `AnimationFrameDecoderTests`, including the real demon fixture against the game client, onto codec + builder, and by keeping the oversized-frame rejection.
- [x] 2.2 Before deleting the decoder, run a temporary comparison over every frame in the local cache. Verify that builder output equals `AnimationFrameDecoder` output, failures included.

## 3. Callers and removal

- [x] 3.1 Add both codecs to `ModelCodecs` and move `GetAnimationFrame` onto the codecs and builder. Verify that the existing `ModelAnimationDtoTests` pass unchanged.
- [x] 3.2 Delete `AnimationFrameDecoder` with Rider Safe Delete. Verify that Rider finds no references and that `dotnet build Hagalaz.sln` succeeds.

## 4. Integration check

- [x] 4.1 Add skeletons and frames to `RealCacheCodecRoundTripTests` with byte-identical checks, run `Hagalaz.Cache.Tests` and `Hagalaz.Services.Cache.Tests` through Rider, and confirm proposal acceptance criteria 1–6.

## Workflow follow-up

- Sync the `cache-type-codecs` delta and archive the change after review.
