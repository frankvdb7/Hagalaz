# Proposal

## Why

`ItemContainerTransaction.Commit()` makes storage irreversible before invoking domain completion and publication callbacks, but currently lets their arbitrary exceptions escape. Callers therefore cannot reliably tell a committed operation from an earlier failure, which risks incorrect economic follow-up.

## What Changes

- **BREAKING:** Add sealed `ItemContainerPostCommitCompletionException` and `ItemContainerPostCommitPublicationException` types. Each directly derives from `Exception`, guarantees transaction storage is already committed, and preserves the exact callback exception as `InnerException`.
- Wrap only transaction-owned completion and publication callbacks. Preserve their current fail-fast ordering; do not roll back or retry committed storage. Leave validation, pre-commit, synchronization, tracker, lock-release, and standalone publication exception behavior unchanged.
- Keep trade terminal cleanup and propagation behavior. In duel victory, log this typed committed outcome and finish the existing victory flow rather than treating it as rejected settlement.
- Add transaction, trade, MoneyPouch, and duel regressions for the new contract and existing rollback behavior.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `item-container-storage`: Define typed completion/publication failures from transaction commit and the committed-storage guarantee.
- `trading-completion`: Specify typed propagation for transaction-owned post-commit failures while preserving terminal cleanup.

## Impact

The public API change is in `Hagalaz.Game.Abstractions`. Production behavior changes only in `ItemContainerTransaction.Commit()` and the duel victory caller that must safely finish after committed publication failure. Tests and current OpenSpec documents will be updated. No dependencies or transaction infrastructure are added.

## Acceptance Criteria

- A typed exception from `Commit()` always means storage is irreversible; its type identifies the phase in which the owner reports the failure, and its inner exception is the exact exception reported by the owner. Owners may defer reporting a completion failure until after required publication.
- Pre-commit failures remain unwrapped and disposal rolls back; callback failures remain fail-fast and disposal does not replay callbacks or roll back.
- Trade stays terminal after committed callback failures. Duel logs a typed post-commit failure and finishes victory cleanup; a rejected pre-commit payout remains retryable.
- Focused tests, solution build, strict OpenSpec validation, duplication checks, and diff validation pass, with unavailable checks reported accurately.

## Non-goals and Stop Conditions

Do not change transaction ordering, standalone publication behavior, caller-visible transaction state, ordinary capacity rejection, retry or recovery policy, or trade/duel state architecture. If a failure occurs outside the two transaction-owned callback phases or requires a new lifecycle mechanism, stop and record it as separate work instead of widening this change.
