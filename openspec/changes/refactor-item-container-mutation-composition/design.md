# Design

## Context

Before this change, `BaseItemContainer` owned slot state and mutation algorithms. Its lock and stable order were shared by ordinary mutation and #437 transfer. `TradeItemContainer` exposed the same synchronization boundary and wrapped the shared range/removal helpers for trade settlement. See proposal.md and the `item-container-storage` spec delta for goals and invariants.

## Goals / Non-Goals

**Goals:**

- Keep `ItemContainerStorage` as the single implementation of slot and mutation mechanics. Add one concrete `ItemContainer` that implements the generic container contracts and composes that storage.
- Keep `IItemContainer` as a contract-only interface and `ItemContainer` as its sole production implementation. Ordinary domain implementations privately own `ItemContainer`, and their interfaces expose `IItemContainer Items`; they do not implement `IContainer` or forward the component's generic surface. `IItemContainer` inherits the empty public `IItemTransactional` marker, while ordinary mutation and transfer operations remain members of `IItemContainer`. `IMoneyPouchContainer` also inherits the marker. Equipment and MoneyPouch own `ItemContainerStorage` and private boundaries, expose no generic `Items` property; `IEquipmentContainer` composes `IReadOnlyItemContainer Items`. Transaction participation is explicit by passing aggregate objects directly to `Begin(...)`. Ordinary container mutations participate automatically only for explicitly enlisted storage.
- Keep `TradeExchange` as the owner of trade-specific economic staging. `TradingCharacterScript` owns terminal trade transaction scopes and session state, setting terminal state before `Commit()` performs post-unlock publication.

**Non-Goals:**

- Redesign constructor semantics, item mutability, or the remaining broad low-level state replacement API. The minimum #439 overlap removes `OnUpdate` from `IItemContainer` and delegates its common operations to composed storage rather than inherited algorithms; it does not decide which operations should eventually leave that interface.
- Redesign item mutability or trade acceptance rules.

## Decisions

### One concrete storage and an owned mutation boundary

`ItemContainer` composes `ItemContainerStorage` and a private concrete mutation boundary; it implements the public empty `IItemTransactional` marker and supplies the boundary through internal `IItemTransactionSource`. Its internal boundary owns one storage reference and its optional publication callback. Transfer requires a caller-owned active transaction containing source and destination storage; it is invoked through `IItemContainer.TryTransferTo`. Ordinary facade mutations automatically use bound storage and its deferred publication path. The internal enlistment bridge registers storage and publication with `ItemContainerTransaction` without exposing either. Ordinary domain interfaces expose `IItemContainer Items`; implementations privately own concrete components. Special domains keep their concrete boundaries private. `IItemContainer` includes neutral exact-removal semantics so shop payment and trade settlement share one generic operation.

### Store methods mutate and return changed slots

Move add, remove, exact removal, range insertion, replace/move/swap/sort/clear, exact-state replacement, queries, and version-aware enumeration into storage. Mutators return changed slots rather than invoking domain code. `AddRange` applies the existing simulation against a cloned slot state and commits only after full validation, eliminating trade-only rollback around a partial apply.

### Exact transfer enters through a boundary and uses one transaction owner

Exact transfer enters through `IItemContainer.TryTransferTo` inside an already-owned `ItemContainerTransaction`. The transaction alone acquires storage locks in stable order, calls the low-level `ItemContainerStorage.TryTransferTo` algorithm, and publishes committed changes after unlocking. Equipment owns its lifecycle completion and uses an internal boundary bridge for equipment transfers. Delete `ItemContainerTransfer` and `IItemContainerStorageOwner`; callers do not receive storage boundaries. Generic mutation infrastructure has no bulk-transfer policy. `FamiliarInventoryContainer.WithdrawAvailableToInventory` owns its partial-success policy and tries every source item in one transaction, leaving items that do not fit.

### Domain containers own operation delegation and publication

