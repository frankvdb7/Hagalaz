# Tasks

## 1. Population and workload metrics

- [x] 1.1 Add the gauges, the `tick.regions` histogram and their publishing methods to `GameWorldMetrics`.
- [x] 1.2 Publish store counts after successful add/remove, idle counts after suspend/resume/idle removal, and the ready-region count in the worker tick.
- [x] 1.3 Add `GameWorldPopulationMetricsTests` covering rejected, duplicate and repeated transitions, units, tags and the tick sample.

## 2. Verification

- [x] 2.1 Run the `Hagalaz.Services.GameWorld.Tests` suite.
- [x] 2.2 Run `openspec validate gameworld-population-metrics --strict` and review the diff.
