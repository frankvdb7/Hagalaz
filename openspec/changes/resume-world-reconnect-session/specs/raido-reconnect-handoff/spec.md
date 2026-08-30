# Raido reconnect handoff

## ADDED Requirements

### Requirement: Existing publication occurs after message consumption

The reconnect integration MUST leave the existing dispatcher contract unchanged
and MUST invoke the existing reconnect publication operation only after the
temporary reader has advanced the consumed request with `Advance(true)`.

#### Scenario: Deferred publication

- GIVEN a temporary context has dispatched a valid reconnect request
- WHEN the dispatch completes
- THEN the reader advances the consumed request
- AND the existing reconnect publication operation is invoked afterward

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

A failed or rejected deferred publication MUST NOT publish a replacement
transport or protocol, and MUST leave the temporary physical connection on the
normal failure and cleanup path.

#### Scenario: Publication loses the existing race

- GIVEN another candidate has already published the reconnect window
- WHEN the deferred publication for this candidate runs
- THEN it fails through the existing reconnect checks
- AND the candidate is cleaned up as a normal temporary connection

### Requirement: Successful transfer preserves the accepted physical lifetime

After successful publication, temporary logical cleanup MUST complete without
aborting the transferred physical connection. The accepted physical connection
MUST remain awaited until its existing `ConnectionClosed` signal completes.

#### Scenario: Temporary lifetime ends after transfer

- GIVEN the existing publication operation has attached the candidate transport
- WHEN the temporary handler finishes its logical cleanup
- THEN it does not abort the attached transport
- AND the accepted physical lifetime remains active until closure

### Requirement: #477 remains the sole reconnect authority

The integration MUST NOT add a second reconnect state machine, registry, waiter,
timeout, lease, reservation model, transport ownership abstraction, or generic
pending infrastructure.

#### Scenario: Existing lifecycle remains authoritative

- GIVEN a reconnect candidate is processed by the GameWorld integration
- WHEN the existing reconnect publication operation accepts or rejects it
- THEN #477 remains responsible for reconnect-window, timeout, terminal, waiter,
  and publication behavior
