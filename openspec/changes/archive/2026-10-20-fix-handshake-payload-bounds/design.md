## Context

`HandshakeProtocol` passes world and lobby payloads to `WorldHandshakeRequestDecoder` and `LobbyHandshakeRequestDecoder`. Both decoders parse the plain header and RSA block, then decrypt the remaining XTEA payload. The current code uses `UnreadSpan`, rents a buffer whose capacity can exceed the payload, constructs readers over the full rented arrays, and advances over settings data without checking the remaining length. `ReadOnlySequence<byte>` inputs can also be multi-segment. The decoder returns `false` for normal parse failures, so the correction must preserve that boundary.

## Goals / Non-Goals

**Goals:**

- Make the shared handshake framing and temporary-buffer paths exact-length and multi-segment safe.
- Keep malformed and truncated input on the existing `false`/null-message failure convention.
- Ensure pooled encrypted/decrypted handshake bytes are cleared on every exit path.
- Cover both world and lobby decoders where the shared defect applies.

**Non-Goals:**

- Changing RSA or XTEA algorithms, credentials, cache CRC semantics, or authentication decisions.
- Rewriting the general Raido protocol framing or adding fuzzing infrastructure.
- Replacing existing sequence readers, string readers, or cache services.

## Decisions

1. **Use the declared packet length as the outer bound.** `TryParsePacketHeader` will require the declared length to equal the remaining input. This matches the existing login protocol contract and prevents trailing bytes from becoming part of the XTEA region. A looser `remaining >= declared` check is rejected because it cannot distinguish the current packet from trailing data.

2. **Centralize XTEA copy, decryption, and cleanup in the handshake helper.** A focused helper will validate the remaining length, copy `UnreadSequence` into the exact used prefix of a rented buffer, decrypt only that prefix, invoke the existing decoder-specific parser over a bounded sequence, then clear and return the buffer in `finally`. This removes the duplicated unsafe buffer lifecycle from world and lobby decoders. Returning a fresh exact-sized managed array is rejected because it would abandon the existing pooling path and leave sensitive data unmanaged by an explicit cleanup owner.

3. **Bound RSA parsing by the actual written byte count.** `BigInteger.TryWriteBytes` provides the number of initialized bytes; the RSA reader will use only that prefix and the helper will clear it before returning the rented array. The RSA header copy will likewise clear its used prefix. This closes the same full-capacity class of bug before XTEA begins.

4. **Validate before every variable advance.** Settings and hardware skips will check `SequenceReader.Remaining` before advancing. Existing `TryRead` operations remain the authoritative field and string validation mechanism; no new protocol-validation framework is introduced.

The decoder owns construction of the final sign-in request, while the shared handshake helper owns temporary buffer acquisition, bounded exposure, and cleanup. There is no retry or reconciliation state in this path; malformed input is rejected by the decoder and the connection lifecycle remains the existing owner of subsequent handling.

## Risks / Trade-offs

- [Strict packet equality rejects payloads with unexpected trailing bytes] → This is required by the login framing contract and prevents ambiguous packet ownership; valid clients already provide the declared exact length.
- [Clearing pooled buffers adds a small per-handshake cost] → The buffers contain credentials and key material, so deterministic cleanup is required and limited to the used ranges.
- [The shared callback helper changes the shape of decoder parsing] → Keep all field parsing and message construction in the existing decoder-specific code so behavior remains reviewable and protocol semantics do not move into a generic parser.

## Migration Plan

No data or deployment migration is required. Deploy the code and focused tests together. Rollback is a source revert if a valid client packet is found to violate the declared-length contract; no persisted state is changed.

## Open Questions

None for issue #348. Any protocol framing discrepancy discovered outside the confirmed exact-payload behavior is a separate follow-up.
