# Design

## Context

See [proposal.md](proposal.md) for motivation and scope. The existing design uses thread-affine mutation boundaries, explicit transactions, direct standalone two-boundary transfer ownership, and domain-owned Equipment and MoneyPouch completion. Keep those mechanisms and their publication ordering.

## Goals / Non-Goals

**Goals:**

- Prevent a thread holding one storage scope from blocking while it acquires another contended boundary.
- Preserve current foreign-thread waiting when the caller owns no storage.
- Keep Equipment recovery in Equipment code and use existing inventory, Rewards, and Bank containers.
- Make MoneyPouch's existing standalone-only command contract fail early and visible.
- Avoid revisions and publications for an empty clear.

**Non-Goals:**

- Transaction propagation, new transaction abstractions, or async ownership.
- Replacing direct standalone transfer with an explicit transaction.
- New compensation destinations or guarantees when all current destinations reject.
- Changes to unrelated domain ordering or public API shape.

## Decisions

### Use one thread-local ownership count only as a blocking guard

Increment the count once after a transaction has bound all resolved boundaries, retain it through publication, and release it when bindings are cleared. Standalone publication claims contribute one per claimed boundary and release in existing cleanup. Acquisition checks the count before entering additional locks: use immediate acquisition and reject contention/foreign logical ownership if positive; retain existing ordered wait/retry behavior otherwise. Same-transaction lock borrowing and transaction discovery remain boundary-local.

Rejected alternative: a thread-local transaction registry or ambient transaction lookup. Existing boundary bindings already identify participation; only a boolean-like count is needed to decide whether blocking can deadlock.

### Restore Equipment items with existing containers

Keep the interactive unequip command outside transactions and mutation locks. Exact-remove the incoming item, compensate on a false/throwing unequip or failed pre-equip insertion, and check the item is neither already in Equipment nor restored before attempting recovery. Try its original inventory slot, ordinary inventory insertion, Rewards, then Bank. Once inserted in Equipment, lifecycle/publication failures do not trigger compensation because the storage mutation is already committed.

Rejected alternative: wrapping UI/script callbacks in a transaction or adding a generic compensation framework; both change the existing interactive boundary and add lifecycle machinery.

### Reject MoneyPouch standalone commands inside a local scope

At entry, use existing boundary-local current-thread transaction discovery for pouch and inventory. Reject if either is enlisted, before validation or side effects. If neither is locally enlisted, preserve the existing standalone path and foreign-contention behavior.

Rejected alternative: adding composable overloads or a general Begin-or-join API.

### Keep empty clear in storage semantics

Before clearing or advancing revision, detect whether any slot is non-null. A non-null zero-count sentinel remains occupied. The existing container mutation boundary already suppresses publication when storage reports no change.

## Risks / Trade-offs

- If Equipment insertion fails and all established recovery destinations reject the item, the code must throw explicitly; recovery cannot guarantee conservation in that case.
- The thread-local count is intentionally synchronous and thread-affine. It must be balanced on every existing success, rollback, and publisher-failure path.
- A publisher exception after committed mutation remains observable after storage has changed; this work preserves that established behavior.

## Migration Plan

No data or API migration is required. Existing call sites retain their current public methods. Run focused and full local validation, then update the PR description to match the implemented behavior; do not commit or push.
