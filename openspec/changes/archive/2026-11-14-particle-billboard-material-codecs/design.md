# Design

## Context

See proposal.md, sections "Why" and "Stored layout evidence". Current state:

- **The five static decoders** live in `Hagalaz.Cache/Logic/Codecs`:

  | Decoder | Stored in | Production callers |
  |---|---|---|
  | `ModelBillboardDecoder` | index 29, archive 0 | `ModelEndpoints` |
  | `ParticleEffectorDecoder` | index 27, archive 1 | `ModelEndpoints` |
  | `ParticleEmitterDecoder` | index 27, archive 0 | `ModelEndpoints` |
  | `RenderAnimationSetDecoder` | index 2, archive 32 | `ModelEndpoints` |
  | `MaterialDefinitionDecoder` | index 26, file 0, member 0 | `TextureEndpoints`, `ItemIcons` |

- **`TextureImageDecoder`** converts texture pixels to ARGB, HDR or PNG. Its callers are `TextureEndpoints` and `ItemIcons`.
- **Survey results** (every stored entry in the local cache):
  - Every opcode payload length matches the decoders.
  - No entry has trailing bytes or a missing terminator.
  - No unknown opcode occurs.
  - Big-smart values always use the 2-byte form.
- **Material table layout:** a `ushort` count (2,738), then one presence byte per texture (all present), then 20 columns with one value per present material:

  | Columns | Width | Contents |
  |---|---|---|
  | 0–17 | byte | Column 16 is the HDR flag. Columns 16 and 17 were previously misread as one `ushort` column. |
  | 18 | int | Unknown meaning; the decoder calls it `EffectParameter2`. |
  | 19 | byte | Alpha mode |

  The total length is 65,714 bytes, with nothing after the last column. The decoder ignores columns 0, 1, 2, 7, 8, 11, 12, 13 and 17.
- **Values derived on decode today:**
  - texture `65535` → `-1` (billboard, emitter);
  - billboard sizes stored minus one;
  - effector attenuation `0` → `1`;
  - emitter speeds `/262144*1000`, lifetimes `/1000`, rates `*1000/64`, sizes `*4`, angles `*8`;
  - emitter op 22 `-1` → "no speed curve", and op 18 `0` → "no colour curve";
  - render animation sequence IDs collected distinct, with negatives dropped.

## Goals / Non-Goals

**Goals:**
- Every stored value is a Type field.
- Today's endpoint values are reproduced by derivation members on each Type.

**Non-Goals:**
- Recording opcode order. The proposal settles canonical encoding for opcode formats.
- Interpreting values whose meaning is unknown. They are kept raw.

## Decisions

### One Type, interface and codec per definition

| Interface (Abstractions) | Type (`Hagalaz.Cache/Types`) | Codec (`Logic/Codecs`) |
|---|---|---|
| `IParticleEmitterType` | `ParticleEmitterType` | `ParticleEmitterTypeCodec` |
| `IParticleEffectorType` | `ParticleEffectorType` | `ParticleEffectorTypeCodec` |
| `IBillboardType` | `BillboardType` | `BillboardTypeCodec` |
| `IRenderAnimationType` | `RenderAnimationType` | `RenderAnimationTypeCodec` |
| `IMaterialTableType` | `MaterialTableType` | `MaterialTableTypeCodec` |

- **Base contract:** each interface derives from `IType`, and each codec implements `ITypeCodec<T>`.
- **IDs:** a Type's ID is its archive member ID. The material table's ID is 0.
- **Registration:** codecs are registered in `AddGameCache` the way the existing codecs are (`TryAddTransient<ITypeCodec<IBillboardType>, BillboardTypeCodec>()` and so on).
- **Rejected alternative:** keeping the records and adding encode methods to the static decoders. That leaves no Type to reuse and contradicts the agreed Type + Codec pattern.

### Fields hold stored values, with presence

