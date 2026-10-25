## Why
The cache providers currently use `TypeProvider<T, TD>` as a static decoding utility, while `CacheApi` owns reference-table memoization inline. Both responsibilities already have clear owners and lifetimes, but their implementation is coupled to unrelated provider and cache orchestration code. The generic and NPC archive-batched range methods also retain decoded archives without disposing them.

## What changes
Compose one internal `CacheTypeDecoder<T>` in each supported type provider, extract per-index reference-table memo state into an internal `ReferenceTableCache` owned by `CacheApi`, and dispose batched archives in generic and NPC range reads. Preserve constructor signatures, provider-specific fallbacks, hooks, cache lock ordering, range algorithms, and snapshot behavior.

## Non-goals
No public interfaces or invalidation APIs, DI registration for internal collaborators, new synchronization framework, async redesign, snapshot algorithm changes, CacheWriter transaction changes, sparse reference-table changes, or unrelated provider/cache cleanup.

## Acceptance criteria
- A1: Type decoding is an instance responsibility composed once by each generic, item, NPC, object, and sprite provider; all three static decoding methods are removed.
- A2: Provider missing-parent, missing-child, mapped-data failure, physical-member mapping, fresh-definition, hook-owner/frequency, and custom-delegation behavior remains unchanged.
- A3: `ReferenceTableCache` owns fixed table slots and per-index initialization locks; CacheApi remains the only consistency coordinator, and CacheWriter continues to decode its own independent table.
- A4: Existing same-instance write invalidation and lock ordering remain unchanged; snapshot implementation is untouched.
- A5: Generic and NPC range batching and hook behavior remain unchanged, while each decoded archive is disposed on switches and on every exit path.
- A6: Focused regression coverage, full prescribed solution validation, OpenSpec validation, baseline-relative jscpd, and final cumulative-diff review pass. Any unavailable benchmark is reported without an invented result.

## Stop conditions
If implementation requires changing public constructors, provider fallback semantics, cache synchronization scope, CacheWriter persistence, or the snapshot algorithm, stop and revise the design rather than expanding this change.
