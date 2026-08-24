## Context

`WorldReconnectRequest` is already produced by the opcode-18 decoder from the real revision-742 handshake payload. `HandshakeHub` currently rejects it. A normal world sign-in authenticates, creates a world session, hydrates a new character, registers it, commits ownership, and publishes world sign-in side effects; none of those creation paths are valid during reconnect.

## Goals / Non-Goals

**Goals:**

- Authenticate reconnect credentials using the existing authentication boundary.
- Match only the existing local world session for the authenticated master id and its retained logical Raido connection.
- Atomically hand off the replacement physical transport and a fresh protocol instance.
- Preserve the existing session, character, store registration, region membership, contacts, persistence revision, and runtime state.
- Rebuild the client’s authoritative map/entity view through the existing `ICharacter.UpdateMap` operation.
- Route terminal cleanup through `ConnectionHub.OnDisconnectedAsync` exactly once.

**Non-Goals:**

- Outbound packet buffering, replay, sequence numbers, ACKs, or exactly-once delivery.
- Authentication/session tokens that are not present in revision 742’s handshake.
- Re-running character hydration, `OnRegistered`, world-session registration, Contacts sign-in, or persistence initialization.
- A generic snapshot/resynchronization framework or broad per-subsystem state replay.
- Lobby reconnect, cross-process ownership, or distributed session routing.

## Decisions

1. **Authenticate before lookup.** The reconnect request’s existing login/password payload is validated through `AuthenticationService`, then the resulting authenticated master id selects the existing session. The client-supplied login is never used as a session key.

2. **Require an active world session and retained logical context.** The session must be an `IGameWorldSession`, its connection must be present in `RaidoConnectionStore` and `Reconnecting`, and its logical features must contain the same character/master ownership. A missing or mismatched condition rejects the replacement.

3. **Reserve during the handshake and commit after reader ownership is released.** The reconnect hub reserves the existing logical connection from inside the temporary handshake dispatch. Raido then stops the replacement physical pumps, waits for the target's previous pumps, captures the temporary reader's unread suffix, installs the fresh protocol, commits the stable application pipes, flushes the reconnect response first, runs the post-commit map/appearance resynchronization, and only then releases the suffix to the resumed application. This prevents two readers, prevents replacement bytes from being read with the old ISAAC state, and makes a successful reconnect response the first packet on the new physical connection.

4. **Do not copy replacement features into the logical context.** Authentication, session, character, contacts, and caller items remain owned by the original logical context. The replacement context is only a physical transport carrier and is not allowed to become a second GameWorld session.

5. **Resynchronize current authoritative state only.** Register the existing character map update with a forced viewport rebuild and appearance refresh as explicit post-commit work. Raido runs it after the successful response is queued/flushed and before pending replacement input and normal reconnected traffic are released. Transient packets lost during the break are not replayed.

6. **Keep failure cleanup idempotent.** A rejected replacement is aborted without publishing a new lobby/world sign-out. The winning replacement completes the temporary application only after the transfer commits; only logical expiry/abort reaches the existing disconnect path.

7. **Keep the reservation narrow.** The GameWorld boundary registers one reconnect response and one post-commit resynchronization action; it does not introduce a second session owner, replay queue, or general reconnect coordinator. An invalidated reservation terminates the temporary replacement instead of allowing ordinary handshake fallback.

## Risks / Trade-offs

- The real handshake authenticates again, so credentials must remain valid during the grace window; this is required by the issue’s authenticated-ownership boundary.
- Current-state resynchronization cannot recover transient effects already lost on the wire; that is explicitly preferable to inventing a replay protocol.
- Reconnect is same-process only because the authoritative character and session remain in the existing in-memory stores.

## Migration Plan

Enable stateful reconnect support for the GameWorld Raido endpoint with the existing bounded default grace period. The logical connection opts in only after successful world sign-in; lobby and pre-auth connections retain terminal transport-loss behavior. No persistent migration is required.
