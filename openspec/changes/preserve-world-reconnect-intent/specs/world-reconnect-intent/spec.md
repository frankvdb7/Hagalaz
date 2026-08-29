## Purpose

Preserves the verified revision-742 world reconnect request intent without implementing the unverified production reconnect flow.

## ADDED Requirements

### Requirement: Revision-742 world authentication uses opcode 16

The revision-742 authentication protocol registration MUST register the world handshake decoder for opcode 16 and MUST NOT register an authentication decoder for opcode 18.

#### Scenario: Authentication registration reflects the verified client contract

- **WHEN** the revision-742 authentication protocol is configured
- **THEN** opcode 16 resolves to `WorldHandshakeRequestDecoder`
- **AND** authentication opcode 18 has no decoder registration
- **AND** the separate ISAAC-framed game-channel opcode 18 remains outside this authentication registration

### Requirement: The opcode-16 reconnect flag becomes explicit request intent

The world handshake decoder MUST interpret the existing opcode-16 boolean as the reconnect discriminator without duplicating the RSA/XTEA parser or changing the common request fields.

#### Scenario: Fresh world authentication preserves the existing request type

- **WHEN** opcode 16 carries reconnect flag 0
- **THEN** the decoder produces `WorldSignInRequest`
- **AND** the existing request fields and segmented-input behavior remain unchanged

#### Scenario: Reconnect authentication is represented separately

- **WHEN** opcode 16 carries reconnect flag 1
- **THEN** the decoder produces `WorldReconnectRequest`
- **AND** the decoded message is not a `WorldSignInRequest`
- **AND** no reconnect handler, response, or authentication orchestration is invoked

### Requirement: Reconnect intent cannot invoke fresh-world sign-in

The request-side correction MUST prevent `WorldReconnectRequest` from being dispatched to the handler registered for `WorldSignInRequest`.

#### Scenario: Exact concrete-type dispatch ignores the unhandled reconnect message

- **WHEN** a concrete message type has no exact Raido handler descriptor
- **THEN** the dispatcher does not invoke a handler registered for a different base message type
- **AND** the reconnect message remains intentionally unhandled until issue #478 establishes its production behavior

### Requirement: Characterization evidence remains qualified

The characterization artifacts MUST preserve the verified request facts and controlled-peer/client observations without promoting them to production-server requirements.

The fixture MUST keep these evidence categories distinct:

1. Controlled-peer inputs: the handshake and authentication-header opcode/flag values supplied during the controlled run.
2. Client-side observations: response 15, world-entry bytes, authentication reset, protocol/ISAAC/key observations, RSA/XTEA boundaries, and client-observed event ordering from that run.
3. Facts discovered from client code: authentication-header opcode 18 was not observed in the characterized trace, and a separate ISAAC-framed game-channel opcode 18 exists without being claimed as observed in that trace.
4. Unknown production-server behavior: production acceptance, ordering, cipher, authentication, session, transport, lifecycle, resumed I/O, and resynchronization behavior.

#### Scenario: Request and observation evidence stays separated

- **WHEN** the fixture records revision-742 reconnect evidence
- **THEN** it records opcode 16 with flags 0 and 1 as request facts
- **AND** it records `authentication.header.opcode18.presentInRegistry=false`
- **AND** it records `authentication.header.opcode18.observed=false`
- **AND** it qualifies response 15 and the 4,608-byte world-entry read as controlled-peer/client observations
- **AND** it records the separate ISAAC-framed game-channel opcode 18 without claiming it appeared in the authentication trace

### Requirement: Unverified production reconnect behavior remains deferred

The change MUST NOT define production-server requirements for response 15, response/adoption ordering, cipher transitions, authentication/session ownership, transport handoff, lifecycle behavior, resumed reads/writes, or resynchronization.

#### Scenario: Missing production evidence remains a follow-up

- **WHEN** no production-server evidence establishes a reconnect behavior
- **THEN** the fixture and specification leave that behavior unknown
- **AND** no production implementation is inferred from controlled-peer or client-code evidence