Each ordinary domain container owns one concrete `ItemContainer`, exposed as `IItemContainer Items` on its domain interface. It does not also implement `IContainer` or forward indexer, capacity, enumeration, mutation, or query members. Generic behavior lives once on `ItemContainer`; storage algorithms remain on `ItemContainerStorage`. Equipment and MoneyPouch own storage and private concrete boundaries; Equipment exposes a read-only `IReadOnlyItemContainer Items`, while both aggregates provide transaction storage through internal `IItemTransactionSource`; neither exposes generic mutation for special storage. TradeOffer, Duel, and Price Checker retain only domain state and compose `ItemContainer`; `GenericContainer` and script-local generic forwarding wrappers are removed. `IItemContainer` does not depend on concrete `ItemContainer`, and bulk transfer is not a generic container member. `TradingCharacterScript` composes its terminal transaction over aggregates implementing `IItemTransactional`, which resolves storage, acquires deterministic locks, snapshots and rolls back enlisted storage, and publishes committed changes after releasing locks. `TradeExchange` stages economic mutations within that caller-owned scope. Transaction callers pass aggregate objects directly to `Begin(...)`. MoneyPouch exact operations own a transaction only when all required storage is unbound; they participate in a single complete active scope and reject partial or conflicting participation. Gameplay interfaces do not expose raw storage or special-domain boundaries. Composition must not be replaced by default-interface implementation inheritance; item-container interfaces define contracts only. Do not add a forwarding base class, extension implementation layer, storage interface hierarchy, or generated forwarding code.

Domain hydration code maps persisted DTOs to physical `(slot, item)` entries and calls one narrow `ItemContainerStorage.RestoreItems` operation. Storage validates capacity bounds, duplicate slots, item null/count rules and replaces state only after the complete input is valid. Normal restored counts remain positive; MoneyPouch independently validates its coin-995 entry at physical slot zero and allows count zero. A GameWorld hydration mapper shares DTO projection and construction without moving DTO knowledge into storage.

### Trade settlement composes a short-lived transaction

`ItemContainerTransaction.Begin` receives public participants before locking and validates every participant and contribution before acquisition. It keeps first-seen participant/publication order separately from unique storage lock order, which is sorted by `MutationOrder`. It captures all snapshots before establishing bindings; any construction failure releases acquired locks in reverse order and exposes no partial scope. Mutations occur directly in storage under those locks through explicit boundary methods. Disposal restores storage unless Commit has declared it irreversible. Commit releases every mutation lock before fixed domain completion and publication but retains scope bindings through that work and pending-fact cleanup. It then reacquires the complete ordered storage set, clears bindings, pulses waiters, and releases locks in reverse order. Foreign overlapping Begin calls wait without retaining a partial lock prefix; same-thread overlap throws. If lock cleanup fails, it preserves the failure and skips observable completion. No transaction interface, staging callback, Include, or OnCommitted API is exposed. MoneyPouch uses transaction-keyed immutable completion facts and does not snapshot or restore pouch storage itself.

Ordinary `IItemContainer` methods use their normal public APIs both inside and outside a transaction. They participate automatically when their storage is enlisted and publish immediately when it is not. `IMoneyPouchContainer.TryAddExact` and `TryRemoveExact` own a transaction when all required storage is unbound, participate in the current transaction when all required storage belongs to it, and throw before mutation for partial or conflicting participation. Callers enlist the MoneyPouch aggregate directly through `ItemContainerTransaction.Begin(...)`.

Mutation-time completion facts capture data needed for notification. MoneyPouch records previous pouch count, new pouch count, and the message change amount; post-commit event payloads use those captured counts even if reentrant publication has changed current storage.

MoneyPouch domain operations keep their current amount and presentation rules but stage pouch and inventory changes through the same transaction. `BankContainer.DepositFromMoneyPouch`, shop buy/sell, and duel stake/unstake/refund similarly stage every economic participant together. A simple exact two-container move uses `IItemContainer.TryTransferTo`; multi-container domain work remains owned by its domain method or concrete composed collaborator.

`DuelArenaScript` composes one concrete `DuelStakeExchange` for inventory/pouch staking, unstaking, and cancellation refunds. The collaborator owns no UI or duel-session state. A failed two-player cancellation refund leaves both stake containers and the live session intact.

Equipment replacement identifies all conflicting equipped items and checks every `CanUnEquipItem` permission before removing the incoming item or invoking an unequip command. After preflight it preserves the existing `UnEquipItem` command calls, including custom/interactive behavior. This is a safe-failure preflight, not a generic transaction redesign of equipment commands.

The familiar partial withdrawal operation belongs to `IFamiliarInventoryContainer`/`FamiliarInventoryContainer`. One transaction attempts each familiar item; individual failures do not abort other transfers, and items that do not fit remain in the familiar container. Successful changes publish once per affected container after commit.

### Trade orchestration is a composed collaborator

