## Why

The revision-742 Java client encrypts only complete 8-byte XTEA blocks. Its authentication payload can therefore contain a short clear tail after the encrypted blocks. The handshake-boundary hardening added exact buffer bounds but also rejected every non-block-aligned payload, so the real client is disconnected before credentials reach the authentication service.

## What Changes

- Keep the exact packet and pooled-buffer bounds introduced by handshake validation.
- Accept a non-empty XTEA section containing complete encrypted blocks followed by at most seven clear bytes.
- Decrypt only the complete blocks while exposing the exact copied payload, including the clear tail, to the existing world and lobby field parsers.
- Add helper and decoder regressions for the revision-742 clear-tail framing.

## Non-Goals

- Changing the RSA or XTEA algorithms, authentication decisions, cache CRC semantics, or post-handshake protocol.
- Relaxing declared packet-length validation or pooled-buffer cleanup.
- Adding a second parser, retry path, or protocol abstraction.

## Acceptance Criteria

- A revision-742-shaped payload with a clear tail is accepted by both world and lobby decoders.
- The XTEA helper decrypts complete blocks and preserves the clear tail unchanged.
- Empty payloads and payloads shorter than one complete XTEA block remain rejected.
- Existing exact-bounds, truncation, segmented-input, and sensitive-buffer cleanup tests remain passing.

## Affected Runtime Boundary

`Hagalaz.Services.GameWorld` handshake decoding for the initial opcode-16 world request and opcode-19 lobby request.
