## 1. Worker lifecycle and serialized execution

- [x] 1.1 Convert `GameWorkerService` to the existing `BackgroundService` lifecycle and remove the discarded `Task.Run`/local cancellation source.
- [x] 1.2 Await the existing four-phase region pipeline directly and preserve its phase ordering and region snapshot semantics.
- [x] 1.3 Measure completed tick duration, log budget overruns, and distinguish stopping cancellation from unexpected tick exceptions.

## 2. Deterministic regression coverage

- [x] 2.1 Add a blocked-major-update regression proving an over-budget tick cannot overlap the next tick.
- [x] 2.2 Add phase-order and adjacent-tick serialization coverage for prepare, update, and reset phases.
- [x] 2.3 Add overrun logging, genuine exception, host cancellation, and shutdown-waits-for-in-flight-work coverage.

## 3. Validation

- [x] 3.1 Run the focused GameWorld test project and the relevant test filter to a clean exit.
- [x] 3.2 Run a solution build, strict OpenSpec validation, and final diff/status review.
