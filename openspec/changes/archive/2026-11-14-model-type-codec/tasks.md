# Tasks

## 1. Model type and codec

- [x] 1.1 Add `IModelType` and its attachment and mapping value types to Abstractions, and replace the unused `ModelType` with the field model from design.md. Verify that `dotnet build Hagalaz.Cache` succeeds and that Rider shows no usages of the old members.
- [x] 1.2 Implement `ModelTypeCodec` decode and encode for the newer layout, including flag 0x40; reject the old layout and flags 0x10/0x20.

  Verify with codec tests:
  - hand-built models round-trip (uniform switches, mappings without texture IDs, two-byte billboard groups, every strip code, version 12/14/15 scale widths);
  - an old-layout model is rejected;
  - an extended-skin model is rejected;
  - a truncated model is rejected.

## 2. Geometry builder

- [x] 2.1 Move `ModelTextureCoordinates` to `Logic/Rendering` without the `allowFirstTexelFallback` parameter. Add `ModelGeometryBuilder.Build(IModelType)` returning `ModelGeometry` with today's derivations. Verify by moving the `ModelGeometryDecoderTests` projection, priority, shading, skin and attachment cases onto codec + builder, with the fallback-off cases asserting the fallback's output.
- [x] 2.2 Before deleting the decoder, run a temporary comparison over every model in the local cache. Verify that `ModelGeometryBuilder.Build(codec.Decode(…))` equals `ModelGeometryDecoder.Decode(…, allowFirstTexelFallback: true)` for every field.

## 3. Callers and removal

- [x] 3.1 Register the codec in `AddGameCache` and add `Models` to `ModelCodecs`. Move `ModelEndpoints.ReadModel`/`BuildModel` onto the codec and builder. Verify that the existing model, particle, appearance, image and item-icon endpoint tests pass unchanged, and that a test resolves `ITypeCodec<IModelType>`.
- [x] 3.2 Delete `ModelGeometryDecoder` with Rider Safe Delete, remove the old-layout-only tests, and move `ModelParticleTests`/`ModelParticleEndpointsTests` onto codec + builder. Verify that Rider finds no references and that `dotnet build Hagalaz.sln` succeeds.

## 4. Integration check

- [x] 4.1 Add models to `RealCacheCodecRoundTripTests` with a byte-identical check over every model, run `Hagalaz.Cache.Tests` and `Hagalaz.Services.Cache.Tests` through Rider, and confirm proposal acceptance criteria 1–6.

## Workflow follow-up

- Sync the `cache-type-codecs` delta and archive the change after review.
- Follow-up change: animation skeleton/frame types and codecs.
