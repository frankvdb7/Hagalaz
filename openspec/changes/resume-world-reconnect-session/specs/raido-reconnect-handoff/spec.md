# Raido reconnect handoff

## ADDED Requirements

### Requirement: Existing publication reuses the temporary physical connection

The reconnect integration MUST leave the existing dispatcher and handler
contracts unchanged and MUST invoke the existing reconnect publication
operation with the temporary connection's current physical transport.

#### Scenario: Existing publication

- GIVEN a temporary context has handled a valid reconnect request
- WHEN the existing reconnect publication operation is invoked
- THEN it receives the temporary connection's current physical transport

### Requirement: Replacement protocol is published atomically

The existing reconnect publication operation MUST install the supplied replacement
protocol before publishing the replacement physical connection and before
completing the existing reconnect waiter.

#### Scenario: Replacement protocol

- GIVEN a valid reconnect window and replacement physical connection
- WHEN existing publication succeeds with a replacement protocol
- THEN the logical context uses that protocol
- AND the replacement physical connection is current
- AND the existing reconnect waiter completes only after both effects

### Requirement: Failed publication uses existing cleanup

A failed or rejected publication MUST NOT publish a replacement
transport or protocol, and MUST leave the temporary physical connection on the
normal failure and cleanup path.

#### Scenario: Publication loses the existing race

- GIVEN another candidate has already published the reconnect window
- WHEN publication for this candidate runs
- THEN it fails through the existing reconnect checks
- AND the candidate is cleaned up as a normal temporary connection

### Requirement: #477 remains the sole reconnect authority

The integration MUST NOT add a second reconnect state machine, registry, waiter,
timeout, lease, reservation model, transport ownership abstraction, or generic
pending infrastructure.

#### Scenario: Existing lifecycle remains authoritative

- GIVEN a reconnect candidate is processed by the GameWorld integration
- WHEN the existing reconnect publication operation accepts or rejects it
- THEN #477 remains responsible for reconnect-window, timeout, terminal, waiter,
  and publication behavior
