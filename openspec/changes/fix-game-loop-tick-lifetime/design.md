## Context

`GameWorkerService` currently implements `IHostedService` manually. `StartAsync` starts `Task.Run(DoWork, ...)` and discards the returned task. Each iteration creates a major region pipeline and applies `Task.WaitAsync` to it; that timeout only abandons the caller's wait, so the region pipeline can continue while a later iteration starts. The region API is already asynchronous but does not accept cancellation tokens, so a running phase cannot be safely interrupted by this change.

The service already runs the scheduler and the four region phases in one sequential method. The existing .NET hosting abstraction provides the lifecycle ownership needed here without introducing another synchronization mechanism.

## Goals / Non-Goals

**Goals:**

- Keep all major region phases in one awaited, serial pipeline.
- Make the hosted service own the worker lifetime and wait for a running tick during normal shutdown.
- Keep the tick budget observable without using it to abandon work.
- Preserve normal loop recovery after a genuine tick exception and distinguish shutdown cancellation from faults.
- Add deterministic tests using controlled tasks rather than timing sleeps.

**Non-Goals:**

- Do not parallelize regions or phases.
- Do not add cancellation-token parameters to the existing region tick APIs.
- Do not introduce locks, queues, a second worker, metrics infrastructure, or a generic game-loop abstraction.
- Do not change scheduler semantics, region discovery, or lifecycle/readiness services outside this worker.

## Decisions

1. **Use `BackgroundService` as the owner of execution and shutdown.** `ExecuteAsync(CancellationToken)` becomes the single worker task. The base `StartAsync` retains it and the base `StopAsync` requests cancellation and awaits it, reusing the established hosting contract instead of duplicating task plumbing. The host-provided stopping token owns the loop boundary; the current non-cancellable region operation is allowed to finish before the worker exits.

2. **Replace `WaitAsync` with a direct await of the complete pipeline.** The service will not begin the next delay or tick until all four phase loops have completed. A `Stopwatch` records the pipeline duration and emits a warning when it exceeds `TickTimeSpan`; this keeps overruns visible without creating a second execution path.

3. **Keep per-iteration exception handling explicit.** An `OperationCanceledException` associated with the worker stopping token ends the loop without an unexpected-failure log. Other exceptions are logged as major tick errors and the loop remains capable of starting a later tick, while the awaited task boundary ensures no unfinished tick is bypassed.

4. **Test the public hosted-service lifecycle.** Tests will start and stop the real service with zero-duration budgets and `TaskCompletionSource` barriers. This makes the old `WaitAsync` overlap and fire-and-forget shutdown behavior reproducible without sleeps, while also asserting the observable logger events.

## Risks / Trade-offs

- [A region phase ignores cancellation] → Normal shutdown can wait for that phase, which is required to prevent work surviving `StopAsync`; host shutdown cancellation still bounds the hosting wait through `BackgroundService`.
- [A slow tick delays later ticks] → This is the intended serialized-world behavior; the overrun warning makes the condition visible.
- [A tick exception aborts the remaining phases of that tick] → Existing exception handling already aborts the failed pipeline and continues the worker loop; the exception remains logged and no subsequent tick can overlap the faulted task.

## Migration Plan

Deploy as a normal code change. No data or configuration migration is required. Rollback is a code rollback to the previous worker implementation.

## Open Questions

None.
