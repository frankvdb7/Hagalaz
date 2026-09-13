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
- [x] 1.8 Publish concurrent active-region creation through the existing
      concurrent dictionary so callers converge on one canonical instance.
- [x] 1.9 Centralize active/idle suspension, resume, and exact idle-destruction
      claims in `MapRegionService` without adding lifecycle states.
- [x] 1.10 Preserve requested dimension identity during region construction and
      revalidate dimension ownership before publication.
- [x] 1.11 Preserve the source dimension in dynamic region-part mappings while
      keeping copied runtime objects in the destination dimension.
- [x] 1.12 Serialize character and NPC membership mutations with active-region
      residency and recheck suspension eligibility at the ownership boundary.
- [x] 1.13 Keep script-backed suspension checks outside the residency gate and
      retain only structural eligibility facts for the final commit.
- [x] 1.14 Route live non-creature mutations through explicit
      `MapRegionService` operations while preserving loader-owned writes.
- [x] 1.15 Route hub/network script and command entrypoints through the
      existing character task queue before they can mutate live map state.
- [x] 1.16 Share one `RsTaskService` instance between creature task scheduling
      and the GameWorker tick boundary, and preserve FIFO task execution.
- [x] 1.17 Keep Raido message scopes request-scoped; resolve scoped gameplay
      dependencies from the Character-owned provider inside deferred tasks and
      serialize ordered Character input through the shared scheduler.
- [x] 1.18 Keep CPU-bound command work data-only; apply Character, widget, and
      script effects on the serialized GameWorker continuation.

## 2. Scheduler and ownership

- [x] 2.1 Preserve in-flight coalescing while leaving canonical-instance
      validation to `MapRegionLoader`.
- [x] 2.2 Reject discarded regions at the scheduler boundary and let the
      loader reject stale instances.
- [x] 2.3 Preserve exact-instance removal and verify old R1 cannot remove R2.
- [x] 2.4 Remove dimensions only through an exact-current-and-empty operation
      under the same residency synchronization boundary used by publication.
- [x] 2.5 Submit an initial load request when a new canonical region is
      published through `IMapRegionLoadScheduler`, whose private channel owns
      request delivery and shutdown.

## 3. Failure and discard behavior

- [x] 3.1 Propagate cache container/decode failures while preserving documented
      missing-archive semantics.
- [x] 3.2 Register NPCs after map preparation and propagate construction and
      registration-service failures as fatal load failures.
- [x] 3.3 Unregister all NPCs successfully registered by a failed attempt,
      log secondary cleanup failures, and preserve the primary failure.
- [x] 3.4 Mark failed instances discarded, exact-remove them, and verify stale failures cannot
      remove a current replacement.
- [x] 3.5 Verify later requests create a fresh instance and failed instances are
      not reset or reused.
- [x] 3.6 Delete obsolete region and region-part unpublished-load rollback
      interfaces, reset methods, helpers, and same-instance rollback tests.
- [x] 3.7 Make terminal region destruction best-effort across NPCs, ground
      items, and game objects, preserving the first cleanup failure after all
      attempts.
- [x] 3.8 Remove pending-destruction residency and retry ownership; let
      `MapRegionService` claim exact idle residency before sequential terminal
      destruction, with game-worker serialization as the mutation boundary.

## 4. Direct NPC cleanup

- [x] 4.1 Use direct indexed `NpcStore` lookup under the existing reader lock and
      remove the unused predicate lookup API.
- [x] 4.2 Preserve primary registration/cleanup failures and log best-effort
      cleanup failures without aggregate exception plumbing.
- [x] 4.3 Make creature removal exact-owner-safe when an index is reused.
- [x] 4.4 Track NPC script initialization before `OnCreate` so registration
      rollback invokes `OnDestroy` for partial initialization only.

## 4A. API boundaries

- [x] 4A.1 Expose dimension residency as read-only dictionaries.
- [x] 4A.2 Depend on `IMapRegionLoadScheduler` at the world sign-in boundary
      while preserving the shared scheduler implementation.

## 5. Verification

- [x] 5.1 Retain deterministic scheduler coalescing, canonical concurrent
      region creation, and ready-region
      suppression coverage; cover explicit viewport rebinding and dynamic
      packet selection independently of readiness. Cover concurrent resume,
      stale destruction claims, destruction-before-resume, suspend-vs-create,
      stale-instance operations, non-zero dimensions, exact dimension removal,
      and terminal cleanup failure handling.
- [x] 5.2 Run focused map loader, map service, map provider, NPC, viewport,
      worker, and scheduler tests plus `git diff --check`.
- [x] 5.3 Run strict OpenSpec validation and the affected project build.
- [ ] 5.4 Rebuild/restart the affected service and manually verify static and
      custom-object clipping in the client using the documented steps in the
      design artifact. Keep this unchecked until the graphical client is run.
- [x] 5.5 Add deterministic character/NPC attach-versus-suspend coverage,
      including stale suspension eligibility and existing-region load request
      behavior.
- [x] 5.6 Add deterministic script lock-scope, idle-resume mutation, and
      non-suspendable NPC attachment coverage.
- [x] 5.7 Add deterministic command-to-game-loop coverage and retain the
      active/idle, dynamic-region, script-callback, and NPC-attachment race
      coverage at the service ownership boundary.
