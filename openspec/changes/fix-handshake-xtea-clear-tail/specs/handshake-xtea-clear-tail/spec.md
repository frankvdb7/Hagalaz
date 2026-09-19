## ADDED Requirements

### Requirement: Revision-742 XTEA tails are decoded safely

The handshake decoders MUST copy the exact remaining XTEA section, decrypt only its complete 8-byte blocks, preserve any trailing one-to-seven clear bytes, and expose no rented capacity to the field parser.

#### Scenario: Lobby authentication contains a clear tail

- **WHEN** a valid opcode-19 authentication payload has complete XTEA blocks followed by a seven-byte clear tail
- **THEN** the lobby decoder produces a `LobbySignInRequest`

#### Scenario: World authentication contains a clear tail

- **WHEN** a valid opcode-16 authentication payload has complete XTEA blocks followed by a seven-byte clear tail
- **THEN** the world decoder produces a `WorldSignInRequest`

### Requirement: Invalid short XTEA sections remain rejected

The handshake decoders MUST reject an empty XTEA section or a section shorter than one complete XTEA block with no sign-in request.

#### Scenario: No complete XTEA block exists

- **WHEN** the remaining authentication section is shorter than eight bytes
- **THEN** decoding returns `false` with a null message

### Requirement: Existing handshake bounds remain authoritative

Exact declared packet lengths, bounded temporary buffers, truncation checks, and used-range clearing MUST remain unchanged by this compatibility correction.

#### Scenario: Exact temporary range is retained

- **WHEN** a valid handshake contains a complete XTEA section with a clear tail
- **THEN** parsing receives exactly the declared section bytes and the used temporary range is cleared before the buffer is returned
