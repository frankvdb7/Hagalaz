## Context

`HandshakeProtocol` reads the first packet byte to select a decoder, then passes only the remaining payload to `IRaidoMessageDecoder`. `Startup` currently maps both world opcodes 16 and 18 to `WorldHandshakeRequestDecoder`, whose parser always constructs `WorldSignInRequest`. The revision-742 client checkout writes the same world handshake fields for its game authentication packet and does not show a synthetic reconnect field.

## Goals / Non-Goals

**Goals:**

- Use the existing opcode registry to keep opcode 16 and 18 distinct through decoding.
- Share the existing world payload parser and produce a typed reconnect request for opcode 18.
- Keep fresh-login authentication unchanged.
- Return the existing failed response for reconnect until later session-recovery changes land.

**Non-Goals:**

- Changing `IRaidoMessageDecoder` or generic Raido framing.
- Adding reconnect credentials, tokens, session ids, timers, rebinds, replay, or persistence.
- Changing the client checkout.

## Decisions

1. **Register a separate reconnect decoder type.** The codec store already selects decoders by opcode. Registering a reconnect-specific subclass for opcode 18 preserves intent without changing every decoder implementation or adding a second generic opcode-carrying state. Opcode 16 continues to use `WorldHandshakeRequestDecoder`.

2. **Share parsing through the existing world decoder.** The base decoder owns RSA/XTEA parsing and construction of common `ClientSignInRequest` fields. A small message-construction seam lets the opcode-specific decoder choose `WorldSignInRequest` or `WorldReconnectRequest` without duplicating wire parsing.

3. **Dispatch reconnect explicitly in `HandshakeHub`.** A handler for the reconnect message sends `ClientSignInResponse.Failed` and aborts. It does not call the existing sign-in method, so opcode 18 cannot authenticate as a fresh login by accident.

4. **Test the decoder boundary and hub branch separately.** Decoder tests prove opcode-specific types and field parity. Hub tests prove the reconnect branch does not invoke authentication and emits the safe failure response. Existing fresh-login tests remain the regression for opcode 16.

The decoder owns message construction. The hub owns the application decision after dispatch. No retry or reconciliation owner exists in this change because unsupported reconnects terminate immediately.

## Risks / Trade-offs

- [A reconnect packet using a future, different layout will be rejected] → The issue requires the current verified revision-742 layout; a changed client payload belongs to a follow-up protocol change.
- [A new decoder registration adds one small type] → This is narrower than changing the common decoder interface and avoids touching every Raido decoder.
- [Reconnect remains unavailable] → The hub returns the existing failure response and #477/#478 can later replace only that branch.

## Migration Plan

No data migration is required. Deploy the decoder/message/handler and tests together. Rollback is a source revert; no persistent state is changed.

## Open Questions

None. The client wire layout and unsupported reconnect behavior are fixed by the issue acceptance criteria.
