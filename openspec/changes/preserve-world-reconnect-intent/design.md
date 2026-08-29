## Context

Completed GameClient #142 work establishes the revision-742 request contract: world authentication uses opcode 16, with flag 0 for fresh login and flag 1 for reconnect. The authentication registry contains no opcode 18 entry. Issue #478 remains blocked on the server-side response, cipher, session, transport, lifecycle, and resynchronization contract.

## Goals / Non-Goals

**Goals:**

- Remove the stale authentication opcode-18 registration.
- Preserve the opcode-16 reconnect flag as explicit request intent.
- Prevent reconnect intent from entering `HandshakeHub.SignInWorld`.
- Preserve the secret-safe controlled-peer/client characterization.
- Leave #477 and all Raido production ownership/lifetime behavior unchanged.

**Non-Goals:**

- A reconnect handler, response 15 implementation, authentication/session orchestration, cipher transition, transport handoff, lifecycle behavior, or resynchronization.
- Any change to the separate ISAAC-framed game-channel opcode 18.
- A Startup refactor or new production testability seam.

## Decisions

### Remove only the stale authentication registration

`Startup` will retain `WorldHandshakeRequestDecoder` on authentication opcode 16 and remove its opcode-18 registration. No replacement authentication handler is added. The game-channel protocol is outside this change.

### Preserve intent with one minimal message type

`WorldHandshakeRequestDecoder` will keep one parser and one common field extraction path. The parsed boolean is named `isReconnect`; it selects `WorldSignInRequest` for false and a sealed `WorldReconnectRequest : ClientSignInRequest` for true. The reconnect message has no additional fields and no handler.

### Rely on exact concrete-type dispatch

Raido's dispatcher indexes handlers by `message.GetType()`. A `WorldReconnectRequest` therefore has no `WorldSignInRequest` descriptor and is ignored until a future #478 implementation supplies an explicit handler. This intentional no-op is safer than authenticating reconnect input as a fresh login. A generic BaseMessage/DerivedMessage regression test covers this existing Raido behavior without GameWorld knowledge.

### Preserve evidence boundaries

The fixture keeps controlled-peer inputs, controlled-peer/client observations, client-code facts, and unknown production behavior separate. Response 15 and the 4,608-byte read remain observations, not production requirements. RSA/XTEA ciphertext and ISAAC keys remain excluded.

## Risks / Trade-offs

- [Reconnect input is temporarily ignored] → This is intentional until #478 establishes response and failure behavior; it prevents accidental fresh-login authentication.
- [A client-code opcode 18 is mistaken for an authentication opcode] → Keep authentication registry/trace facts separate from the ISAAC-framed game channel.
- [A controlled-peer observation is treated as a server contract] → Qualify every fixture key and OpenSpec scenario.

## Migration Plan

No migration or rollout is required. The only runtime changes are the request-side registration and intent distinction.
