## 1. Readiness publication

- [x] 1.1 Move the region-ready transition to the end of the complete population pipeline and verify with a gated loader test that readiness remains false through static collision population and becomes true only after success
- [x] 1.2 Gate world collision reads for not-ready regions with the existing blocking collision semantics and verify that pathfinding and per-tick movement cannot step through an in-progress region while ready-region flags remain unchanged

## 2. Failure and retry behavior

- [x] 2.1 Preserve the scheduler's single in-flight owner and add deterministic failure and cancellation tests proving the in-flight admission is released and readiness is not published
- [x] 2.2 Add a deterministic retry test proving a later request invokes the loader again after a failed attempt, and make any focused region-state cleanup required for that retry use the existing region lifecycle without introducing a second queue or state store
- [x] 2.3 Retain and run the existing duplicate-request and ready-region suppression tests to verify no concurrent duplicate loads or unnecessary reloads are introduced
- [x] 2.4 Add transactional rollback coverage for NPCs, items, static and non-static objects, collision, cancellation, rollback failure, and retry on the same region instance
- [x] 2.5 Make `NpcService.RegisterAsync` transactional across global-store
      publication, region membership, initialization, and owned scope cleanup;
      add store-insertion and partial-registration regression tests.
- [x] 2.6 Reject destroyed NPC instances before registration and use a fresh
      NPC for retry coverage.
- [x] 2.7 Always attempt global NPC-store removal during unregistration, even
      after destruction failure, and preserve both failures when removal also
      fails.

## 3. Verification and runtime validation

- [x] 3.1 Run the focused GameWorld readiness, scheduler, map-region, and pathfinder tests with `dotnet test Hagalaz.Services.GameWorld.Tests/Hagalaz.Services.GameWorld.Tests.csproj --no-restore`
- [x] 3.2 Build the affected GameWorld project and run `git diff --check` to verify compilation and whitespace cleanliness
- [x] 3.3 Run `openspec validate fix-map-region-load-readiness --type change --strict` and confirm all proposal, spec, design, and task artifacts pass validation
- [ ] 3.4 Rebuild and restart the affected GameWorld service, then manually verify a static wall or solid map object blocks movement after loading while a known custom object retains its existing clipping behavior
