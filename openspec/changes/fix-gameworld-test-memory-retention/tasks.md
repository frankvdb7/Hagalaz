## 1. Remove uncontrolled worker-test execution

- [x] 1.1 Extract the complete single-tick operation from `GameWorkerService`, preserve the existing hosted delay and production phase ordering, cancellation, exception, and overrun behavior; verify with a focused GameWorker test build.
- [x] 1.2 Migrate ordinary `GameWorkerServiceTests` off `TickTimeSpan = TimeSpan.Zero` and add deterministic coverage for one tick, adjacent ticks, cancellation, unexpected exceptions, overrun logging, and snapshot sharing; verify the exact worker test names pass without a repeating worker.
- [x] 1.3 Retain only focused hosted-lifecycle tests, keep the no-tick cancellation test on a long delay, and guarantee gate release plus awaited `StopAsync`/`ExecuteTask` completion in `finally` cleanup; verify shutdown and cancellation tests pass when assertions fail or cancellation occurs.
- [x] 1.4 Make worker test diagnostic recording bounded to asserted entries and confirm no test-owned worker, callback, or recorder remains active after each lifecycle test; verify repeated direct worker-test execution has stable completion and no unbounded process growth.

## 2. Audit high-frequency pathfinder fixtures

- [x] 2.1 Run the pathfinder tests through the built test assembly and inspect collision lookup volume and process/live-heap behavior separately from the worker tests; record whether NSubstitute call history is a material retention contributor.
- [x] 2.2 Confirm whether the evidence is material; no collision fake is added because the direct pathfinder run showed no material retention signal.

## 3. Validate the change

- [ ] 3.1 Run the focused GameWorld worker and pathfinder tests, then the complete `Hagalaz.Services.GameWorld.Tests` project with serialized, non-overlapping execution; verify the intended test counts pass and no testhost remains running after completion. Direct current-source worker tests pass 12/12 repeatedly, and the existing full assembly passes 811/811 with testhost peaking around 129 MB and exiting normally. Normal project-level attempts remain pending because the outer `dotnet` driver grew to about 942 MB (filtered) and 1.35 GB (full) without spawning testhost or producing test output.
- [ ] 3.2 Run the broader relevant test projects, validate the OpenSpec change strictly, and review the cumulative diff for public API changes, global parallelism changes, GC workarounds, disabled tests, or stranded background work. OpenSpec strict validation and focused diff review pass; broader project execution remains pending. The GameWorld testhost retention issue is isolated from the remaining outer `dotnet`/MSBuild driver growth.
