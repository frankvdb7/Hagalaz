## Why

Raido currently treats a Kestrel transport as the whole hub connection. A transient socket loss therefore removes the connection immediately and invokes terminal hub cleanup, leaving no same-process opportunity for a replacement transport to continue the existing logical connection.

## What Changes

- Split the existing Raido connection context into a stable logical identity/lifetime and a replaceable physical transport generation.
- Add an explicitly opted-in, bounded reconnect grace window for same-process transport loss.
- Add one application-callable rebind operation that atomically installs a replacement transport for the retained logical connection.
- Reject stale generations for input dispatch and writes, and make sends while detached fail explicitly.
- Expose connection features for explicit logical opt-in/veto, post-rebind notification, and reader-safe physical transport handoff.
- Preserve immediate terminal disconnect behavior for non-opted-in connections and make terminal cleanup happen once.
- Add focused Raido tests for lifecycle, rebind races, stale generations, expiry, explicit close, sends, and shutdown.

## Capabilities

### New Capabilities

- `raido-logical-reconnect`: Retains an opted-in Raido logical connection during a bounded same-process transport loss and permits one replacement transport rebind.

### Modified Capabilities

None.

## Impact

The change is limited to `Raido.Server` connection context, handler, store, options, reconnect features, focused `Raido.Server.Tests`, and the GameWorld activation boundary. It reuses the existing lifetime manager, dispatcher, caller context, connection store, pipes, and cancellation primitives. No dependency, wire protocol, negotiation, authentication, replay buffer, persistence, or Redis change is introduced.

## Acceptance Criteria

- Non-opted-in transport loss still reaches terminal disconnect immediately.
- Opted-in transport loss retains one logical connection during a bounded grace window without invoking terminal hub disconnect at loss time.
- Endpoint support alone does not retain a connection; only an explicitly enabled eligible logical connection can enter the grace window.
- A known logical connection can accept exactly one winning replacement transport; the logical identity, caller context, features, items, protocol association, and client destination survive.
- The old generation cannot dispatch input or receive writes after a successful rebind.
- Sends while the logical connection is detached fail explicitly and are not replayed.
- Grace expiry, explicit close, and server shutdown remove the logical connection and invoke terminal cleanup exactly once.
- Concurrent rebind attempts have one deterministic winner and no write/rebind deadlock.
- Application/protocol code can veto reconnect before loss or during the grace window, with terminal cleanup occurring once.
- A replacement handshake completes its reader before the lower physical transport is transferred, and the replacement handler does not run terminal logical disconnect callbacks after a successful handoff.
- The repository's focused Raido tests, build, and strict OpenSpec validation pass.

## Stop Conditions

Stop and record a follow-up if satisfying the change requires GameWorld authentication/session resume, reconnect wire fields, replay or duplicate suppression, sequence/ACK state, encryption/ISAAC reinitialization, cross-process storage, or a generic actor/state-machine framework.
