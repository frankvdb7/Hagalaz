# Design

## Context

See [proposal.md](proposal.md) for motivation and [specs](specs) for the public guarantee. `Commit()` already crosses the irreversible state transition before invoking completion owners, and `Dispose()` observes that state so it cannot roll storage back.

## Goals / Non-Goals

**Goals:**

- Make the irreversible outcome explicit when a transaction-owned completion or publication callback throws.
- Preserve callback order, fail-fast behavior, exact inner exception identity, and existing committed-state cleanup.
- Let the duel caller finish its existing victory flow for a typed post-commit failure while continuing to surface/report the failure.

**Non-Goals:**

- Do not wrap errors from transaction validation, snapshot/tracker work, synchronization, or lock release.
- Do not alter direct standalone publication exceptions, Equipment's attempt-all hook policy, or trade's current terminal transitions.
- Do not add a result type, public committed state, retry, compensation, or transaction coordination abstraction.

## Decisions

1. Add exactly two public sealed exception types in the collections namespace: `ItemContainerPostCommitCompletionException` and `ItemContainerPostCommitPublicationException`. Both derive directly from `Exception`, accept the original callback exception, and preserve that exact instance as `InnerException`. Do not add a shared base type or phase enum.
2. In `Commit()`, place a narrow `try`/`catch` around each completion-owner phase. Wrap the first thrown `Exception` and let it stop the current loop and later phases. Do not wrap the rest of `Commit()`.
3. Leave `Dispose()` and transaction state sequencing unchanged. After a typed post-commit failure, ordinary disposal performs existing committed cleanup, does not restore snapshots, and does not replay owner callbacks.
4. Trade already marks `Completed`/`Cancelled` before `Commit()` and resets the session in `finally`; those paths should propagate the typed exception without further lifecycle changes. Duel catches only the typed exception at the victory boundary, logs it with the original exception in its inner chain, then continues the existing respawn and victory presentation. A `false` payout result and pre-commit exceptions retain their existing behavior.
5. Keep phase classification at the `IItemContainerCompletionOwner` method boundary. The exception type describes the phase in which the owner reports its failure, not necessarily its underlying cause. Equipment internally attempts its queued hooks and then publishes before surfacing retained hook failures from `PublishCommittedChanges()`; that existing behavior is preserved, so a hook failure is a publication-phase failure even when Equipment publication succeeds. No caller currently branches on these types separately; callers that need the cause inspect `InnerException`.

## Risks / Trade-offs

- **Callers that assert an exact callback exception type will observe a wrapper** → update only tests/callers for transaction-owned callbacks and assert the exact original through `InnerException`.
- **An untyped exception from internal transaction mechanics does not prove whether storage committed** → keep those failures unclassified as required; only the new exception provides the positive commit guarantee.
- **Duel must not let a committed publication failure abort death completion** → catch only the typed exception, log it, and continue the already-established victory path; keep capacity rejection (`false`) distinct.

## Migration Plan

No persisted data or deployment migration is required. Build and test the affected abstractions, GameWorld, and Scripts projects; validate the full solution and OpenSpec before updating the PR description and pushing the change.
