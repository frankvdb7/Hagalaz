# Proposal

## Why

An atomic trade can commit recipient and escrow storage, then throw while publishing container changes; the current script can mistake that committed exchange for a failure and leave the session active. A failed exchange followed by a refund that cannot fit also leaves both final confirmations active, while an old widget callback can target a later session.

## What Changes

- Keep trade completion, refund, and forced escrow recovery inside the existing `ItemContainerTransaction` and shared session gate.
- Let the trade lifecycle observe whether terminal storage committed, set its terminal state before publication, and guarantee cleanup in `finally` when publication throws.
- Reset both acceptances and accepted revisions if an exchange and its ordinary refund both fail, leaving escrow available for a fresh confirmation or later cancellation.
- Bind accept callbacks to the session that created their widget, so stale callbacks cannot mutate a later trade.
- Add deterministic regressions for publication failure, failed refund, final-accept handlers, stale callbacks, and completion races with destroy or interruption.
- Replace transaction commit-callback registration and the commit-status overloads with one explicit storage-commit operation and one publication operation. Migrate the existing equipment, pouch, shop, and economic callers while retaining successful ordering and domain behavior.
- Simplify transaction, pouch, shop, and trade publication to ordinary exception propagation: the first failure stops later publication. Remove exception collection, aggregation, and stored publication failures from this path, as requested by the user.

## Capabilities

### New Capabilities

- `trading-completion`: terminal trade lifecycle, confirmation, refund, and committed-publication behavior.

### Modified Capabilities

- `item-container-storage`: explicit commit and publication phases, with domain effects executed by their owners.

## Impact

This affects `TradingCharacterScript`, `TradeExchange`, `ItemContainerTransaction`, and the existing economic callers that use pouch changes or equipment/shop effects. The session gate, storage locks, snapshots, rollback, refund/recovery operations, domain publishers, and common session cleanup are reused. The user explicitly extended this change to simplify the transaction API after reviewing the callback and overload approaches. No package, persistence, or protocol changes are required.

## Acceptance Criteria

- A committed exchange, refund, or forced recovery reaches its matching terminal lifecycle state before publication and cleans up exactly once in `finally` even when publication throws; the original exception propagates directly.
- An uncommitted failed exchange can refund normally, or remains active with both acceptances and accepted revisions cleared if refund cannot fit.
- Final accept callbacks operate only on the session that registered them; concurrent final accepts complete one exchange, and stale callbacks cannot affect a later session.
- Completion racing with owner/target destruction or interruption produces one conserved outcome.
- The transaction has one commit method and one publication method, exposes its committed status, and has no arbitrary post-commit callback registration or overloads.
- Successful pouch messages/events, equipment effect ordering, shop events, and exact rollback are preserved through explicit domain calls. Publication stops at its first exception without undoing committed storage or retrying notifications.

## Non-goals and Stop Conditions

- Do not rebuild the existing session gate, storage mechanics, rollback, or recovery path.
- Do not add transaction frameworks, new coordinators, durable trade persistence, a trade ledger, or unrelated container changes.
- The transaction API revision is limited to commit/publication and migration of its current consumers. Stop if another lifecycle owner, retry path, or transaction framework becomes necessary.
- Existing equipment lifecycle cleanup remains owned by EquipmentContainer; the simplification concerns transaction and economic publication, not that separate lifecycle policy.
