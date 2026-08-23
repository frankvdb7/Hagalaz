## Why

Issue #348 identifies that world and lobby handshakes can parse bytes outside the current packet. The decoders use the first segment of the unread input and then expose the full capacity of rented buffers, so stale pooled bytes, incomplete sequences, and truncated fields can affect unauthenticated parsing. This is a high-severity security correctness issue because the decrypted handshake contains credentials, identifiers, tokens, and key material.

## What Changes

- Require the declared handshake packet length to match the current input exactly.
- Copy the complete encrypted XTEA payload from the unread `ReadOnlySequence<byte>` before decrypting it.
- Reject empty or non-XTEA-block-aligned encrypted payloads and expose only the exact decrypted length to the parser.
- Bound RSA and XTEA temporary readers to the number of bytes actually written or decrypted.
- Reject truncated length-prefixed settings blocks, hardware fields, and cache CRC blocks without advancing beyond the payload.
- Clear used encrypted/decrypted pooled buffer ranges before returning them to `ArrayPool<byte>`.
- Add focused regression tests for stale capacity, segmented input, malformed lengths, truncation, and cleanup behavior.

## Capabilities

### New Capabilities

- `handshake-payload-validation`: World and lobby handshake decoders parse only exact packet payloads and fail closed for malformed input.

### Modified Capabilities

None.

## Impact

The change is limited to `Hagalaz.Services.GameWorld` handshake decoders and their existing helper, plus `Hagalaz.Services.GameWorld.Tests`. It reuses `ReadOnlySequence<byte>`, `SequenceReader<byte>`, `XTEA`, `ArrayPool<byte>`, and existing decoder return-value conventions. No protocol redesign, dependency, worker, persistence, or retry mechanism is introduced.

## Acceptance Criteria

- A valid contiguous or multi-segment handshake produces the same sign-in request.
- Declared packet lengths, XTEA block lengths, strings, settings blocks, hardware fields, and cache CRCs cannot read beyond the current packet.
- Unwritten rented capacity and bytes from prior pool users cannot influence a decode.
- Used pooled ranges containing handshake data are cleared before return.
- Invalid or truncated input returns `false` with a null message and does not throw from buffer advancement.

## Stop Conditions

Stop and record a follow-up if fixing the issue requires changing post-handshake protocol framing, RSA/XTEA algorithms, authentication semantics, or introducing a general buffer-management framework.
