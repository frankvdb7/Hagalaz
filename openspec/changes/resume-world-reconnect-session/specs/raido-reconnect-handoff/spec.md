# Raido reconnect handoff

## ADDED Requirements

### Requirement: Only the existing Raido connection owns handoff eligibility

The system MUST use the existing detached logical Raido connection and its
reconnect window. It MUST NOT add a second reconnect registry, claim, lease,
waiter, or state machine.

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
connection ID, features, items, and logical state.

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
