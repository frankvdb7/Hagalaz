# Tasks

## 1. Cache invalidation ownership

- [x] 1.1 Add memoized reference-table invalidation and per-index snapshot eviction, with focused cache/provider tests proving that a changed table is loaded again and retained entries for that index are removed.

## 2. Raw mutation integration

- [x] 2.1 Route raw write and copy through `ICacheAPI` using its existing write lock, preserve raw bytes, invalidate affected state in `finally`, and guard Cache API reference-table loads with the matching read lock; verify table-load ordering and a same-service integration regression for table replacement, raw archive replacement, and raw copy through subsequent object-type reads.
- [x] 2.2 Update the canonical `cache-archive-member-snapshots` specification to match the delta and verify the focused service test, OpenSpec strict validation, and `git diff --check`.

## Workflow follow-up

- Archive this change after all tracked tasks and required validation pass.
