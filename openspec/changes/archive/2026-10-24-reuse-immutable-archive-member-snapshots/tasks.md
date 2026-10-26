# Tasks

## 1. Snapshot representation and retention

- [x] 1.1 Implement immutable packed member snapshots with checked physical slices and fresh read-only streams; verify single-member, multi-member, multi-chunk, negative-delta, empty-member, and stream-ownership cases against `ArchiveDecoder` fixtures.
- [x] 1.2 Implement per-`CacheApi` byte-bounded LRU retention, options validation, and cache metrics; verify default/zero/negative capacity, exact fit, oversize exclusion, recency, eviction, accounting, and empty snapshots in `Hagalaz.Cache.Tests` and `Hagalaz.Cache.Extensions.Tests`.
- [x] 1.3 Add active same-archive load sharing and exact flight cleanup; verify same-key overlap, independent archives, oversized/zero-capacity sharing, failed-load retry, and no retained flight accumulation with controlled test gates.

## 2. Cache API ordering

- [x] 2.1 Route the three-argument `CacheApi.Read` through validated snapshots while preserving file-not-found and corruption failures; verify physical member mapping and cache-hit behavior with actual decoded archive data.
- [x] 2.2 Coordinate member reads and `CacheApi.Write` with the specified reader/writer lock and exact invalidation; verify successful/throwing writes, active-load exclusion, unrelated-entry survival, writer serialization, and continued usability of pre-write streams using observable member bytes.
- [x] 2.3 Verify `ReadArchive` still returns separate mutable archives that cannot alter snapshot bytes; run the focused cache API and archive decoder tests.

## 3. Provider hot path

- [x] 3.1 Migrate generic and specialized single-definition `Get` calls to physical-member reads while preserving custom delegation, factory fallback, range behavior, fresh definitions, and hook frequency; verify each provider path in focused provider tests.
- [x] 3.2 Verify GameWorld definition overrides remain isolated from cached archive bytes and other decodes; run the focused GameWorld definition-loading tests.

## 4. Integration evidence

- [x] 4.1 Run cache, cache-extension, and GameWorld test projects and inspect new test assertions against every behavior requirement in the delta spec.
- [x] 4.2 Run solution tests, solution build, strict OpenSpec validation, and `git diff --check`; inspect analyzer/duplication results and the complete base-to-branch diff.
- [x] 4.3 Measure a controlled cold/warm revision-742 GameWorld cache-resolution workload; results and its no-live-database limitation are recorded in `design.md`.

## Workflow follow-up

- Archive the change only after every required acceptance criterion is verified and the canonical cache-archive-member-snapshots specification reflects the implemented behavior.
- Commit, push the requested branch, and create a PR referencing #506; use a draft when the runtime A/B acceptance criterion remains unverified.
