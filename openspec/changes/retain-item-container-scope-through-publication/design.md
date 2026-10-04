# Design

`ItemContainerTransaction` remains the sole scope owner. Active scopes own both bindings and ordered mutation locks. Commit first establishes irreversibility and discards snapshots, then attempts to release every mutation lock. Completion hooks and publication run outside mutation locks while bindings still identify the committed scope. Pending completion facts are discarded, after which the transaction reacquires its complete storage set in `MutationOrder`, clears its bindings, pulses waiters, and releases locks in reverse order.

An overlapping `Begin(...)` uses the existing storage monitor. If it sees a foreign binding, it releases its currently acquired lock set before waiting on the conflicted storage with `Monitor.Wait` in a predicate loop, then retries deterministic acquisition from the beginning. This avoids holding a partial lock prefix while the previous scope reacquires its full set for teardown. Same-thread overlap throws immediately. No global synchronization or additional ownership object is introduced.

Rollback continues to restore snapshots while Active locks are held, discard pending facts, clear bindings, pulse waiters, and release locks. Committed completion failures retain current stop/aggregation behavior, make the transaction terminal after cleanup, and never cause rollback or retry.

## Risks

- A synchronous callback that attempts to mutate or re-enlist the same storage now fails while the committed scope owns it. Reads remain available and disjoint transactions remain independent.
- Waiting transactions may re-contend after wake and retry the complete ordered lock set; this is necessary to preserve the existing lock order.
