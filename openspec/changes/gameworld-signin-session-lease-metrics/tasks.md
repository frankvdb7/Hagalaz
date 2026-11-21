# Tasks

## 1. Metrics

- [x] 1.1 Add the instruments and recording methods to `GameWorldMetrics`.
- [x] 1.2 Record the outward sign-in outcome in `AuthenticationService`.
- [x] 1.3 Record lease renewal outcomes in `GameSessionLeaseService`.
- [x] 1.4 Maintain the pending-cleanup gauge at `GameSessionStore` transitions.
- [x] 1.5 Add `AuthenticationSignInMetricsTests` and `SessionReliabilityMetricsTests` with `MeterListenerSpy`.

## 2. Verification

- [x] 2.1 Run the `Hagalaz.Services.GameWorld.Tests` suite.
- [x] 2.2 Run `openspec validate gameworld-signin-session-lease-metrics --strict` and review the diff.
