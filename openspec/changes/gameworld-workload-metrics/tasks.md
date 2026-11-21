# Tasks

## 1. Workload metrics

- [x] 1.1 Add the instruments and publishing methods to `GameWorldMetrics`.
- [x] 1.2 Instrument `RsTaskService.Tick()`.
- [x] 1.3 Instrument `MapRegionLoadScheduler` enqueue, dequeue, completion and shutdown.
- [x] 1.4 Add `GameWorldWorkloadMetricsTests` with `MeterListenerSpy` and a manual `TimeProvider`.

## 2. Verification

- [x] 2.1 Run the `Hagalaz.Services.GameWorld.Tests` suite.
- [x] 2.2 Run `openspec validate gameworld-workload-metrics --strict` and review the diff.
