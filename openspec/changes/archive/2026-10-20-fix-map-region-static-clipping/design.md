## Context

The cache location payload contains complete 64x64 local coordinates. `DecodePart` is intentionally chunk-oriented: it filters one plane and rotates coordinates relative to an 8x8 part. The GameWorld static-region loader currently calls that operation with a 64x64 range and plane 0, which mixes the two coordinate contracts.

## Goals / Non-Goals

**Goals:**

- Give complete-region loading a full-coordinate decode path.
- Reuse the existing cache readers and terrain/object codecs.
- Keep chunk rotation behavior unchanged for callers of `DecodePart`.

**Non-Goals:**

- Redesign collision ownership or asynchronous region scheduling.
- Alter custom database object loading.

## Decisions

Add a distinct `DecodeRegion` operation to `IMapProvider`. It will decode the already-authoritative terrain and object streams once, report all effective terrain flags, and report every decoded object with its complete local coordinates and effective plane. `MapRegionLoader` remains the owner of turning those callbacks into static `IGameObject` instances and region collision.

The existing `DecodePart` operation remains unchanged for chunk-oriented consumers. Reusing its implementation for full-region loading would require a mode flag or sentinel plane and would preserve the coordinate ambiguity that caused this defect.

## Risks / Trade-offs

- [Risk] The new operation adds a small interface surface. → It has one production caller, a distinct complete-region contract, and avoids changing the established chunk contract.
- [Risk] Cache payload decoding can still fail before callbacks are produced. → Existing cache readers retain their current error logging; cache-read failure handling is outside this change.
