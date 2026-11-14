# Tasks

## 1. Particle effector and billboard

- [x] 1.1 Add `IParticleEffectorType`/`ParticleEffectorType` and `ParticleEffectorTypeCodec`:
  - every opcode is a nullable or flag field, and op 2 is kept raw;
  - encoding is canonical ascending order;
  - derivation members reproduce today's effector values.

  Verify with codec tests: round-trip, attenuation `0` preserved while the effective value is 1, truncated entry rejected, unknown opcode rejected.
- [x] 1.2 Add `IBillboardType`/`BillboardType` and `BillboardTypeCodec`:
  - ops 3 and 5 are kept raw;
  - texture `65535` and sizes are stored raw, and derivation members give today's values.

  Verify by moving the billboard cases from `ModelGeometryDecoderTests` onto the codec and adding round-trip tests.

## 2. Particle emitter

- [x] 2.1 Add `IParticleEmitterType`/`ParticleEmitterType` and `ParticleEmitterTypeCodec`:
  - every opcode's payload is stored raw, including ops 2, 12–14, 16, 17, 19, 25, 26, 29, 30 and 32–34;
  - op 24 is a flag;
  - derivation members give today's scaled values, curves, effector IDs and `UnsupportedOpcodes` (ascending, distinct).

  Verify by moving `ModelParticleTests` onto the codec and adding round-trip tests, including the op 22 `-1` and repeated op 24 cases.

## 3. Render animation set

- [x] 3.1 Add `IRenderAnimationType`/`RenderAnimationType` and `RenderAnimationTypeCodec`:
  - every opcode the current decoder knows is a field, with the last value kept on repeats;
  - `SequenceIds` keeps today's rule (distinct, negatives dropped).

  Verify by moving `RenderAnimationSetDecoderTests` onto the codec and adding a test for a repeated opcode with different values.

## 4. Material table and texture conversion

- [x] 4.1 Add `IMaterialTableType`/`MaterialTableType` and `MaterialTableTypeCodec`:
  - presence flags plus all 20 columns, with named and raw row properties;
  - `Find(textureId)` returns a row or `null`;
  - the encoding is byte-identical.

  Verify by moving `MaterialDefinitionDecoderTests` onto the codec and adding a byte-identical round-trip test.
- [x] 4.2 Rename `TextureImageDecoder` to `TextureImageConverter` in `Hagalaz.Cache.Logic.Rendering` and rename its tests. Verify that `TextureImageConverterTests` pass unchanged.

## 5. Registration, callers and removal

- [x] 5.1 Register the five codecs in `AddGameCache`. Verify with a test resolving all five from a provider built by `AddGameCache`.
- [x] 5.2 Move `ModelEndpoints`, `TextureEndpoints` and `ItemIcons` onto the injected codecs and the Type derivation members. Verify that the existing model, particle, texture, material and item-icon endpoint tests pass unchanged.
- [x] 5.3 Delete the five static decoders. Verify by grepping that no references remain and that `dotnet build Hagalaz.sln` succeeds.

## 6. Integration check

- [x] 6.1 Add a round-trip test over every stored entry of the five definitions in the local cache (`Source/Cache`, skipped when absent): re-decoded values must match, encode → decode → encode must be byte-stable, and the material table must be byte-identical. Run it against the local cache, run the `Hagalaz.Cache.Tests` and `Hagalaz.Services.Cache.Tests` suites, and confirm proposal acceptance criteria 1–5.

## Workflow follow-up

- Sync the `cache-type-codecs` spec and archive the change after review.
- Follow-up changes: model types and codecs, then animation skeleton/frame types and codecs.
