# Tasks

## 1. Metrics

- [x] 1.1 Add the instruments and recording methods to `GameWorldMetrics`.
- [x] 1.2 Instrument `CharacterDehydrationWorkerService` snapshot capture and flush attempts.
- [x] 1.3 Instrument `CharacterLogoutService` recovery claims.
- [x] 1.4 Add `CharacterPersistenceHealthMetricsTests` with `MeterListenerSpy`.

## 2. Verification

- [x] 2.1 Run the `Hagalaz.Services.GameWorld.Tests` suite.
- [x] 2.2 Run `openspec validate gameworld-character-persistence-metrics --strict` and review the diff.
