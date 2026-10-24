# Design

## Ownership

`CacheApi` owns a fixed-size table array and one monitor per ordinary index, initialized from `IFileStore.IndexFileCount`. A private helper validates the index, checks the table slot with `Volatile.Read`, then locks and rechecks only on a cold miss. It calls the existing provider to decode and publishes only successful results with `Volatile.Write`. The existing `ReaderWriterLockSlim` is always acquired before a per-index monitor.

`ReferenceTableProvider` retains only the existing store/container/codec dependencies and performs the existing physical index-255 read, container decode, and reference-table decode on each call. It does not memoize or synchronize. `CacheWriter` continues to call it directly, so a write gets an independent table object and does not mutate the object returned by `CacheApi`.

## Write ordering

CacheApi high-level writes keep the writer lock around `CacheWriter.Write` and, in `finally`, clear the affected ordinary-index table slot and invalidate the exact archive snapshot. Raw writes and copies also keep the lock through the store operation and `finally` invalidation. An ordinary target invalidates only its snapshot. A target `(255, dataIndex)` clears that index's table slot and all its snapshots. Table clearing needs no per-index monitor because the writer lock excludes every CacheApi memo read and initialization.

Raw copy reads the source stream and passes that stream directly to `IFileStore.Write`. It keeps byte-for-byte behavior and does not update versions, checksums, or reference-table metadata.

## Consistency boundary

Previously returned mutable reference-table objects are independent references and are not retroactively changed by clearing CacheApi's slot. `ReadArchive`, `ReadContainer`, and composed range/inspection operations remain non-atomic across their full multi-call sequence. Direct file-store and cross-process writes do not trigger local invalidation.

## Test decisions

Use the existing `TestSupport/MemoryFileStore` for real CacheWriter/provider/codec and service integration tests. Keep concurrency tests event-gated. Keep existing type-provider, sparse-ID, snapshot, and GameWorld tests as regression evidence. Do not create a second store fake.
