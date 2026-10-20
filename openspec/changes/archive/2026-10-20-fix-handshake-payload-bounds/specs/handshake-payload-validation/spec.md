## ADDED Requirements

### Requirement: Handshake decoding is bounded to the current packet

World and lobby handshake decoders MUST require the declared packet payload length to match the current input and MUST parse the encrypted and decrypted portions only within that exact payload.

#### Scenario: Exact contiguous payload is decoded

- **WHEN** a valid world or lobby handshake is provided as one contiguous sequence whose declared length matches the input
- **THEN** the decoder produces the corresponding sign-in request

#### Scenario: Exact multi-segment payload is decoded

- **WHEN** the same valid handshake bytes are split across multiple `ReadOnlySequence<byte>` segments
- **THEN** the decoder produces the same result as the contiguous input

#### Scenario: Trailing bytes are not part of the packet

- **WHEN** the declared packet length is smaller than the available input or the encrypted payload length is smaller than bytes available to a rented buffer
- **THEN** decoding fails closed and trailing or unwritten bytes cannot satisfy required fields

### Requirement: Malformed handshake input fails closed

The decoders MUST reject invalid XTEA block lengths, truncated fixed-size fields, unterminated strings, settings blocks that exceed the remaining payload, invalid hardware blocks, and missing cache CRC values without throwing or producing a sign-in request.

#### Scenario: Encrypted payload is not block aligned

- **WHEN** the encrypted XTEA payload length is zero or is not a multiple of the XTEA block size
- **THEN** decoding returns `false` with no message

#### Scenario: Length-prefixed data exceeds the payload

- **WHEN** a settings length is greater than the bytes remaining in the decrypted payload
- **THEN** decoding returns `false` without advancing beyond the payload

#### Scenario: Required field or cache CRC is truncated

- **WHEN** any required field, hardware value, or expected cache CRC is missing
- **THEN** decoding returns `false` with no partially constructed message

### Requirement: Sensitive temporary data is cleared

Handshake parsing MUST clear the used ranges of pooled buffers that contain encrypted or decrypted handshake data before returning those buffers to the pool.

#### Scenario: Decode succeeds

- **WHEN** a handshake containing credentials, identifiers, tokens, or key material is decoded
- **THEN** the used temporary buffer ranges are cleared before pooled reuse

#### Scenario: Decode fails after temporary data is populated

- **WHEN** malformed input causes parsing to fail after a temporary buffer has been populated
- **THEN** the used temporary buffer ranges are still cleared before pooled reuse
