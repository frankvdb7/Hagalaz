# Design

## Context

See proposal.md for the remaining issue scope and transaction API decisions. `TradingCharacterScript` owns one shared session state and gate. `ItemContainerTransaction.Commit()` makes the already-mutated storage irreversible, releases mutation locks while retaining scope bindings, then runs transaction-owned domain completion and automatic publication before releasing the scope. Equipment retains its domain-owned hook policy; pouch notices follow container publication; shop events follow a successful commit.

## Goals / Non-Goals

**Goals:**

- Keep transaction completion inside one `Commit()` call, preserving existing post-unlock publication and domain effects.
- Set the terminal trade state before `Commit()` so a publication exception cannot leave permanently committed storage in a retryable session.
- Keep accept callbacks tied to the session that created them.
- Leave a deliberate safe retry state when exchange and normal refund both fail.
- Exercise the real widget callback path and lifecycle hooks with deterministic synchronization.

**Non-Goals:**

- Changing atomic storage commit, rollback, successful domain effect ordering, or the existing equipment lifecycle cleanup policy.
- Adding a second session owner, recovery mechanism, or container abstraction.
- Adding durable trade state or changing item/currency conservation rules.

## Decisions

1. **Keep commit and publication in one public operation.** `ItemContainerTransaction.Commit()` declares the already-mutated storage irreversible, releases mutation locks while retaining scope bindings, and then runs automatic completion and publication. It clears bindings and wakes waiting overlapping scopes after pending completion cleanup. Callers do not query committed state or invoke a separate publication method. A completion or publication exception propagates after storage is irreversible, and neither `Commit()` nor `Dispose()` retries or rolls back that storage.

2. **Keep pouch publication data with MoneyPouch.** MoneyPouch records the immutable facts required to publish its messages and events after container publishers. Callers do not receive or manage publication receipts, and transaction infrastructure does not become a generic event bus.

3. **Let the session owner control terminal trade completion.** `TradingCharacterScript` begins each terminal transaction with every required participant, asks `TradeExchange` to stage the economic mutations, sets `Completed` or `Cancelled`, then calls `Commit()`. `TradeExchange` does not begin or commit terminal transactions and does not own session state. A pre-commit failure rolls back on disposal; a post-commit publication failure leaves storage committed and the session terminal. Existing cleanup remains in `finally`.

4. **Keep equipment and shop effects in their owners.** Equipment owns its ordered post-commit lifecycle effects and attempt-all policy before normal participant publication. Shop sorts stock and sends its bought event only after `Commit()` returns successfully; an exception stops this sequence. Do not register arbitrary callbacks on the transaction or add a replacement callback interface.

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
