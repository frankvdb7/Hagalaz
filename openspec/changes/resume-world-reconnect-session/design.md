# Design

## Ownership boundaries

`PasswordGrantCommandConsumer` remains the sole credential-validation owner.
The tokenless authorization consumer calls it with empty scopes, returns the
subject and existing validation outcomes, and never issues or looks up tokens.
`AuthenticationService` runs that request through the existing sign-in
resilience/rate-limit pipeline and resolves the subject to the master ID without
installing temporary authentication features.

`HandshakeHub` owns only reconnect request preflight, authentication, existing
session lookup, and identity ownership verification. It must not reproduce
Raido's reconnect eligibility or lifetime checks. The existing session's
authentication subject, session master ID, and character master ID must all
match the authenticated master ID.

`RaidoConnectionContext` remains the sole reconnect authority. It retains the
existing reconnect window, lock, waiter, timeout, terminal behavior, and
physical publication transition. The only added reconnect-related state is a
single private claim identifying the owning replacement physical
`ConnectionContext` by reference, with the replacement protocol associated with
that in-progress transition. There is no public reservation or separate claim
object, registry, lease, state machine, or waiter.

## Handoff ordering

The handler resolves and verifies ownership before attempting the Raido seam.
Under `_reconnectLock`, the existing window is validated and the candidate is
atomically claimed. The lock is released while the winner writes and flushes
response 15 through the temporary context using the smallest existing/internal
physical write path that exposes an unambiguous physical success/failure
result. Exceptions, cancellation, closure/completion that makes continuation
unsafe, and ambiguous results fail closed.

After a successful flush, the existing transition verifies that the same
physical candidate still owns the claim and that #477 has not become terminal.
The replacement protocol is then installed on the stable logical context,
the replacement physical callbacks/transport are published, the private claim
is cleared, and the existing reconnect waiter is completed. Normal logical
processing can resume only after all three handoff effects are complete.

The temporary reader advances its consumed boundary before publication when the
handoff occurs during dispatch. It then relinquishes the adopted transport and
skips normal disconnect cleanup for that physical connection. Failed and
rejected temporary contexts retain normal cleanup.

## Failure and stale continuation rules

Every candidate-specific completion, failure, cancellation, timeout, and
cleanup path compares the stored claim with the same physical candidate under
the existing reconnect lock. A stale continuation is a no-op: it cannot clear
a newer claim, install a protocol, publish a transport, complete the waiter,
or change session/character state.

If the flush fails, the candidate-owned claim is cleared or invalidated through
the existing reconnect transition. Another candidate can proceed only while
the original #477 window remains valid. If timeout or abort wins, #477 terminal
behavior remains authoritative; no separate claim timeout or timeout extension
is added. A terminal transition invalidates the claim and completes the one
existing waiter with its existing result.

## Protocol and response

`WorldReconnectResponse` uses fixed opcode 15 and variable-short framing. Its
payload is exactly the revision-742 4,608-byte player-entry payload. The
player-entry bit serialization is shared with the existing standard-map encoder
without changing standard-map output. The new client protocol is configured
with the request ISAAC seed and installed on the resumed logical context,
never on the temporary handshake context.

## Rejected alternatives

- Sending response 15 before claiming is rejected because concurrent candidates
  could both receive success.
- Holding the reconnect lock over asynchronous network I/O is rejected because
  it blocks timeout/abort and violates the existing lock's synchronous state
  transition role.
- Publishing before response flush or protocol installation is rejected because
  resumed reads/writes could use the wrong protocol or overtake the handshake.
- A second reservation, lease, registry, waiter, state machine, or generic
  transport writer is rejected as duplicate ownership or unnecessary scope.
