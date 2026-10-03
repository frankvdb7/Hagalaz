# Design

## Context

See proposal.md. Storage already mutates directly under reentrant ordered locks; snapshots capture references, counts, and revisions. Ordinary container methods publish through an owned mutation boundary. Equipment and pouch own specialized storage. The user approved the single-scope API and internal deferred domain work.

## Goals / Non-Goals

One disposable boundary must provide atomic mutations and automatic completion. Preserve existing item semantics and issue #347 session ownership. No new unit-of-work, coordinator, staging store, generic event/result framework, or configurable completion policy.

## Decisions

- Public Begin/Commit/Dispose only. A public empty participant marker provides cross-assembly typing; a separate internal bridge resolves repository-owned boundaries. Unsupported markers and malformed/null/empty contributions fail before locking.
- Resolve participants and contributions in first-seen order; deduplicate storage separately. Acquire all locks in MutationOrder, snapshot all storage, then bind. Construction failure cleans every acquired resource in reverse order. Scope bindings live on storage and remain internal; no public current transaction API.
- Exact-transfer and pouch helpers inspect all required bindings before additional locking. No bindings permits an owned standalone scope; complete same-scope originating-thread bindings permit joining; partial/conflicting/foreign-thread bindings throw InvalidOperationException. Existing bool/count domain rejection remains intact.
- Commit freezes completion data and changes Active to Committed before clearing snapshots, unbinding, and unlocking. Cleanup attempts every owned resource even if an individual operation fails; completion is skipped if cleanup fails or any lock remains intentionally held. Completed means all deferred completion succeeded. Committed failures remain irreversible; Dispose is owner-thread and idempotent.
- Occupied non-weapon/shield replacements retain the existing standalone command path and notification ordering, including custom interactive unequip commands. Invocation from bound storage is rejected before mutation; only their existing transaction-backed sibling paths migrate.
- Existing shop callers own standalone scopes so sorting and the bought event continue after successful Commit; an explicit nested Begin is rejected. No new deferred shop-event phase is added.
- Ordinary notification gateways aggregate changed slots. Pouch records captured previous-count/message facts as internal deferred actions. Equipment records one owned hook batch using its existing attempt-all helper. Fixed hook, container, and pouch categories replace public callback registration and receipts.
- A hook failure still permits publication. Container publication stops at its first failure and skips all pouch effects; pouch publication stops at its first failure. One failure preserves its original exception/stack; hook plus publication failure becomes AggregateException(hookFailure, publicationFailure), preserving both exception objects. No configurable pipeline is introduced.
- Trade owns Begin and terminal state under the existing gate. TradeExchange stages checked domain operations only. Mark Completed/Cancelled immediately before Commit; finally performs existing cleanup. Failed attempts dispose before refund/recovery begins. No transaction state query or result object drives trade lifecycle.
- Publication order follows first-seen storage/publisher registration, independently of lock order; pouch notifications follow mutation registration order. Preserve bank->inventory->pouch, offer/stake->recipient inventories->pouches, equipment hooks->inventory/equipment, and shop stock->inventory->pouch->sort/bought event.

## Risks / Trade-offs

- Commit can throw after storage becomes permanent -> XML docs and regressions distinguish pre-commit rollback from post-commit completion failure.
- Observer delivery can be partial -> no compensation/retries; propagate original publication failure and preserve committed storage.
- Item scripts may throw while validating/mutating -> restore all snapshots on disposal, not just notified storage.

## Migration Plan

Deploy the API and all current callers together; no persisted data changes. This change supersedes the transaction API decisions in trade-terminal-lifecycle-integrity while retaining its lifecycle fixes. Sync current specs only after the approved regression boundary passes.

## Representative migrated operations

An exact transfer retains its owned domain boundary, with notifications deferred by enlisted storage:

```csharp
using var transaction = ItemContainerTransaction.Begin(source.Mutations, destination.Mutations);
if (!source.Mutations.TryTransferTo(destination.Mutations, item, count)) return false;
transaction.Commit();
```

Pouch participation includes its private storage and inventory without exposing either through the marker:

```csharp
using var transaction = ItemContainerTransaction.Begin(bank.Items.Mutations, character.MoneyPouch.Mutations);
if (!character.MoneyPouch.TryRemoveExact(count) || !bank.Items.Add(coins)) return false;
transaction.Commit();
```

The trade session owns the terminal transition under its existing gate:

```csharp
using var transaction = ItemContainerTransaction.Begin(firstOffer.Mutations, secondOffer.Mutations,
    first.Inventory.Items.Mutations, second.Inventory.Items.Mutations,
    first.MoneyPouch.Mutations, second.MoneyPouch.Mutations);
if (_tradeExchange.TryStageCompletion(first, firstOffer, second, secondOffer))
{
    session.State = TradeState.Completed;
    transaction.Commit();
}
```

The common marker types ordinary and composite participants across assemblies. The internal bridge contributes storage, while the existing exact-transfer helper and TradeExchange retain their domain rules. None requires a separate unit of work or public transaction mutation API.

## Principle review

- KISS: one using scope and one Commit; normal container/domain mutations supply item semantics.
- YAGNI: no dynamic enlistment, public status, rollback, publication phase, result infrastructure, or generic completion policies.
- SRP: the scope owns atomicity and lifecycle; equipment, pouch, shop and trade retain domain behavior.
- Invalid use: complete same-scope/thread participation is checked before helpers lock or mutate; Begin rejects nesting; the irreversible state prevents rollback and completion retry, and Commit owns publication.
