# Design

## Context

`CacheApi.Read(indexId, archiveId, memberIndex)` currently decodes a new mutable `IArchive` for every member request. `ArchiveDecoder` already owns revision-742 validation and reconstruction, `CacheOptions` is bound through `IOptions<CacheOptions>`, and `CacheApi` is the production owner of `ICacheWriter`. Single-definition provider calls use `ReadArchive`; range paths often keep a mutable archive for sequential reads. See `proposal.md` and the capability delta for motivation and behavior.

## Goals / Non-Goals

**Goals:**

- Reuse the existing decoder to construct an immutable, packed snapshot once per cold archive load.
- Bound retained snapshot weight and share only currently active cold loads.
- Keep member reads concurrent while preventing a local write from overlapping snapshot loading or using a stale retained entry afterward.
- Preserve per-call definition decoding and mutable archive ownership.

**Non-Goals:**

- Async I/O, FileStore synchronization changes, cross-process coherence, definition caching, and changes to provider range algorithms.
- Making the multi-step on-disk write transactional. `CacheWriter` retains its current write order and failure semantics.

## Decisions

### Snapshot ownership and packing

`CacheApi` owns one internal `ArchiveSnapshotCache`; that cache retains `ArchiveSnapshot` values keyed by `(indexId, archiveId)`. A snapshot contains a private packed byte array and one offset/length descriptor per physical member. Construction calls the existing `ReadContainer` and `IArchiveDecoder.Decode`, copies the complete member contents into their final packed positions using checked offsets, and disposes the temporary archive and container. It may copy from valid `MemoryStream.TryGetBuffer` segments, but never assumes a stream's current position describes its full contents.

Each read creates a new non-writable `MemoryStream` over the selected packed slice. No array, archive, or mutable stream escapes. Existing streams remain valid after eviction because they retain the backing array. `ReadArchive` stays on its existing decode path and returns a separate mutable archive.

### Retention and active loads

The snapshot cache uses a dictionary plus linked-list LRU and a byte total, all protected by one ordinary monitor. Accounting includes packed bytes, two `int` values per descriptor, and a fixed 128-byte per-entry allowance; this is a conservative retention budget, not a managed-heap measurement. Capacity is read from `IOptions<CacheOptions>` and defaults to 64 MiB. Negative capacity is rejected at construction, zero retains no completed snapshots, and oversized snapshots are served only to attached callers.

The same monitor protects a temporary flight dictionary. Each flight contains one `Lazy<ArchiveSnapshot>` configured with `ExecutionAndPublication` and a participant count. The monitor is released before archive I/O, decompression, decode, packing, stream creation, or metric callbacks. Successful fitting snapshots publish once and leave flight lookup. Successful unretained snapshots stay discoverable only while attached callers still own the flight; failures remove only that exact flight so a later request can retry. Each participant releases its count after opening its requested stream.

### Read/write ordering

`CacheApi` owns one `ReaderWriterLockSlim` with `NoRecursion`. A member read enters the read lock before reference-table/archive/member validation and keeps it through snapshot lookup, any cold reconstruction, and opening the requested stream. It releases the lock before the provider codec or hook runs. Multiple reads can hold the lock concurrently; different cold archives therefore reconstruct independently.

`CacheApi.Write` enters the write lock before calling `_cacheWriter.Write`. In `finally`, while still exclusive, it invalidates only `(indexId, fileId)`, whether the writer succeeded or failed. This blocks unrelated member reads during a local write by design. The production lock order is always `ReaderWriterLockSlim` then snapshot-cache monitor; no path acquires them in reverse order. The lock is synchronous and is never held across `await`.

### Provider use and metrics

Generic and specialized single-definition `Get` methods resolve the logical child in the existing reference table and pass `entry.Index` to the three-argument `ICacheAPI.Read`, disposing the returned stream after synchronous decode. Custom delegation, factory fallback, and hook calls remain at their current provider boundaries. `GetRange` and `GetAll` remain unchanged.

The existing archive-load instruments record actual `ReadArchive` decodes and snapshot cold reconstructions; the snapshot loader records only once inside the shared `Lazy`. Small low-cardinality counters report snapshot hits/misses, evictions, and retained-byte deltas. Measurement callbacks run outside the snapshot-cache monitor. Container decode timing remains owned by the existing container decoder instrumentation.

### Alternatives rejected

- Caching `IArchive` or its member streams would share mutable position/state and would not provide independent stream ownership.
- Caching decoded definitions would allow one caller's mutation or GameWorld database override to affect another caller.
- A retained cache without active-flight sharing allows concurrent different-member misses to decompress the same archive repeatedly.
- Per-key locks, generations, epochs, TTLs, and distributed invalidation add state without being needed: the read/write lock prevents local writes from overlapping any active snapshot flight.
- Moving range reads to repeated single-member calls could add lookup/stream overhead to sequential decoding and is not required for the measured hot path.

## Controlled A/B sample

A Release-mode sample used the local revision-742 cache and the production `GameObjectService`, definition codec, provider, and `CacheApi` path. It resolved 19,967 unique valid object IDs spread across 260 populated index-16 archives. The override repository returned an empty result, so this measured the cache-resolution path without a live database or full hosted service. The same cache files and process were used for each phase; “cold” means the snapshot cache had not yet seen the workload.

| Phase | Bulk resolution | Archive loads | Snapshot hits / misses | Archive-load time | Container-decode time | Allocated bytes | Retained bytes added | Evictions |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Existing `ReadArchive` path, cold | 4,251.56 ms | 19,967 | n/a | 3,961.83 ms | 2,695.82 ms | 3,203,570,768 | 0 | 0 |
| Existing `ReadArchive` path, warm | 3,799.52 ms | 19,967 | n/a | 3,658.93 ms | 2,209.31 ms | 3,198,309,408 | 0 | 0 |
| Snapshot path, cold | 162.98 ms | 260 | 19,707 / 260 | 88.50 ms | 42.29 ms | 62,735,360 | 2,783,276 | 0 |
| Snapshot path, warm | 151.87 ms | 0 | 19,967 / 0 | 0 ms | 0 ms | 18,502,576 | 0 | 0 |

The existing archive-load duration covers file-store reads, container decode, archive decode, and snapshot packing; container-decode time is a subset, not an additional cost. These are single-run local measurements, not a hosted-service benchmark or a production request trace. They demonstrate the expected archive-load reduction for this controlled workload without establishing a general speed-up target.

## Risks / Trade-offs

- **A local write blocks member reads for unrelated archives** → Accepted for simple, provable ordering; measure contention before considering finer-grained coordination.
- **The byte budget is not an exact heap/RSS limit** → Include packed data, descriptors, and fixed metadata in retention accounting, and describe it as a retention bound.
- **External processes or direct FileStore writes can leave snapshots and memoized reference tables stale** → Keep the existing operational requirement to restart affected readers; do not claim cross-process coherence.
- **CacheWriter may update the archive and reference-table file in multiple non-transactional steps** → Invalidate the snapshot in `finally`, propagate the original failure, and do not expand this change into rollback or repair logic.
- **A failure after a disk mutation can leave existing on-disk partial-write behavior** → This change only prevents stale in-process snapshot reuse; `CacheWriter` transactionality remains a separate concern.

## Migration Plan

No persisted data or public `ICacheAPI` signature changes. The new option uses existing options binding. Deploying a process starts with an empty local snapshot cache; rollback is a code rollback, with no cache-file migration. Existing running readers must still be restarted after unsupported raw or external mutations.
