# Design

## Context

See proposal.md, sections "Why" and "Stored layout evidence".

- **Production caller:** only `ModelEndpoints.GetAnimationFrame`.
  1. It reads the frame member (index 0, archive = ID >> 16, member = ID & 65535).
  2. It reads skeleton `bytes[1..2]` from index 1.
  3. It calls `DecodeSkeleton` and `DecodeFrame`, then maps the results to `GetAnimationFrameDto`.
- **Tests:** `AnimationFrameDecoderTests` includes a real demon skeleton and frame fixture with values captured from the game client.

### Stored layout

**Skeleton:**

| Element | Encoding |
|---|---|
| Slot count | ushort |
| Type per slot | byte |
| Enabled per slot | byte |
| Mask per slot | ushort |
| Group length per slot | unsigned smart |
| Group entries | unsigned smarts |

**Frame:**

| Element | Encoding |
|---|---|
| Header | byte |
| Skeleton ID | ushort |
| Slot count | ushort |
| Flags per slot | byte |
| Values | signed smart for each set axis bit (1/2/4), in slot order and x, y, z order |

Unsigned smart: a 1-byte value when below 128, otherwise a 2-byte value with the top bit set. Signed smart: a 1-byte value when within −64..63, otherwise 2 bytes.

## Goals / Non-Goals

**Goals:**
- Raw field Types with byte-identical codecs.
- Resolution moved to a rendering builder without changing any value.

**Non-Goals:**
- Storing the skeleton ID once per frame set. Per-frame storage is lossless and simpler.

## Decisions

### Types

**`AnimationSkeletonType`:**

| Field | Contents |
|---|---|
| `Types` | Raw bytes |
| `Enabled` | Raw bytes |
| `Masks` | Ushorts |
| `Groups` | One array of entries per slot |

**`AnimationFrameType`:**

| Field | Contents |
|---|---|
| `Header` | Byte |
| `SkeletonId` | Ushort |
| `SlotFlags` | One raw byte per stored slot |
| `SlotValues` | Per slot, nullable x/y/z holding a value only where the axis bit is set |

Both Types are mutable classes with interfaces in Abstractions, like the earlier Types.

**Rejected alternative:** deriving flag bytes from the values. Axis bits occur on zero values, and tween-only slots exist, so the bytes would differ.

### Codecs

- **Decoding:** reads the layouts above with the bounds-checked reader and rejects trailing data.
- **Encoding:**
  - writes canonical smart values;
  - writes the flag byte as stored, and the values for its axis bits;
  - throws if a slot's flag bits and values disagree.

### `AnimationFrameBuilder`

- **`ResolveSkeleton(IAnimationSkeletonType)`** returns `AnimationSkeleton(Types, Enabled, Masks, Groups)`, with types 6 → 2 and enabled == 1 → true.
- **`Resolve(IAnimationFrameType, AnimationSkeleton)`** returns `AnimationFrame(SkeletonId, AnimationTransform[])` using today's rules:
  - a frame with more slots than its skeleton throws `InvalidDataException`;
  - zero-flag slots are skipped;
  - absent axes default to 128 for types 3 and 10, otherwise 0;
  - types 2 and 9 are scaled with `(v << 2) & 16383`;
  - pivots are assigned for types 1, 2 and 3 when the last pivot slot is newer than the last used one;
  - the tween value is bits 3–4.

These records replace the old nested decoder records and live in `Logic/Rendering` with the builder.

**Rejected alternative:** the frame decode that takes the skeleton, from the earlier plan. Raw parsing doesn't need it, and the user chose the plain codec.

### Callers

`GetAnimationFrame` uses:
- `codecs.AnimationFrames.Decode(id, member)`;
- `codecs.AnimationSkeletons.Decode(frame.SkeletonId, skeletonContainer.Data)`;
- the builder, with the same DTO mapping as before.

`ModelCodecs` gains both codecs. The skeleton ID now comes from the decoded frame instead of `bytes[1..2]`.

### State transitions and ownership

This is read-only decode and encode. The codecs own the stored formats, and the builder owns resolution.

## Risks / Trade-offs

- **Resolution parity.** → A temporary comparison over every frame in the cache, against the old decoder, runs before the decoder is deleted. The demon fixture test stays.
- **Running time.** A full round trip over 533,611 frames is slower than the earlier checks. → It runs only with the local cache, like the other real-cache tests.

## Migration Plan

This is an in-process refactor with no data migration. Rollback is a revert.
