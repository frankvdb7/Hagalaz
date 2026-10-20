## Why

Entering the world fails while the server constructs the character because the
cache archive decoder treats cumulative per-file chunk sizes as file-size
deltas. Valid negative footer deltas then become negative `MemoryStream`
capacities.

## What changes

- Decode archive footer entries using the revision-742 cumulative-size format.
- Preserve the existing cache, container, and archive ownership boundaries.
- Add regressions for negative footer deltas, multiple chunks, and extracted
  member contents.

## Impact

This affects cache archive member decoding used by item definitions during
GameWorld character construction and by other multi-file cache stores. It does
not change cache files, compression, protocol framing, or character state.

## Acceptance criteria

- Valid revision-742 archive metadata with negative footer deltas decodes
  without negative allocations.
- File sizes and contents remain correct across one and multiple chunks.
- Invalid archive metadata fails with a clear data-format exception.
- The focused cache tests, affected build, strict OpenSpec validation, and the
  real client world-entry check pass.

## Non-goals

- Do not regenerate or modify the pinned cache fixture.
- Do not add a fallback item definition or bypass cache decoding.
- Do not change GameWorld character construction or the reconnect protocol.
