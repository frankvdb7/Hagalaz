# Design

`ItemContainerTransaction` remains the sole scope owner. Active scopes own boundary bindings and ordered boundary mutation locks. Commit first establishes irreversibility and discards snapshots, then attempts to release every boundary lock. Completion hooks and publication run outside mutation locks while bindings still identify the committed scope. Pending completion facts are discarded, after which the transaction reacquires its complete boundary set in `MutationOrder`, clears its bindings, pulses waiters, and releases locks in reverse order.

An overlapping `Begin(...)` uses the existing boundary monitor. If it sees a foreign binding, it releases its currently acquired lock set before waiting on the conflicted boundary with `Monitor.Wait` in a predicate loop, then retries deterministic acquisition from the beginning. This avoids holding a partial lock prefix while the previous scope reacquires its full set for teardown. Same-thread overlap throws immediately. No global synchronization or additional ownership object is introduced.

Rollback continues to restore snapshots while Active locks are held, discard pending facts, clear bindings, pulse waiters, and release locks. Committed completion failures retain current stop/aggregation behavior, make the transaction terminal after cleanup, and never cause rollback or retry.

`ItemContainerMutationBoundary` owns its transaction binding and validates mutation access while holding its mutation lock. `ItemContainerStorage` remains synchronization-agnostic. The boundary records changes directly into an active transaction before unlock; when no transaction is bound, it publishes after unlock. Ordinary operations never carry transaction identity, and no ambient transaction context is used. This prevents a later binding from claiming an earlier standalone mutation. Committed teardown reacquires all boundaries in `MutationOrder`; interruption is recorded and retried for the same lock until the coherent set is held, then all bindings are cleared and waiters pulsed before locks are released and failures propagated.

Terminal TradeExchange staging uses `TryTransferTo(...)` for each non-coin and recovery item and the narrowly scoped `IMoneyPouchContainer.TryTransferCoinsFrom(...)` operation for offered coins. That coin operation verifies the caller-owned transaction already includes source, pouch, and all inventory contributions, preflights existing overflow rules, then removes and adds within the same scope. It never creates a transaction. `StorageSnapshot` remains shallow with respect to item metadata: it restores slots, references, counts, and revision but not arbitrary mutable `ExtraData`.

## Risks

- A synchronous callback that attempts to mutate or re-enlist the same storage now fails while the committed scope owns it. Reads remain available and disjoint transactions remain independent.
- Waiting transactions may re-contend after wake and retry the complete ordered lock set; this is necessary to preserve the existing lock order.
