# Design

## Context

Before this change, `BaseItemContainer` owned slot state and mutation algorithms. Its lock and stable order were shared by ordinary mutation and #437 transfer. `TradeItemContainer` exposed the same synchronization boundary and wrapped the shared range/removal helpers for trade settlement. See proposal.md and the `item-container-storage` spec delta for goals and invariants.

## Goals / Non-Goals

**Goals:**

- Keep `ItemContainerStorage` as the single implementation of slot and mutation mechanics. Add one concrete `ItemContainer` that implements the generic container contracts and composes that storage.
- Keep `IItemContainer` as a contract-only interface and `ItemContainer` as its sole implementation. Ordinary domain implementations privately own `ItemContainer`, and their interfaces expose `IItemContainer Items`; they do not implement `IContainer` or forward the component's generic surface. `IItemContainer` exposes `IItemContainerMutationBoundary`, while `ItemContainer` privately owns its concrete boundary. Equipment and MoneyPouch own `ItemContainerStorage` and private boundaries, expose no generic `Items` property, and Equipment itself remains a read-only `IContainer<IItem?>`. The broader #439 decision about which operations should remain on `IItemContainer` is deferred.
- Keep `TradeExchange` as the owner of trade-specific settlement and compose `ItemContainerTransaction` for multi-boundary locking, rollback and post-commit publication.

**Non-Goals:**

- Redesign constructor semantics, item mutability, or the remaining broad low-level state replacement API. The minimum #439 overlap removes `OnUpdate` from `IItemContainer` and delegates its common operations to composed storage rather than inherited algorithms; it does not decide which operations should eventually leave that interface.
- Redesign item mutability, shop transaction behavior, or trade acceptance rules.

## Decisions

### One concrete storage and an owned mutation boundary

`ItemContainer` composes `ItemContainerStorage` and a private concrete `ItemContainerMutationBoundary`, exposed through `IItemContainerMutationBoundary`. The boundary owns exactly one storage reference and its optional publication callback. It provides instance-based `TryTransferTo`; the transaction is the only multi-boundary lock/snapshot/rollback mechanism. Its internal enlistment bridge registers storage and publication with `ItemContainerTransaction` without exposing either. Ordinary domain interfaces expose `IItemContainer Items`; implementations privately own concrete components. Special domains keep their boundaries private. `IItemContainer` includes neutral exact-removal semantics so shop payment and trade settlement share one generic operation.

### Store methods mutate and return changed slots

Move add, remove, exact removal, range insertion, replace/move/swap/sort/clear, exact-state replacement, queries, and version-aware enumeration into storage. Mutators return changed slots rather than invoking domain code. `AddRange` applies the existing simulation against a cloned slot state and commits only after full validation, eliminating trade-only rollback around a partial apply.

### Exact transfer enters through a boundary and uses one transaction owner

Exact transfer enters through `IItemContainerMutationBoundary.TryTransferTo`, which creates a short-lived `ItemContainerTransaction`. The transaction alone acquires storage locks in stable order, calls the low-level `ItemContainerStorage.TryTransfer` algorithm, and publishes committed changes after unlocking. Equipment uses the concrete transaction's internal pre-publication callback to preserve its callback order. Delete `ItemContainerTransfer` and `IItemContainerStorageOwner`; callers receive boundaries and do not recover them by casts. Generic mutation infrastructure has no bulk-transfer policy. `FamiliarInventoryContainer.WithdrawAvailableToInventory` owns its partial-success policy and tries every source item in one transaction, leaving items that do not fit.

### Domain containers own operation delegation and publication

Each ordinary domain container owns one concrete `ItemContainer`, exposed as `IItemContainer Items` on its domain interface. It does not also implement `IContainer` or forward indexer, capacity, enumeration, mutation, or query members. Generic behavior lives once on `ItemContainer`; storage algorithms remain on `ItemContainerStorage`. Equipment and MoneyPouch own storage and private mutation boundaries, expose domain operations only, and do not expose `Items`; Equipment itself remains a read-only `IContainer<IItem?>` with no publication-only escape hatch. TradeOffer, Duel, and Price Checker retain only domain state and compose `ItemContainer`; `GenericContainer` and script-local generic forwarding wrappers are removed. `IItemContainer` does not depend on concrete `ItemContainer`, and bulk transfer is not a generic container member. `TradeExchange` composes a short-lived `ItemContainerTransaction` over `IItemContainerMutationBoundary` participants through `IItemContainerTransaction`; the transaction acquires deterministic locks, snapshots and rolls back enlisted storage, and publishes committed boundary changes after releasing locks. Pouch staging participates through semantic MoneyPouch operations and transaction callbacks without exposing its boundary. Gameplay interfaces do not expose raw storage or special-domain boundaries. Composition must not be replaced by default-interface implementation inheritance; item-container interfaces define contracts only. Do not add a forwarding base class, extension implementation layer, storage interface hierarchy, or generated forwarding code.

