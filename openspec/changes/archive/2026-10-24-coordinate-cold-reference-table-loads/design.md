# Design

`ReferenceTableProvider` already owns the memoized table array and the `Invalidate(indexId)` operation. Give each array slot its own monitor. Warm reads use a volatile fast path; cold reads re-check, decode, and publish under that index's monitor. `Invalidate` takes the same monitor before clearing the slot. A failed decode leaves the slot empty, so a later caller can retry. Different indices remain independent.

This keeps table initialization at its existing owner and does not expand synchronization into `CacheApi` or definition codecs. `CacheApi` continues to coordinate local mutations with its existing reader/writer lock; provider-level locking also keeps its own read/invalidate operations consistent.

## Verification

The profile used the local revision-742 cache at `C:\Users\fmavd\TheStormProject\data\Cache`, an already warm archive snapshot, and 20 synchronized 16-reader bursts. Before the change, every cold burst read and decoded index 16's table 16 times; cold mean/median was 44.546/42.551 ms and warm mean/median was 0.175/0.172 ms. After adding per-index synchronization, every cold burst read the table once; cold mean/median was 9.114/7.225 ms and warm mean/median was 0.163/0.161 ms. Warm bursts performed no table reads. The final implementation then added a volatile warm-hit fast path; the cold path is unchanged, but no separate timing is claimed for the final warm path. This controlled local profile isolates reference-table memoization and is not production request telemetry.
