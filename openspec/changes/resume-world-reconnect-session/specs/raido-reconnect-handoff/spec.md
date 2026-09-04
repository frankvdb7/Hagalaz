# Raido reconnect handoff

## ADDED Requirements

### Requirement: Only the existing Raido connection owns handoff eligibility

The system MUST use the existing detached logical Raido connection and its
reconnect window and detach/attach lifecycle. It MUST NOT add a second
reconnect registry, claim, lease, waiter, candidate state machine, or winner
state machine. Reconnect enabling MUST be exposed through the stable logical
connection's `IRaidoStatefulReconnectFeature`, not through `RaidoCallerContext`.

#### Scenario: Concurrent candidates have one winner

- GIVEN two valid candidates for one detached logical connection
- WHEN both attempt handoff
- THEN exactly one transfers the physical socket
- AND the loser cannot alter the winner's transport, protocol, session, or
  character

### Requirement: Physical ownership changes atomically

The candidate MUST stop owning and reading the physical socket before the
target starts reading it. After success, candidate cleanup MUST NOT abort,
dispose, or write to the transferred socket. The target MUST retain its stable
logical connection ID, features, items, and logical state. The replacement
physical `ConnectionContext.ConnectionId` MUST NOT be overwritten.

#### Scenario: Immediate post-response input reaches the target

- GIVEN the reconnect response has been flushed
- WHEN the client sends its first fresh-ISAAC game packet immediately
- THEN only the resumed target logical context observes that packet

### Requirement: Protocol transition is fresh and ordered

The target MUST install a fresh revision-specific protocol seeded from the
reconnect request before resumed game input is parsed. The old target protocol
scope MUST be disposed exactly once, and a failed candidate MUST dispose its
new scope locally.

#### Scenario: Reconnect response is unencrypted and freshly seeded

- GIVEN a valid revision-742 reconnect
- WHEN response 15 is sent
- THEN it uses plain two-byte handshake length framing and is not ISAAC-framed
- AND subsequent game packets use a fresh incoming ISAAC seed and outgoing
  seed-plus-50 state

### Requirement: Handoff cancellation and commit ordering are explicit

The candidate cancellation token MAY cancel the operation before physical
ownership transfer. After the target owns the physical socket, that token MUST
NOT cancel the target-owned protocol installation, response write, flush, or
existing reconnect waiter completion. Response 15 MUST be written and flushed
after the target accepts the replacement and installs the fresh protocol, and
the reconnect waiter MUST complete only after that flush succeeds.

#### Scenario: Candidate cancellation before transfer

- GIVEN the candidate's `ConnectionAborted` token is already canceled
- WHEN the candidate attempts handoff
- THEN the target remains detached and eligible
- AND the candidate cannot alter target protocol or transport state

#### Scenario: First game packet uses the target protocol

- GIVEN response 15 has flushed and the existing reconnect waiter has completed
- WHEN the replacement socket immediately receives one game packet
- THEN the target's fresh protocol reader dispatches it
- AND the candidate handshake protocol does not observe it