`TradingCharacterScript` owns its two `TradeContainer` offers and composes one concrete `TradeExchange` with the injected `IItemBuilder`:

```text
TradingCharacterScript
    |
    +-- TradeContainer SelfContainer
    +-- TradeContainer TargetContainer
    +-- TradeExchange
            +-- IItemBuilder
            +-- creates ItemContainerTransaction per operation
            +-- collaborates through IItemContainer,
                IItemTransactional, and IMoneyPouchContainer
```

`TradeExchange` is a concrete composed collaborator, not a global/static service. It coordinates trade decisions, recipient preflight, escrow settlement, and recovery by staging mutations in its caller's transaction. `TradingCharacterScript` owns terminal transaction scopes and trade-session state, sets the terminal state before commit, and retains cleanup ownership. `ItemContainerTransaction`, mutation boundaries, and MoneyPouch retain ownership of locking, snapshots, rollback, and automatic post-unlock publication.

Death processing defers the actual equipment `Clear(true)` mutation until after inventory restoration and ground-item creation. That mutation naturally publishes the final equipment state; no publication-only completion method is needed on `IEquipmentContainer`.

### Zero-count and equipment rules stay at their owners

Storage accepts a simple `countToResetTo` constructor option. MoneyPouch seeds and validates its coin sentinel; ShopStock retains original-stock and normalization behavior. Equipment keeps semantic slot mapping and equipment callbacks. For Weapon/Shield conflicts, `EquipmentContainer` preflights every `CanUnEquipItem`, then stages incoming exact removal, outgoing transfers, and exact-slot equipment insertion in one transaction. Its private exact-slot storage operation stages the incoming instance without widening any public mutation capability or exposing transaction internals. After commit and unlock, callbacks run in weapon unequip, shield unequip, weapon profile/special-attack, incoming equip order before inventory/equipment publication. It does not run the conflicting items' `UnEquipItem` commands in this path. Other occupied equipment slots keep their existing command behavior.

## Risks / Trade-offs

- **Trade-off:** Domain containers expose generic operations through a composed `ItemContainer`. Specialized domain operations remain on the domain object, keeping the generic component independent of gameplay services.
- **Risk:** A concrete wrapper publishes at the wrong time or bypasses domain behavior. → **Mitigation:** Keep storage mutation and publication separate; container methods publish only after storage reports a successful commit, while equipment and trade callbacks remain domain-owned.
- **Risk:** Move/swap/replace and exact state replacement can bypass callback or revision behavior if split between storage and domain code. → **Mitigation:** Have storage return committed changed slots and advance only its private revision; domain wrappers preserve existing publication flags and equipment effects.
- **Risk:** TradeExchange staging may omit storage required by a terminal operation. → **Mitigation:** `TradingCharacterScript` supplies every required aggregate to `Begin(...)` before invoking the staging method, with existing rollback and publication tests covering the operation.
- **Risk:** MoneyPouch's zero-count coin sentinel and ShopStock's zero-count depleted entries look similar but have different domain rules. → **Mitigation:** Share only the configured reset count; keep sentinel validation and stock normalization in their domain classes.

## Migration Plan

1. Keep slot state, mutation mechanics, revision/enumeration, lock/order, and transfer planning/commit in `ItemContainerStorage`; add an instance `ItemContainerMutationBoundary` and remove owner-capability/static coordination.
2. Add `ItemContainer` as the sole generic contract implementation composed over storage and its public mutation boundary; publish committed mutations through a simple optional callback.
3. Migrate ordinary domain interfaces to expose `IItemContainer Items` while implementations privately own concrete `ItemContainer`; migrate special domains to private boundaries and domain-safe operations.
4. Add `ItemContainerTransaction` and have `TradingCharacterScript` own terminal scopes while `TradeExchange` stages mutations without raw storage access or runtime infrastructure casts.
5. Replace inheritance-based test fixtures with composed test containers and split core tests into storage, boundary, and transaction suites; add focused ShopStock and Equipment characterization.
6. Delete both old base classes and verify no source/test references or replacement implementation base remain.
7. Run focused and affected test suites, solution build, strict OpenSpec validation, and diff checks. Clone cleanup remains a later pass.
8. Audit all item-container interfaces and consumers to ensure contracts contain declarations only, generic dispatch flows through the composed component, special domain behavior remains intact, and the complete branch introduces no infrastructure casts.

Rollback is a source revert; there is no data or protocol migration.
