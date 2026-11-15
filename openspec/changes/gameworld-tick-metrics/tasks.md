# Tasks

## 1. Tick instruments in the GameWorld worker

- [x] 1.1 Add the five tick instruments, bounded outcome and phase constants, and a phase scope that records only when its histogram is enabled to `GameWorldMetrics`. Verify the project builds.
- [x] 1.2 Inject `TimeProvider` into `GameWorkerService` and record tick count, duration, overrun and interval in `ExecuteTickAsync`, and phase durations around the scheduler, major update, client prepare, client update, client reset and housekeeping steps. Verify the existing sequential-execution and shutdown tests stay green.
- [x] 1.3 Add `GameWorkerTickMetricsTests` using a manual `TimeProvider` and `MeterListenerSpy`, covering success, at-budget, overrun, phase failure, worker cancellation, foreign cancellation, interval, and instrument units and tag keys.

## 2. Verification

- [x] 2.1 Run the `Hagalaz.Services.GameWorld.Tests` suite.
- [x] 2.2 Run `openspec validate gameworld-tick-metrics --strict` and review the diff.
