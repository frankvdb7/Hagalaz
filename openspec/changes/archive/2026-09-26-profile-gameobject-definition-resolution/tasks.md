## 1. Investigation profiling

- [x] 1.1 Profile per-batch cache-key, HybridCache outcome/factory, and returned-dictionary work without IDs or per-definition activities.
- [x] 1.2 Profile repository, provider/archive, codec/fallback, and composition work; confirm the original per-definition SQL N+1 was resolved and isolate the remaining archive reconstruction cost.

## 2. Root-cause analysis and runtime handoff

- [x] 2.1 Run focused and full GameWorld and Cache tests, affected builds, strict OpenSpec validation, and `git diff --check`.
- [x] 2.2 Analyze the supplied completed login profile: the original per-definition SQL N+1 is fixed; repeated whole-archive reconstruction in type-provider reads is a separate Cache-layer optimization assigned to #506. No Aspire restart or client interaction is part of this cleanup.

## 3. Permanent observability disposition

- [x] 3.1 Remove investigation-only phase spans, per-archive Cache tracing, FileStore profiling, archive split timing, and the elapsed-time success log; retain the two bounded GameWorld activities and add low-cardinality operational metrics.
- [x] 3.2 Add focused ActivityListener and MeterListener coverage for region loads, bulk definition resolution, cache outcomes, archive fallback, and Cache archive/container work.
- [x] 3.3 Keep snapshot-cache metrics and archive reuse implementation deferred to #506; do not add observability product requirements because this change uses `skip_specs: true`.

## Validation results

- Focused GameWorld tests: 20 passed, 0 failed.
- Focused Cache API tests: 5 passed, 0 failed.
- Full GameWorld tests: 1,180 passed, 0 failed, 0 skipped.
- Full Cache tests: 237 passed, 0 failed, 0 skipped.
- `Hagalaz.ServiceDefaults`, `Hagalaz.Cache`, and `Hagalaz.Services.GameWorld` builds: passed with 0 warnings and 0 errors.
- `openspec validate profile-gameobject-definition-resolution --type change --strict`: passed.
- `git diff --check`: passed.