Domain hydration code maps persisted DTOs to physical `(slot, item)` entries and calls one narrow `ItemContainerStorage.RestoreItems` operation. Storage validates capacity bounds, duplicate slots, item null/count rules and replaces state only after the complete input is valid. Normal restored counts remain positive; MoneyPouch independently validates its coin-995 entry at physical slot zero and allows count zero. A GameWorld hydration mapper shares DTO projection and construction without moving DTO knowledge into storage.

### Trade settlement composes a short-lived transaction

`ItemContainerTransaction` implements `IItemContainerTransaction`, receives interface-typed participants, deduplicates and orders their stores by `MutationOrder`, and owns execution, locking, snapshots, rollback, changed-slot tracking, and publication. `TryExecute` exists only on the concrete transaction; callbacks receive the narrower active `IItemContainerTransaction` staging context. `RecordChangedSlots` is private transaction bookkeeping. `TradeExchange` performs domain-specific settlement through the transaction interface and keeps escrow, pouch overflow, and event/message sequencing in trade/pouch domain code. MoneyPouch stages pouch and inventory mutations through the same active transaction and registers its domain effects with `OnCommitted`; it does not snapshot or restore pouch storage itself.

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
                IItemContainerMutationBoundary,
                IItemContainerTransaction, and IMoneyPouchContainer
```

`TradeExchange` is a concrete composed collaborator, not a global/static service. It coordinates trade decisions, recipient preflight, escrow settlement, and recovery. `ItemContainerTransaction`, the mutation boundaries, and MoneyPouch retain ownership of locking, snapshots, rollback, generic publication, and pouch post-commit behavior. Trade-session state remains on `TradingCharacterScript` and its session model.

Death processing defers the actual equipment `Clear(true)` mutation until after inventory restoration and ground-item creation. That mutation naturally publishes the final equipment state; no publication-only completion method is needed on `IEquipmentContainer`.

### Zero-count and equipment rules stay at their owners

Storage accepts a simple `countToResetTo` constructor option. MoneyPouch seeds and validates its coin sentinel; ShopStock retains original-stock and normalization behavior. Equipment keeps semantic slot mapping and equipment callbacks. It uses storage-only transfer, then runs callbacks, then publishes inventory/equipment updates in the established order.

## Risks / Trade-offs

- **Trade-off:** Domain containers expose generic operations through a composed `ItemContainer`. Specialized domain operations remain on the domain object, keeping the generic component independent of gameplay services.
- **Risk:** A concrete wrapper publishes at the wrong time or bypasses domain behavior. → **Mitigation:** Keep storage mutation and publication separate; container methods publish only after storage reports a successful commit, while equipment and trade callbacks remain domain-owned.
- **Risk:** Move/swap/replace and exact state replacement can bypass callback or revision behavior if split between storage and domain code. → **Mitigation:** Have storage return committed changed slots and advance only its private revision; domain wrappers preserve existing publication flags and equipment effects.
- **Risk:** TradeExchange snapshots and storage mutation may restore the right slots but publish through the wrong owner. → **Mitigation:** Snapshot both storage and its owning `IItemContainer`, and retain existing post-commit publication tests.
- **Risk:** MoneyPouch's zero-count coin sentinel and ShopStock's zero-count depleted entries look similar but have different domain rules. → **Mitigation:** Share only the configured reset count; keep sentinel validation and stock normalization in their domain classes.

## Migration Plan

1. Keep slot state, mutation mechanics, revision/enumeration, lock/order, and transfer planning/commit in `ItemContainerStorage`; add an instance `ItemContainerMutationBoundary` and remove owner-capability/static coordination.
2. Add `ItemContainer` as the sole generic contract implementation composed over storage and its public mutation boundary; publish committed mutations through a simple optional callback.
3. Migrate ordinary domain interfaces to expose `IItemContainer Items` while implementations privately own concrete `ItemContainer`; migrate special domains to private boundaries and domain-safe operations.
4. Add `ItemContainerTransaction` and migrate TradeExchange to compose it without raw storage access or runtime infrastructure casts.
5. Replace inheritance-based test fixtures with composed test containers and split core tests into storage, boundary, and transaction suites; add focused ShopStock and Equipment characterization.
6. Delete both old base classes and verify no source/test references or replacement implementation base remain.
7. Run focused and affected test suites, solution build, strict OpenSpec validation, and diff checks. Clone cleanup remains a later pass.
8. Audit all item-container interfaces and consumers to ensure contracts contain declarations only, generic dispatch flows through the composed component, special domain behavior remains intact, and the complete branch introduces no infrastructure casts.

Rollback is a source revert; there is no data or protocol migration.
