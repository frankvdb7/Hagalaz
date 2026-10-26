## Current ownership graph

- Each provider owns the cache, type data, codec/factory, and optional hook used by `Get`; the generic provider also delegates to an optional custom provider.
- `TypeProvider<T, TD>` currently coordinates shared decoding through static methods that accept those dependencies on every call.
- `CacheApi` owns the reader/writer lock and snapshot cache, and currently also owns table slots, per-index locks, memo loading, and memo clearing.
- The registered `ReferenceTableProvider` is stateless. `CacheWriter` uses it directly to decode an independent mutable table before persisting a write.
- Generic and NPC `GetRange` batch adjacent definitions by archive and own the resulting `IArchive`; item, object, and sprite ranges call `Get`.

## Invariants

- Generic and sprite single reads require a parent archive mapping; item, NPC, and object reads fall back when the parent mapping is absent. All providers fall back for an absent child mapping.
- Mapped physical read and codec failures propagate. A successful mapped single read invokes its configured hook once with the actual provider; a factory fallback skips the hook.
- Decoded type objects are fresh. Generic custom-provider delegation and each range algorithm remain unchanged.
- `CacheApi` remains the only writer-lock owner. Lock order is CacheApi reader/writer lock, cold per-index memo lock, then snapshot-cache synchronization. A memo clear is called under the CacheApi writer lock and does not take the per-index lock.
- CacheWriter keeps its direct, independent stateless reference-table-provider dependency. Existing high-level and raw mutation invalidation rules remain unchanged.
- Range archive members remain owned by their archive. Do not separately dispose member streams.

## Design decisions

- Add one internal sealed `CacheTypeDecoder<T>` with the cache, type data, factory, codec, and optional hook captured once in its constructor. Its two named methods express the established required-parent difference without flags or strategies. Both read one table and share a private child-decoding method.
- Compose the decoder directly in the five existing provider constructors. Keep public signatures and DI registrations as-is. Providers retain only fields still needed by archive size, range loading, hooks, or custom delegation.
- Add one internal `ReferenceTableCache` with bounded arrays and per-index monitors. It owns only memo state and cold loading. `CacheApi` constructs it and retains all cache-wide locking, write calls, invalidation, and snapshot ownership.
- Do not add a lock to `ReferenceTableCache.Clear`: the existing CacheApi writer lock excludes loads through the same API. Do not call public `ReadReferenceTable` while already holding CacheApi's non-recursive read lock.
- In both batched range methods, dispose the previous archive before loading the next and dispose the final archive in `finally`. Leave type iteration, decoding, fallback, and one range-level hook call in place.

## Rejected alternatives

- Keeping static forwarding methods would preserve the utility-class coupling and repeated dependency passing this change removes.
- Adding public/cache-wide invalidation or another lock would duplicate CacheApi ownership and risk recursion/contention.
- Redirecting CacheWriter through CacheApi would mutate or share reader-held table objects and create a circular ownership boundary.
- Replacing range batching with per-definition snapshot reads would change the established range algorithm and hook cadence.

## Validation evidence

Existing CacheApi tests already cover warm memo hits, failed-load retry, synchronized same-index initialization, independent index loads, write invalidation, raw mutation invalidation, and ordering. Existing provider tests cover physical member mapping, fallbacks, hooks, fresh object definitions, custom delegation, and mapped errors. Extend these suites only for missing gaps and add deterministic archive-disposal regressions.
