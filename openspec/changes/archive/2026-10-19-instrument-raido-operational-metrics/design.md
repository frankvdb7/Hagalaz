# Design

## Context

See proposal.md for motivation. `RaidoMetrics` already owns the `Raido.Server` meter and connection instruments. The connection handler owns decoded-message dispatch, `RaidoHubConnectionContext.WriteAsync` is the shared per-recipient application-message write boundary, and `RaidoTcpConnectionContext` owns the logical detach/rebind/terminal transitions. `Hagalaz.ServiceDefaults` already exports the meter.

## Goals / Non-Goals

**Goals:**

- Record bounded counts, durations, and outcomes at those existing ownership boundaries.
- Keep metric-disabled hot paths free of metric-only timestamps, tag construction, attempt objects, and outcome-state mutation.
- Preserve write and reconnect behavior.

**Non-Goals:**

- Add a telemetry abstraction, service, dependency, dashboard, or GameWorld-specific instrumentation.
- Change the public dispatch contract or reconnect transition rules.
- Repair the separately identified active-connection accounting issue.

## Decisions

1. **Extend `RaidoMetrics` and reuse its meter.** Add counters and histograms to the existing singleton and extend its per-connection enabled-instrument snapshot. This keeps instrument ownership and export under `Raido.Server`. A separate metrics service or meter would duplicate ownership and require extra wiring.

2. **Record inbound metrics once at the receive dispatch boundary.** `RaidoHubConnectionHandler` records after the dispatcher completes. The built-in dispatcher reports handled, unhandled, unauthorized, and error outcomes through an internal per-message outcome on the logical connection; custom dispatchers that complete without reporting an outcome are treated as handled. This avoids changing the public `IRaidoDispatcher` contract and avoids counting one message once per registered hub. No outcome state is touched when inbound instruments are disabled.

3. **Record outbound writes at `RaidoHubConnectionContext.WriteAsync`.** All application responses and client proxies converge there, including one call per recipient for broadcasts. A metric attempt object is created only when the write instruments are enabled so early unavailable, flush, cancellation, and failure paths can produce one accurate outcome without changing the existing write contract. A successful stable logical transport flush is tagged `accepted`; the physical output pump forwards bytes separately, so this outcome does not claim physical delivery. Keepalive pings continue through their existing separate path.

4. **Observe reconnect transitions inside `RaidoTcpConnectionContext`.** Use the existing reconnect-window timestamp as the duration start. Record detach after the state lock is released, record success when a replacement physical connection is published, and record timeout or terminal failure when the existing terminal transition completes. The connection factory supplies the existing `RaidoMetrics` before initial physical activation so an early detach is observable. Terminal transition outcomes are carried to the existing post-lock completion boundary; metric callbacks do not run while holding the lifecycle lock.

5. **Count client timeout only when the terminal transition wins.** `CheckClientTimeout` already checks the active state and calls `TryAbortIfActive`; increment the timeout counter only when that call returns true. Repeated heartbeat checks then cannot double count.

6. **Use fixed outcome strings and message type names only.** Dispatch and write metrics use the bounded application-defined Raido message type plus a closed outcome set. An unavailable write distinguishes a detached reconnect window from an already-terminal connection. Reconnect metrics use only a closed outcome set. Do not include identities, endpoints, payloads, or exception details.

## Risks / Trade-offs

- **Metric names and type-name tags become operational contracts** → Keep names consistent with `raido.server.*`, use closed outcome values, and cover them with `MeterListener` tests.
- **Custom dispatcher outcome detail is limited by the existing public contract** → A successful custom dispatcher call maps to `handled`; thrown exceptions map to `error`. Built-in dispatchers provide the finer unhandled and unauthorized outcomes.
- **Metrics can add work when enabled** → Snapshot instrument enablement once per logical connection and avoid metric-only work on disabled paths.