- **Shape:** Types are mutable classes. Each opcode becomes a field holding its raw stored value or values.
- **Optional opcodes** use nullable fields: `null` means the opcode is absent. This keeps "absent" apart from "stored with the default value", for example effector attenuation stored as `1`.
- **Payload-less opcodes** become `bool` fields (for example billboard op 7 `HideFace`, emitter op 24).
- **Opcodes whose meaning is unknown** are kept as raw values named by opcode, so they survive encoding:
  - billboard ops 3 and 5;
  - effector op 2;
  - emitter ops 2, 12, 13, 14, 16, 17, 19, 25, 26, 29, 30, 32, 33 and 34;
  - render animation ops 26–37, 43–45 and 53–56.
- **Repeated opcodes** keep the last value, as the client does. Emitter op 24 repeats carry no payload, so they collapse to one flag.
- **Material table:** 18 byte columns, the int column 18 and the alpha byte, held as one row object per present material, with named properties where the meaning is known (`AlphaMode`, `ColorBlend`, `Brightness`, `EffectId`, `EffectParameter`, `ScrollU`, `ScrollV`, `HasHdr`, the column-18 int) and raw `Column0`…`Column17` properties for the rest. It also keeps the presence flags, so absent textures stay absent. `MaterialTableType.Find(textureId)` returns a row or `null`.

### Canonical encoding

- **Opcode formats:** the encoder writes present opcodes in ascending opcode order, followed by the `0` terminator. Ascending order is simple, deterministic, and every decoder accepts any order.
- **Material table:** written in its fixed column layout, so it is byte-identical.

**Rejected alternative:** recording each entry's stored opcode order. The user chose canonical encoding (proposal.md, "Stored layout evidence").

### Derivation members reproduce today's values

Each Type gets read-only members that compute what consumers show today, using the formulas listed under Context. For example:
- `BillboardType.TextureIdOrNone`, `HalfWidth` and `HalfHeight`;
- `ParticleEffectorType.EffectiveAttenuation`;
- `ParticleEmitterType.SpeedMin` and the other scaled values, `SpeedOverLife`, `ColorOverLife` and `UnsupportedOpcodes`;
- `RenderAnimationType.SequenceIds`, which keeps today's rule of distinct values with negatives dropped, in canonical order: op 1 first, then the others ascending. The user accepted this order change.

`ModelEndpoints` maps these to the existing DTOs unchanged.

- **`UnsupportedOpcodes`:** derived in ascending order without repeats, because the Type no longer records stored order. That only changes the order and duplicates of an informational list the admin shows as text, for 80 emitters with a repeated op 24 and entries whose stored order differs. The user accepted this.
- **Rejected alternative:** storing derived values as fields. That loses stored values, for example attenuation `0` versus `1`.

### `TextureImageConverter`

`TextureImageDecoder` is renamed `TextureImageConverter` and moved to `Hagalaz.Cache.Logic.Rendering`. Its body is unchanged.

`TextureEndpoints` and `ItemIcons` take the material alpha mode from `MaterialTableType.Find(id)`. The transparency rule stays in `TextureEndpoints`, since providers and deduplication are out of scope.

### Callers

- `ModelEndpoints` injects the emitter, effector, billboard and render-animation codecs.
- `TextureEndpoints` and `ItemIcons` inject `ITypeCodec<IMaterialTableType>`. `ItemIcons.TextureSource` receives the codec through its constructor.
- The cache reads (`ReadMember`, `cache.Read(26, 0, 0)`) stay where they are.
- Error handling is unchanged: decode failures throw `InvalidDataException`/`NotSupportedException` exactly as before, so the existing availability warnings stay.

### State transitions and ownership

This is read-only decode and encode, with no state transitions, retries or reconciliation. Each codec is the single owner of its stored format.

## Risks / Trade-offs

- **The unsupported-opcode text changes order and drops repeats.** → Informational only, and accepted by the user.
- **Unknown-meaning fields are raw and undocumented.** → Named by opcode or column, so nothing is guessed. They can be named later without a format change.
- **The real-cache check needs the local cache.** → A round-trip test over `Source/Cache` that skips when the cache directory is absent, plus fixture-based round-trip tests that always run.

## Migration Plan

This is an in-process refactor with no data migration. Rollback is a revert.
