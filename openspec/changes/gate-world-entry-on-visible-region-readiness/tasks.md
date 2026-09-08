## 1. Scheduler readiness barrier

- [x] 1.1 Add scheduler-owned completion tracking for scheduled regions without changing `IMapRegionLoadScheduler.RequestLoad`.
- [x] 1.2 Add an internal all-visible-regions wait operation that treats only `IsLoaded == true` as success and reports loader/readiness failures without retrying.
- [x] 1.3 Add deterministic scheduler tests for waiting on every region, already-loaded suppression, and failure completion.
- [x] 1.4 Use one lock-protected in-flight dictionary, make load failures fault
      shared waiters, keep caller cancellation local, and cancel unresolved
      waiters during scheduler shutdown.

## 2. World-entry integration

- [x] 2.1 Rebuild the initial character viewport and await all visible regions before `Character.OnRegistered()`.
- [x] 2.2 Abort the session when initial region readiness fails and delegate cleanup to the normal disconnect/sign-out owner, without publishing successful world presence.
- [x] 2.3 Add world-sign-in consumer regression tests for delayed success and clean failure/disconnect behavior.
- [x] 2.4 Reuse the prebuilt entry viewport during startup map delivery and add
      focused coverage that prevents a second rebuild.

## 3. Verification

- [x] 3.1 Run focused GameWorld scheduler, map-update, and world-sign-in tests with isolated output paths.
- [ ] 3.2 Run `dotnet build` for the affected GameWorld project and `git diff --check`.
- [x] 3.3 Run `openspec validate gate-world-entry-on-visible-region-readiness --type change --strict`.
