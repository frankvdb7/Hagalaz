## Why

Issue #478 needs a working revision-742 world reconnect path. The current server creates a new connection/session path for a reconnecting physical connection, while the existing #477 Raido support can retain the old logical connection during a bounded physical transport loss.

The controlled revision-742 client characterization recorded in GameClient #142 establishes the observed request contract: fresh world login uses opcode 16 with reconnect flag 0, and reconnect uses opcode 16 with reconnect flag 1. No wire opcode 18 was emitted. The same evidence records response 15, the 4,608-byte `readEnterWorldPacket(true)` payload, authentication reset, protocol preservation, new client/server ISAAC instances, temporary-key clearing, RSA/XTEA boundaries, the server-key `+50` transform, and event ordering. The change connects those existing mechanisms to that observed contract without treating the old opcode-18 assumption as production behavior.

## What Changes

- Preserve opcode 16 as fresh `WorldSignInRequest` handling.
- Dispatch opcode 16 with reconnect flag 1 as `WorldReconnectRequest`, using only fixture-proven request fields.
- Do not treat wire opcode 18 as the active reconnect contract; reject or leave it unsupported rather than routing it into fresh sign-in or reconnect handling.
- Add a distinct response-15 message and encoder based on the characterized payload fixture.
- Apply only the characterized authentication reset, protocol/cipher transition, and `readEnterWorldPacket(true)` resynchronization payload.
- Add a transport-only Raido handoff that adopts replacement physical transport ownership while the old logical context retains protocol/cipher ownership, logical lifecycle, and completion state.
- Authenticate reconnect credentials through reused existing validation without creating a session, hydrating or registering a character, replacing ownership, or leaving temporary token/features unmanaged.
- Resume the existing `GameSession` and exact `ICharacter` only after the characterized reconnect sequence completes.
- Add focused protocol, Raido, authentication, session, cleanup, and ordering regressions.

## Capabilities

### New Capabilities

- `raido-reconnect-handoff`: Transport-only replacement ownership and consumed/unread input handoff for a retained logical context.
- `world-reconnect-session`: Characterized revision-742 opcode-16 reconnect-flag authentication, response 15, protocol/cipher transition, session reuse, and resynchronization.

### Modified Capabilities

- `openspec/specs/game-world-sessions/spec.md`: A retained world session may be resumed by its authenticated owner without creating or replacing a session, and terminal cleanup remains exact-owner and once-only.

## Impact

- Raido.Server connection context, handler, store, and transport handoff internals.
- GameWorld handshake decoding, response encoding, authentication service, connection setup, and handshake hub.
- Existing GameSessionStore and GameSession connection-ID routing remain authoritative.
- No new package, worker, reconnect registry, distributed route, GameUpdate path, lobby path, replay, ACK, or deduplication behavior.
- The checked-out GameClient source is not changed. Its issue #142 controlled-peer characterization supplies the observed client contract; Hagalaz tests must use a local secret-safe fixture and must not depend on that checkout.

## Acceptance criteria

- Opcode 16 with reconnect flag 0 remains fresh world sign-in and produces `WorldSignInRequest`.
- Opcode 16 with reconnect flag 1 produces `WorldReconnectRequest` and does not enter fresh-login orchestration.
- Wire opcode 18 is not treated as the active reconnect contract.
- Response 15, its characterized world-entry payload, protocol/cipher transition, physical handoff, resynchronization, and resumed reads/writes follow the checked-in fixture-defined order.
- The existing GameSession and exact ICharacter instance are reused without normal hydration, duplicate registration, or duplicate terminal cleanup.

## Stop conditions

- Do not implement a wire-opcode-18 path, an uncharacterized cipher transition, an assumed response/adoption order, or an unproven resynchronization message.
- Stop and update this change if the checked-in fixture cannot represent the observed opcode-16 reconnect-flag contract or its characterized response/state transition.
