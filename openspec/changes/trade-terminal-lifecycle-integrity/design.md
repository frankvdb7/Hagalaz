# Design

## Context

See proposal.md for the remaining issue scope and the user-requested transaction API simplification. `TradingCharacterScript` owns one shared session state and gate. The existing transaction combines storage commit, participant publication, and arbitrary domain callbacks, and the attempted commit-status fix added overloads. Equipment requires effects before publication; pouch notices and shop events follow participant publication.

## Goals / Non-Goals

**Goals:**

- Replace transaction commit-callback registration and execution overloads with explicit commit and publication phases, preserving existing domain effects.
- Let the existing session owner distinguish a committed terminal storage operation from an uncommitted failure that may be retried or refunded.
- Keep accept callbacks tied to the session that created them.
- Leave a deliberate safe retry state when exchange and normal refund both fail.
- Exercise the real widget callback path and lifecycle hooks with deterministic synchronization.

**Non-Goals:**

- Changing atomic storage commit, rollback, successful domain effect ordering, or the existing equipment lifecycle cleanup policy.
- Adding a second session owner, recovery mechanism, or container abstraction.
- Adding durable trade state or changing item/currency conservation rules.

## Decisions

1. **Separate storage commit from publication.** `ItemContainerTransaction.TryCommit` is the sole execution method and retains deterministic locking, snapshots, staging, and rollback. Its read-only `Committed` property becomes true only after successful storage commit. `PublishChanges` runs the existing participant publishers after unlock, in order, at most once. Its first exception propagates directly and stops later publication. A failed/unexecuted transaction or repeated publication has no publication effect. Remove `OnCommitted`, `OnCommittedBeforePublish`, and all execution overloads; callers execute named domain work directly at the required phase.

2. **Keep economic publication facts with the pouch owner.** Pouch staging returns a small immutable `MoneyPouchChange` value on success, including the previous count and established message amount; null means staging failed. This value contains no arbitrary delegate. A pouch-specific publication method runs container publishers and committed pouch notices/events in order with ordinary exception propagation. This centralizes the repeated storage-plus-pouch publication sequence used by trade, duel, bank, shop, and the pouch itself without adding a generic effects framework or a second state store.

3. **Publish trade only after its owner marks it terminal.** Each terminal `TradeExchange` method commits storage and returns its transaction and immutable pouch publication facts. The result exposes committed status and explicit publication, with no exception field or captured failure. The session owner marks the trade Completed or Cancelled before requesting publication and guarantees its existing cleanup through `finally`. Exceptions propagate normally. Remove callback/output overloads and retain one method per named trade operation. Pre-commit exceptions retain rollback and restore the Completing session to Active.

4. **Keep equipment and shop effects in their owners.** Equipment executes its existing lifecycle methods and profile updates after commit using its existing lifecycle cleanup helper, then requests transaction publication. Shop purchasing directly publishes, sorts stock, and sends the bought event in order; an exception stops this sequence. Do not register these operations on the transaction or add a replacement callback interface.

5. **Keep retry state in the existing session.** When completion and ordinary refund fail, the owner clears both acceptances and accepted revisions, then refreshes the current confirmation UI. Escrow remains in its existing offer containers. No new state or recovery path is needed; future completion requires both callbacks to accept the same unchanged offer revisions again.

6. **Capture session identity in accept handlers.** Both offer-stage and final-confirmation callbacks pass their captured session into the owner method. The existing `IsActiveSession` check rejects callbacks after that session closes, including when another trade has since started. The shared gate remains the sole serialization mechanism for final accepts and lifecycle callbacks.

7. **Test production entry points with existing fixtures.** Capture the click handlers attached to the real trade widgets and invoke both final handlers behind a barrier. Use the test containers' post-commit publication hook to throw after storage commit, and barriers to race `FinishTradeSession` with owner destroy, target destroy, and interruption. Keep conservation assertions across recipients, escrow, and recovery destinations.

## Risks / Trade-offs

- [Risk] A publication failure is still visible to the caller after the economic operation commits. → Mark the lifecycle terminal before publication and guarantee cleanup in `finally`.
- [Trade-off] The first publication failure skips later notifications and shop effects. → Propagate that original error without retrying or undoing committed storage; this is the user-approved simplification of the earlier attempt-all policy.
- [Risk] If both exchange and refund fail, the session remains open with conserved escrow. → Clear confirmation state and require fresh acceptance; existing later cancel/destroy paths remain responsible for refund or recovery.
- [Risk] Mocked widgets can obscure whether tests cover production callbacks. → Capture handlers through `AttachClickHandler` during `StartTradeSession` and the confirmation-stage transition rather than invoking private acceptance methods directly.

## Migration Plan

No data or protocol migration is needed. Deploy the transaction API and its migrated callers together. The storage model and persisted data do not change.
