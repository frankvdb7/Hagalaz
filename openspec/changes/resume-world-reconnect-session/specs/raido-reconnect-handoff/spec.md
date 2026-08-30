# Raido reconnect handoff

## ADDED Requirements

### Requirement: One candidate owns the in-progress handoff

The existing reconnect transition MUST atomically claim at most one candidate
physical `ConnectionContext` by reference under its existing reconnect lock.
The claim MUST remain private to `RaidoConnectionContext` and belong to the
already-open reconnect window.

#### Scenario: Concurrent candidates race

- GIVEN two valid physical candidates and one existing reconnect window
- WHEN both attempt the handoff
- THEN exactly one owns the private in-progress claim
- AND the other sends no successful response 15

### Requirement: Flush precedes publication and resume

The winning candidate MUST write and flush response 15 before the existing
logical context installs the replacement protocol, publishes the replacement
physical transport, completes the existing reconnect waiter, or resumes normal
processing.

Successful completion requires an unambiguous successful physical write/flush
result from the candidate transport. Exceptions, cancellation, unsafe
completion/closure, or ambiguity MUST fail closed according to the concrete
transport API semantics.

#### Scenario: Failed physical flush

- GIVEN the winner's response write/flush fails, is canceled, closes, or is ambiguous
- WHEN handoff completion runs
- THEN no protocol or transport is published
- AND the candidate-owned claim is cleared only if it still belongs to that candidate

### Requirement: Stale continuations are harmless

Candidate-specific completion, failure, timeout, and cleanup MUST mutate state
only when the claim still identifies that same physical candidate.

#### Scenario: Timeout wins during flush

- GIVEN the reconnect window becomes terminal while the winner flushes
- WHEN the stale continuation returns
- THEN it cannot publish a transport, install a protocol, or complete the waiter

### Requirement: Adopted transport has one cleanup owner

After successful publication, the temporary context MUST relinquish the adopted
physical transport, advance its input boundary, and skip normal cleanup that
would read from, dispose, abort, or sign out the adopted connection.

#### Scenario: Successful adoption transfers cleanup ownership

- GIVEN the replacement transport has been published to the existing logical context
- WHEN the temporary handshake context finishes
- THEN it does not read from, dispose, abort, or sign out the adopted transport

### Requirement: Existing reconnect authority remains singular

The implementation MUST NOT add a second reconnect state machine, registry,
lease, public reservation, reservation object, application-visible ownership
abstraction, generic pending infrastructure, or second waiter.

#### Scenario: Existing transition remains the authority

- GIVEN a reconnect candidate is being handed off
- WHEN lifetime, eligibility, timeout, terminal state, or waiter completion is decided
- THEN the existing #477 reconnect transition remains the sole authority
