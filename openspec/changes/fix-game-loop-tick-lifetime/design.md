## Context

`GameWorkerService` currently implements `IHostedService` manually. `StartAsync` starts `Task.Run(DoWork, ...)` and discards the returned task. Each iteration creates a major region pipeline and applies `Task.WaitAsync` to it; that timeout only abandons the caller's wait, so the region pipeline can continue while a later iteration starts. The region API is already asynchronous but does not accept cancellation tokens, so a running phase cannot be safely interrupted by this change.

The service already runs the scheduler and the four region phases in one sequential method. The existing .NET hosting abstraction provides the lifecycle ownership needed here without introducing another synchronization mechanism. The region tick APIs are the narrow cancellation boundary: they can stop between synchronous creature callbacks and asynchronous creature callbacks without changing the broader creature contracts.

## Goals / Non-Goals

**Goals:**

- Keep all major region phases in one awaited, serial pipeline.
- Make the hosted service own the worker lifetime and wait for a running tick during normal shutdown.
- Keep the tick budget observable without using it to abandon work.
- Pass the worker stopping token through all four asynchronous region tick phases and observe it at safe iteration boundaries.
- Preserve normal loop recovery after a genuine tick exception and distinguish shutdown cancellation from faults.
- Add deterministic tests using controlled tasks rather than timing sleeps.

**Non-Goals:**

- Do not parallelize regions or phases.
- Do not change the synchronous or asynchronous creature tick contracts; cancellation is owned by the region phase boundary.
- Do not introduce locks, queues, a second worker, metrics infrastructure, or a generic game-loop abstraction.
- Do not change scheduler semantics, region discovery, or lifecycle/readiness services outside this worker.

## Decisions

1. **Use `BackgroundService` as the owner of execution and shutdown.** `ExecuteAsync(CancellationToken)` becomes the single worker task and the base `StartAsync` retains it. `StopAsync` requests cancellation through the base service but passes `CancellationToken.None` to the base wait, because .NET 10 suppresses wait cancellation and could otherwise report success while `ExecuteTask` is still alive. The worker therefore waits for the current tick to finish or for a region API to reach an explicit safe cancellation boundary.

2. **Replace `WaitAsync` with a direct await of the complete pipeline.** The service will not begin the next delay or tick until all four phase loops have completed. A `Stopwatch` records the pipeline duration and emits a warning when it exceeds `TickTimeSpan`; this keeps overruns visible without creating a second execution path.

3. **Keep per-iteration exception handling explicit.** Delay cancellation is handled separately from tick execution. During a tick, only an `OperationCanceledException` carrying the worker stopping token ends the loop without an unexpected-failure log; an independently thrown cancellation exception remains a genuine tick failure even if shutdown was requested concurrently. The awaited task boundary ensures no unfinished tick is bypassed.

4. **Propagate cancellation through the region boundary.** `IMapRegion`’s four asynchronous tick methods accept an optional `CancellationToken`. `MapRegion` checks it before and between part/item/creature callbacks, while existing creature callback signatures remain unchanged. The generic creature rendering wrapper returns the concrete update task directly; character rendering remains task-based because viewport loading and character-store enumeration are genuinely asynchronous.

5. **Test the public hosted-service lifecycle.** Tests start and stop the real service with zero-duration budgets and `TaskCompletionSource` barriers. This makes the old `WaitAsync` overlap and fire-and-forget shutdown behavior reproducible without sleeps, while also asserting host-token expiry, cancellation-token propagation, and observable logger events.

## Risks / Trade-offs

- [A region phase ignores cancellation] → Normal shutdown can wait for that phase, which is required to prevent work surviving `StopAsync`; the host timeout cannot make this service report success while its worker task remains alive.
- [A cancellation exception is thrown by tick work] → Matching the exception's cancellation token, rather than only checking whether shutdown was requested, preserves genuine tick failures.
- [A slow tick delays later ticks] → This is the intended serialized-world behavior; the overrun warning makes the condition visible.
- [A tick exception aborts the remaining phases of that tick] → Existing exception handling already aborts the failed pipeline and continues the worker loop; the exception remains logged and no subsequent tick can overlap the faulted task.

## Migration Plan

Deploy as a normal code change. No data or configuration migration is required. Rollback is a code rollback to the previous worker implementation.

## Open Questions

None.
