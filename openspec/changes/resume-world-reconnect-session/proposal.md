## Why

Issue #476 preserves revision-742 world opcode 18 as explicit reconnect intent and #477 retains an opted-in Raido logical connection during a bounded physical transport loss. GameWorld still rejects that intent, so a valid reconnect loses the existing in-memory character and session instead of reattaching the client.

## What Changes

- Authenticate the existing revision-742 world reconnect handshake without creating a second session or hydrating a second character.
- Resolve the existing world session by the authenticated master id and require its Raido logical connection to be in the reconnecting state.
- Rebind the replacement physical transport through `RaidoConnectionStore` and install a fresh protocol/ISAAC instance before post-reconnect writes.
- Reuse the existing `GameSession` and exact `ICharacter` instance, then send only focused authoritative state needed to rebuild the client view.
- Keep failed/expired/racing reconnects on the existing terminal cleanup path exactly once.

## Capabilities

### Modified Capabilities

- `raido-logical-reconnect`: GameWorld consumes the existing rebind operation and can provide the fresh protocol used by the replacement transport.
- `gameworld-world-session`: A valid opcode-18 reconnect resumes an existing world session and character.

## Impact

The change is limited to the GameWorld handshake/authentication/session integration and the Raido rebind protocol transfer. It reuses the existing session store, character store, authentication service, protocol resolver, map update API, and terminal disconnect pipeline. No generic replay queue, ACK protocol, distributed ownership, snapshot framework, or lobby behavior is introduced.

## Acceptance Criteria

- A valid revision-742 opcode-18 handshake finds the intended reconnecting logical world session using authenticated ownership.
- The replacement transport is attached through Raido, with a fresh protocol/ISAAC state before the first post-reconnect write.
- The exact existing `GameSession` and `ICharacter` instances remain in use; normal hydration, registration, and world-session sign-in are not repeated.
- Invalid ownership, expired sessions, protocol mismatch, and concurrent losers cannot attach to another session.
- Physical loss during the grace period does not run GameWorld logout/persistence cleanup; terminal expiry runs the existing cleanup once.
- Reconnect resynchronization uses only existing authoritative update APIs and does not replay old encoded bytes.

## Stop Conditions

Stop and record a follow-up if satisfying the issue requires changing the revision-742 client, adding a generic snapshot/replay architecture, modifying lobby reconnect, or introducing cross-process/distributed reconnect ownership.
