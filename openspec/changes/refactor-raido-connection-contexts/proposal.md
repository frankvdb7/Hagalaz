## Why

Raido currently combines stable reconnect state, physical transport state, and logical Hub state in one `RaidoConnectionContext`. Splitting those responsibilities will make the existing #477 stateful reconnect lifecycle explicit without adding another reconnect mechanism or coupling Raido to the later GameWorld reconnect work.

## What Changes

- **BREAKING** Rename the public logical context to `RaidoHubConnectionContext`.
- **BREAKING** Replace the staged connection-context builder API with `IRaidoHubConnectionContextFactory`.
- **BREAKING** Rename the Hub-facing `ConnectionAbortedToken` property to `ConnectionAborted`.
- Rename the logical Hub handler, store, and lifetime-manager vocabulary to `RaidoHubConnectionHandler`, `RaidoHubConnectionStore`, and `IRaidoHubLifetimeManager`.
- Add an internal `RaidoTcpConnectionContext : ConnectionContext` for stable TCP and physical transport state.
- Give the TCP context stable `Transport` and internal `Application` pipes that survive physical replacement, with a minimal lower-level physical transport relay.
- Move existing reconnect, transport, heartbeat-registration, and terminal-lifecycle behavior to the TCP context without redesigning it.
- Keep protocol, message writing, Hub timeout policy, caller state, and logical lifecycle state on the Hub context.
- Resolve connection-specific initial protocols in the application connection scope, keep protocol selection outside the factory, and coordinate explicit protocol transitions with Hub writes.
- Resolve dynamically selected protocols in a dedicated async scope whose lifetime is transferred to the logical Hub context and disposed on protocol replacement or logical cleanup.
- Update the connection-context factory, dispatchers, lifetime management, stores, callers, handlers, consumers, and tests to use the split.
- Remove Raido APIs and escape hatches added specifically for the current #488 GameWorld integration.
- Stop exposing raw physical reader/writer access through the public Hub context; callers that need the logical API use `RaidoHubConnectionContext` instead.

## Capabilities

### New Capabilities

- `raido-connection-contexts`: Separates stable TCP connection behavior from logical Hub connection behavior while preserving stateful physical reconnect.

### Modified Capabilities

- None.

## Impact

This is a source-level Raido refactor affecting the server context, handler, connection-context factory, dispatcher, lifetime manager, connection store, caller context, options, and focused tests. GameWorld reconnect authentication and cross-context physical transport handoff remain separate follow-up work.

## Migration

This is a breaking Raido API refactor. Replace `RaidoConnectionContext` with `RaidoHubConnectionContext`, the staged connection-context builder API with `IRaidoHubConnectionContextFactory`, and `ConnectionAbortedToken` with `ConnectionAborted`. The logical Hub handler, store, and lifetime manager are now named `RaidoHubConnectionHandler`, `RaidoHubConnectionStore`, and `IRaidoHubLifetimeManager`. The raw connection `CreateReader`/`CreateWriter` extension APIs and public signatures using the old context type are removed; raw physical reader and writer access is intentionally no longer exposed through the public Hub context. Replace mutable `Protocol` assignment with the explicit asynchronous `SetProtocolAsync` transition, passing an explicit cancellation token. Protocol transitions are serialized with writes: an in-flight old-protocol write completes before the transition changes the protocol, later writes use the new protocol, and an already-started read is not reinterpreted. Factory callers pass the `statefulReconnect` choice explicitly.
