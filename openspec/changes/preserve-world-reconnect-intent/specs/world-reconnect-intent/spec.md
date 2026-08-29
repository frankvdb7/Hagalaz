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

The characterization artifacts MUST distinguish observed client requests and trace facts, controlled-peer stimuli, client-side observations, client-code facts, and unknown production-server behavior. None of the first four categories MAY be promoted to a production-server requirement without new evidence.

The fixture MUST keep these evidence categories distinct:

1. Observed client requests and trace facts: handshake opcode 14, authentication opcode 16 with flags 0 and 1, and no authentication-header opcode 18 in the characterized reconnect trace.
2. Controlled-peer stimuli: fresh-login response 2 and reconnect response 15 supplied by the controlled peer.
3. Client-side observations: world-entry read sizes, authentication reset, protocol preservation, temporary-key clearing, fresh client/server ISAAC instances, RSA/XTEA boundaries, server-key `+50`, and client-observed event ordering.
4. Client-code facts: authentication opcode 18 is absent from the revision-742 authentication protocol registry, and a separate ISAAC-framed game-channel opcode 18 exists without being claimed as observed in the reconnect trace.
5. Unknown production-server behavior: production acceptance, response/payload ordering, cipher, authentication/session ownership, transport handoff/adoption, lifecycle, resumed I/O, and resynchronization behavior.

#### Scenario: Request and observation evidence stays separated

- **WHEN** the fixture records revision-742 reconnect evidence
- **THEN** it records opcode 16 with flags 0 and 1 as observed client request facts
- **AND** it records `authentication.header.opcode18.observed=false` as a trace observation
- **AND** it records `controlled.peer.fresh.response=2` and `controlled.peer.reconnect.response=15` as controlled-peer stimuli
- **AND** it qualifies the 4,656-byte and 4,608-byte world-entry reads as client-side observations
- **AND** it records `authentication.header.opcode18.presentInRegistry=false` as a client-code fact
- **AND** it records the separate ISAAC-framed game-channel opcode 18 without claiming it appeared in the authentication trace

### Requirement: Unverified production reconnect behavior remains deferred

The change MUST NOT define production-server requirements for response 15, response/adoption ordering, cipher transitions, authentication/session ownership, transport handoff, lifecycle behavior, resumed reads/writes, or resynchronization.

#### Scenario: Missing production evidence remains a follow-up

- **WHEN** no production-server evidence establishes a reconnect behavior
- **THEN** the fixture and specification leave that behavior unknown
- **AND** no production implementation is inferred from controlled-peer or client-code evidence
