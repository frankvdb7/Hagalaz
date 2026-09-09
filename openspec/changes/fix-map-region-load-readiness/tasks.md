## 1. Readiness and preparation

- [x] 1.1 Keep `MapRegion.Load()` as the final readiness publication signal.
- [x] 1.2 Stage static decode callbacks and verify decode failure applies no
      collision or objects.
- [x] 1.3 Prepare database-backed source data and configured map entities before
      applying region state.
- [x] 1.4 Retain fail-closed collision reads for not-ready regions.

## 2. Failure and discard behavior

- [x] 2.1 Propagate cache container/decode failures while preserving documented
      missing-archive semantics.
- [x] 2.2 Register NPCs after map preparation, skip only isolated non-cancellation
      NPC failures, and continue with valid NPCs.
- [x] 2.3 Unregister all NPCs successfully registered by a failed attempt and
      preserve cleanup failures.
- [x] 2.4 Exact-remove failed region instances and verify stale failures cannot
      remove a current replacement.
- [x] 2.5 Verify later requests create a fresh instance and failed instances are
      not reset or reused.
- [x] 2.6 Delete obsolete region and region-part unpublished-load rollback
      interfaces, reset methods, helpers, and same-instance rollback tests.

## 3. Direct NPC cleanup

- [x] 3.1 Use direct indexed `NpcStore` lookup under the existing reader lock and
      remove the unused predicate lookup API.
- [x] 3.2 Keep registration/cleanup aggregate exceptions flat and understandable.

## 4. Verification

- [x] 4.1 Retain deterministic scheduler coalescing and ready-region suppression
      coverage.
- [ ] 4.2 Run focused map loader, map service, map provider, NPC, and scheduler
      tests plus `git diff --check`.
- [ ] 4.3 Run strict OpenSpec validation and the affected project build.
- [ ] 4.4 Rebuild/restart the affected service and manually verify static and
      custom-object clipping in the client.
