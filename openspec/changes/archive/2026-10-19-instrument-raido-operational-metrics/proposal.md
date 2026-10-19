# Proposal

## Why

Raido's existing `Raido.Server` meter reports connection count and lifetime, but operators cannot see message dispatch outcomes, per-client message writes, reconnect recovery, or client timeout terminations. Adding these signals to the existing meter makes the current Prometheus/Grafana export useful for diagnosing message and stateful reconnect behavior.

## What Changes

- Add inbound message count and dispatch duration with bounded message-type and outcome dimensions.
- Add per-connection outbound message-write count and duration with bounded message-type and write-outcome dimensions at the shared write boundary.
- Add logical reconnect lifecycle events and wait duration, and expose client timeout termination through the same meter.
- Keep reconnect behavior, GameWorld code, metric export wiring, and the separate active-connection accounting issue out of scope.

## Capabilities

### New Capabilities

- `raido-server-metrics`: Operational message, reconnect, and timeout metrics emitted by the existing Raido server meter.

### Modified Capabilities

None.

## Impact

- Affected runtime code: `Raido.Server` dispatch, connection write, logical reconnect, and client-timeout boundaries, plus its existing `RaidoMetrics`.
- Affected verification: focused `MeterListener` coverage in Raido server tests and reconnect integration tests where needed.
- No new package or service wiring is expected; `Hagalaz.ServiceDefaults` already exports the `Raido.Server` meter.

## Scope and Acceptance

- A handled, unhandled, unauthorized, or failed inbound dispatch records one count and a duration with a bounded outcome.
- Each actual per-connection outbound message write records a count and duration with a bounded outcome; recipient identity and payload data are never dimensions.
- Detach, reconnect completion, reconnect timeout or terminal failure, and client timeout are recorded at the Raido lifecycle transitions that own them. Reconnect duration starts at logical detach and ends at rebind or terminal completion.
- Focused `MeterListener` tests cover dispatch outcomes, per-recipient writes including failure/cancellation, reconnect outcomes and duration, and exactly-once client timeout recording.
- Existing `Raido.Server` meter export discovers the new instruments without per-service configuration changes.

## Non-goals and Stop Conditions

- Do not change reconnect semantics, add dashboards, add bytes-in/bytes-out metrics, or tag metrics with connection/player IDs, endpoints, payloads, or exception details.
- Do not fix the independent active-connection accounting issue as part of this change.
- If correct outcome recording would require changing public dispatch contracts or reconnect transitions, stop and revise this design before proceeding.
