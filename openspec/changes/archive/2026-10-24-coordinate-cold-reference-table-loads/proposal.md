# Coordinate cold reference-table loads

## Why

`ReferenceTableProvider` memoizes decoded tables with an unsynchronized `??=`. A controlled local-cache profile of 20 synchronized 16-reader bursts for index 16 observed 16 physical index-255 reads per cold burst. The cold burst averaged 44.5 ms while the same requests with the table warm averaged 0.175 ms. Concurrent first reads therefore repeat material storage and decode work.

## What Changes

- Coordinate initialization and invalidation for each reference-table index inside the existing `ReferenceTableProvider`.
- Preserve retry after a failed decode and allow independent indices to initialize concurrently.

## Capabilities

### Modified Capabilities

- `cache-reference-tables`: concurrent cold reads for one index share its published decoded table, and invalidation waits for active initialization before clearing it.

## Impact

Changes are limited to the existing reference-table provider and its unit tests. No public API or table format changes.

## Non-goals

- No generic cache, event bus, global codec lock, or synchronization around definition decoding.
- No changes to archive snapshots, range reads, or cross-process mutation behavior.

## Acceptance Criteria

- Concurrent cold reads for the same index perform one reference-table file read and receive the same published table instance.
- Invalidation that overlaps initialization waits for that initialization, clears its result, and allows the next read to load a fresh table.
- Different index IDs use independent synchronization.
- Focused provider tests and the local cold/warm profile pass.
