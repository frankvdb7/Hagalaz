## Purpose

This capability lets GameWorld preserve whether a revision-742 client requested a fresh world login or a world reconnect, without implementing reconnect session recovery.

## ADDED Requirements

### Requirement: World handshake intent is explicit

The GameWorld handshake MUST preserve the distinction between opcode 16 fresh world login and opcode 18 world reconnect after decoding.

#### Scenario: Fresh world login

- **WHEN** a valid revision-742 world handshake arrives with opcode 16
- **THEN** decoding produces the existing fresh-login request type and the application uses the existing world sign-in path

#### Scenario: World reconnect

- **WHEN** a valid revision-742 world handshake arrives with opcode 18
- **THEN** decoding produces a distinct reconnect request type containing the same verified handshake fields sent by the client, and application dispatch selects the reconnect branch

### Requirement: Reconnect payload uses only verified fields

The reconnect request MUST use the revision-742 world handshake payload layout and MUST NOT require a synthetic reconnect token, session identifier, or other field absent from the client payload.

#### Scenario: Shared world handshake fields

- **WHEN** opcode 16 and opcode 18 carry valid client payloads with the same revision-742 layout
- **THEN** both decoded requests expose the same login, password, seed, cache, client, display, and size fields, differing only in typed intent

### Requirement: Unsupported reconnect is safe

The application MUST reject a reconnect request with the existing safe failure response and terminate the handshake connection until reconnect session recovery is implemented.

#### Scenario: Reconnect recovery is not implemented

- **WHEN** a decoded reconnect request reaches the GameWorld hub
- **THEN** the caller receives the existing failed sign-in response and the connection is aborted without authenticating or resuming a character

### Requirement: Malformed reconnect input fails closed

The reconnect decoder MUST apply the same required-field, length, and cache validation limits as the existing world handshake decoder.

#### Scenario: Truncated reconnect payload

- **WHEN** a reconnect payload is truncated or contains an invalid variable-length field
- **THEN** decoding returns `false` with no application message and does not dispatch a reconnect request
