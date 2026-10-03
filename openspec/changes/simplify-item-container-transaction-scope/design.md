# Design

## Context

See proposal.md. Storage already mutates directly under reentrant ordered locks; snapshots capture references, counts, and revisions. Ordinary container methods publish through an owned mutation boundary. Equipment and pouch own specialized storage. The user approved the single-scope API and internal deferred domain work.

## Goals / Non-Goals

One disposable boundary must provide atomic mutations and automatic completion. Preserve existing item semantics and issue #347 session ownership. No new unit-of-work, coordinator, staging store, generic event/result framework, or configurable completion policy.

## Decisions

- Public Begin/Commit/Dispose only. A public empty participant marker provides cross-assembly typing; a separate internal bridge resolves repository-owned boundaries. Unsupported markers and malformed/null/empty contributions fail before locking.
- The pouch contributes its private boundary followed by every boundary from the inventory participant's existing internal bridge. Unsupported inventory participants or null/empty inventory contributions throw ArgumentException during resolution, before locks or mutation. Transaction storage deduplication preserves first-seen publication order across inventory aliases.
- Resolve participants and contributions in first-seen order; deduplicate storage separately. Acquire all locks in MutationOrder, snapshot all storage, then bind. Construction failure cleans every acquired resource in reverse order. Scope bindings live on storage and remain internal; no public current transaction API.
- Exact-transfer and pouch helpers inspect current-thread bindings before additional locking. No current-thread bindings permits an owned standalone scope; other-thread bindings serialize through deterministic storage locks. Complete same-scope originating-thread bindings permit joining; partial/conflicting current-thread bindings throw InvalidOperationException. Explicit Begin rejects current-thread nesting before locking and checks binding again after acquiring each lock. Existing bool/count domain rejection remains intact.
- Commit freezes completion data and changes Active to Committed before clearing snapshots, unbinding, and unlocking. Cleanup attempts every owned resource even if an individual operation fails; completion is skipped if cleanup fails or any lock remains intentionally held. Completed means all deferred completion succeeded. Committed failures remain irreversible; Dispose is owner-thread and idempotent.
- Occupied non-weapon/shield replacements retain the existing standalone command path and notification ordering, including custom interactive unequip commands. Invocation from bound storage is rejected before mutation; only their existing transaction-backed sibling paths migrate.
- Existing shop callers own standalone scopes so sorting and the bought event continue after successful Commit; an explicit nested Begin is rejected. No new deferred shop-event phase is added.
- Ordinary notification gateways aggregate changed slots. The boundary holds a fixed internal completion owner with only discard, before-publication and after-publication operations. Equipment stores ordered Equipped/Unequipped/WeaponProfile facts with item references in small owned batches; pouch stores previous-count/change-count facts. Neither the transaction nor boundary stores arbitrary executable work.
- A scope issues monotonically increasing completion ordinals as ordering facts. Fixed owner operations receive each ordinal in mutation order, preserving interleaved effects across owners independently from lock/enlistment order. No action queue or completion command is stored by the transaction. Owner pending data is keyed by scope in BCL ConcurrentDictionary so reentrant or contending later scopes cannot consume an earlier scope's completion. Each value remains an originating-thread queue protected during recording by existing storage locks; no second mutation lock or completion worker is introduced. Owners remove each fact before invoking observable code.
- Before-publication domain failures still permit remaining completion and publication. Container publication stops at its first failure and skips post-publication completion; post-publication completion stops at its first failure. All remaining owner facts are discarded in finally, including committed failure paths. Rollback discards facts before resource release without running domain effects. One failure preserves its original exception/stack; multiple failures form one flat AggregateException retaining original leaf exceptions. Construction and rollback failures enter the existing cleanup error list so unexpected cleanup failures cannot replace them.
- Trade owns Begin and terminal state under the existing gate. TradeExchange stages checked domain operations only. Mark Completed/Cancelled immediately before Commit; finally performs existing cleanup. Failed attempts dispose before refund/recovery begins. No transaction state query or result object drives trade lifecycle.
- Publication order follows first-seen storage/publisher registration, independently of lock order; pouch notifications follow mutation registration order. Preserve bank->inventory->pouch, offer/stake->recipient inventories->pouches, equipment hooks->inventory/equipment, and shop stock->inventory->pouch->sort/bought event.

## Risks / Trade-offs

- Commit can throw after storage becomes permanent -> XML docs and regressions distinguish pre-commit rollback from post-commit completion failure.
- Observer delivery can be partial -> no compensation/retries; propagate original publication failure and preserve committed storage.
- Item scripts may throw while validating/mutating -> restore all snapshots on disposal, not just notified storage.
- Eager mutations do not isolate unsynchronized readers. Transactions must stay synchronous on their originating thread, without crossing await boundaries.

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

## Joining helper side effects

Bank pouch deposits, pouch withdrawals and equipment capacity failures previously sent a failure message after optional disposal, which left outer locks held when joining. They now report that message only for an owned scope after disposal. A joined caller receives the existing false result and owns rollback and reporting after its scope ends. Current production UI callers are standalone, so their visible messages remain unchanged.

Exact transfer, pouch exact add/remove, equipment TryMoveTo and familiar WithdrawAvailableToInventory contain no failure publication. Successful equipment completion and pouch notification facts remain domain-owned and run through fixed completion stages after unlock.

Preflight gameplay validation remains a deliberate limitation. EquipItem/UnEquipItem invoke script-owned CanEquipItem/CanUnEquipItem, which can send eligibility events or messages; AddFromInventory can warn that the pouch is full before its transfer. These UI/domain entry points cannot become entirely side-effect-free under an enclosing scope without changing script validation and warning ordering. Existing transaction composition uses exact transfer/TryMoveTo and pouch exact operations instead. Bank and familiar single-item UI methods likewise retain their existing outer failure messages after the exact-transfer helper returns. No failure notification queue or redesign of domain validation is introduced. This limitation does not affect deferred commit publication and should be reviewed separately if a production caller needs to compose those gameplay entry points.
