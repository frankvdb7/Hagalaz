## Context

The current worker loop waits with `Task.Delay(TickTimeSpan, stoppingToken)` and then executes the complete game tick. Most worker tests configure `TickTimeSpan` as zero and use blocking gates to stop the loop after a particular callback. This couples ordinary assertions to scheduler timing and makes failure cleanup depend on code after the gate being reached. Direct assembly execution shows the pathfinder and worker tests complete quickly; the risky ownership boundary is the hosted loop used by the worker fixtures.

## Goals / Non-Goals

**Goals:**

- Keep one production owner for the repeating worker loop.
- Expose one internal, deterministic operation for a complete tick so ordinary tests do not create a repeating worker.
- Make ordinary tests call the internal tick operation directly; hosted lifecycle coverage must await shutdown on every path.
- Preserve phase ordering, snapshot sharing, cancellation, exception logging, and overrun behavior.
- Use a focused collision fake only if measured pathfinder retention proves NSubstitute call history is material.

**Non-Goals:**

- Do not change production tick frequency, global test parallelism, CI memory limits, or garbage-collection policy.
- Do not pool or reuse SmartPathFinder arrays.
- Do not rewrite unrelated fixtures or add a second scheduler/queue.

## Decisions

1. **A single internal tick operation owns one complete tick.** The repeating hosted loop remains responsible only for waiting, invoking that operation, and classifying cancellation versus unexpected errors. This keeps the test seam aligned with the production operation without exposing a public API.

2. **Use the internal tick operation as the deterministic test boundary.** Ordinary tests call one complete tick directly, so they do not need to control the hosted delay. The only hosted lifecycle test that does not need a tick uses a long real delay and stops the service immediately. A new clock or scheduler abstraction is rejected because it would add a second timing mechanism to production.

3. **Use exception-safe test ownership.** Tests that start the hosted service release gates and await `StopAsync` in `finally` blocks. Ordinary tick tests do not start the hosted service and therefore cannot strand its execution task.

4. **Do not replace pathfinder substitutes speculatively.** First compare direct pathfinder execution and inspect lookup volume/retention. If NSubstitute is material, replace only the hot collision lookup with a small non-recording fake; keep interaction assertions on tests that actually require them.

5. **Keep diagnostic recording bounded by behavior.** The worker logger records the bounded entries asserted by a test rather than serving as an unlimited history for a producer that may still be running.

## Risks / Trade-offs

- [Risk] A hosted lifecycle test can accidentally recreate a hot loop. → Keep ordinary behavior tests on the internal tick seam and use a long delay for the no-tick hosted cancellation test.
- [Risk] Moving assertions to the internal tick seam could stop testing hosted shutdown. → Retain focused lifecycle tests that start the actual `BackgroundService` and assert its execution task is completed after cleanup.
- [Risk] The `dotnet test` MSBuild driver may retain more evaluation memory than direct VSTest. → Report those as separate runner evidence and validate the actual testhost independently; do not claim a test-lifecycle fix solved build-driver memory without measurement.

## Migration Plan

No deployment or data migration is required. Apply the production seam and test fixture changes together, run the focused GameWorld tests through direct assembly execution and the project runner where memory permits, then run the broader relevant tests. Revert the change as one unit if the acceptance tests show altered worker ordering or shutdown semantics.
