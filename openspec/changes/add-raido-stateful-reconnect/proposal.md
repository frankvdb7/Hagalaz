## Why

Raido currently treats its physical transport as permanent, so a transport loss ends the stable connection context and any write while detached can be misreported as successful through discard pipes. Stateful reconnect needs to replace only the physical transport while preserving the existing connection-scoped state and lifetime.

## What Changes

- Add opt-in stateful reconnect for Raido connection contexts, including a bounded configurable reconnect timeout.
- Rebind a published physical `ConnectionContext` without replacing the stable `RaidoConnectionContext` or its delegated state.
- Detach physical transports on loss, wake only their pending operations, and ignore stale callbacks and operation failures after replacement.
- Make the handler create a fresh protocol reader for each physical transport and wait in one reconnect window between detach and successful publication.
- Remove discard/no-op pipe behavior from the reconnect path; detached writes return without touching a pipe.
- Enable the feature only for GameWorld and add focused regression coverage for transport races, ownership, callbacks, timeout, and lifetime stability.

## Capabilities

### New Capabilities

- `raido-stateful-reconnect`: Opt-in physical transport rebinding with stable connection state, bounded reconnect waiting, and stale-operation race safety.

### Modified Capabilities

- None.

## Impact

- Affected production code is limited to `Raido.Server` connection context, handler, builder/options integration, and the GameWorld connection builder opt-in.
- Existing connection store, lifetime manager, GameWorld session/protocol state, and GameUpdate behavior remain unchanged.
- Tests use real `Pipe` instances and existing test doubles; no production fake pipes or reconnect framework are introduced.
