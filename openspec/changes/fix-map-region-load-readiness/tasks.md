## 1. Lifecycle, readiness, and preparation

- [x] 1.1 Replace the loaded flag with `MapRegionState` containing only
      `Initializing`, `Ready`, and `Discarded`.
- [x] 1.2 Guard legal transitions and publish `Ready` only after complete
      population; make `Discarded` terminal without reset/retry.
- [x] 1.3 Stage static decode callbacks and verify decode failure applies no
      collision or objects.
- [x] 1.4 Prepare database-backed source data and configured map entities before
      applying region state.
- [x] 1.5 Retain fail-closed collision reads for not-ready regions.
- [x] 1.6 Filter GameWorker processing to `Ready` regions.
- [x] 1.7 Rebind stale viewport/map-update references through the canonical
      map-region service and consume only `Ready` data.

## 2. Scheduler and ownership

- [x] 2.1 Require an `Initializing` region to remain the exact canonical
      service instance before scheduling.
- [x] 2.2 Reject discarded/stale instances and preserve in-flight coalescing.
- [x] 2.3 Preserve exact-instance removal and verify old R1 cannot remove R2.

## 3. Failure and discard behavior

- [x] 3.1 Propagate cache container/decode failures while preserving documented
      missing-archive semantics.
- [x] 3.2 Register NPCs after map preparation and propagate construction and
      registration-service failures as fatal load failures.
- [x] 3.3 Unregister all NPCs successfully registered by a failed attempt and
      preserve cleanup failures.
- [x] 3.4 Mark failed instances discarded, exact-remove them, and verify stale failures cannot
      remove a current replacement.
- [x] 3.5 Verify later requests create a fresh instance and failed instances are
      not reset or reused.
- [x] 3.6 Delete obsolete region and region-part unpublished-load rollback
      interfaces, reset methods, helpers, and same-instance rollback tests.

## 4. Direct NPC cleanup

- [x] 4.1 Use direct indexed `NpcStore` lookup under the existing reader lock and
      remove the unused predicate lookup API.
- [x] 4.2 Keep registration/cleanup aggregate exceptions flat and understandable.

## 5. Verification

- [x] 5.1 Retain deterministic scheduler coalescing and ready-region
      suppression coverage; cover explicit viewport rebinding and dynamic
      packet selection independently of readiness.
- [x] 5.2 Run focused map loader, map service, map provider, NPC, viewport,
      worker, and scheduler tests plus `git diff --check`.
- [x] 5.3 Run strict OpenSpec validation and the affected project build.
- [ ] 5.4 Rebuild/restart the affected service and manually verify static and
      custom-object clipping in the client.
