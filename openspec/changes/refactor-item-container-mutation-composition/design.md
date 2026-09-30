# Design

## Context

Before this change, `BaseItemContainer` owned slot state and mutation algorithms. Its lock and stable order were shared by ordinary mutation and #437 transfer. `TradeItemContainer` exposed the same synchronization boundary and wrapped the shared range/removal helpers for trade settlement. See proposal.md and the `item-container-storage` spec delta for goals and invariants.

## Goals / Non-Goals

**Goals:**

- Keep `ItemContainerStorage` as the single implementation of slot and mutation mechanics. Add one concrete `ItemContainer` that implements the generic container contracts and composes that storage.
- Keep `IItemContainer` as a contract-only interface and `ItemContainer` as its sole implementation. Ordinary domain implementations own `ItemContainer`, while their interfaces expose `IItemContainer Items`; they do not implement `IContainer` or forward the component's generic surface. Equipment and MoneyPouch own `ItemContainerStorage` directly and expose no generic `Items` property; Equipment itself remains a read-only `IContainer<IItem?>`. Special-domain mutation stays behind domain APIs. `IItemContainer` has no concrete `ItemContainer` dependency, bulk movement belongs to `ItemContainerTransfer`, and equipment publication stays private. The broader #439 decision about which operations should remain on `IItemContainer` is deferred.
- Keep `TradeExchange` as the owner of trade-specific settlement and multi-container restoration.

**Non-Goals:**

- Redesign constructor semantics, item mutability, or the remaining broad low-level state replacement API. The minimum #439 overlap removes `OnUpdate` from `IItemContainer` and delegates its common operations to composed storage rather than inherited algorithms; it does not decide which operations should eventually leave that interface.
- Redesign item mutability, shop transaction behavior, or trade acceptance rules.

## Decisions

### One concrete storage, plus a narrow transfer boundary

Create one public sealed `ItemContainerStorage` and one public sealed `ItemContainer` in Abstractions. `ItemContainer` implements only `IItemContainer` and `IItemContainerStorageOwner`, delegates mutation mechanics to its composed storage, and invokes an optional `Action<HashSet<int>?>` after successful mutations. Its publication implementation invokes the same callback so cross-container commits notify the composed owner. Domain classes do not implement the generic container contract or expose raw storage. `IItemContainer` includes neutral exact-removal semantics so shop payment and trade settlement share one generic operation.

### Store methods mutate and return changed slots

Move add, remove, exact removal, range insertion, replace/move/swap/sort/clear, exact-state replacement, queries, and version-aware enumeration into storage. Mutators return changed slots rather than invoking domain code. `AddRange` applies the existing simulation against a cloned slot state and commits only after full validation, eliminating trade-only rollback around a partial apply.

### Transfer planning belongs to storage

Move the existing #437 plan/simulate/commit algorithm into a storage-to-storage static operation. `ItemContainerTransfer` resolves provider stores and publishes updates after a successful commit; its storage-only entry point returns slot sets so equipment can run domain callbacks before publication. Bulk `AddAndRemoveFrom` behavior belongs to the coordinator, not the `IItemContainer` contract, and retains its per-source-slot exact-transfer behavior.

### Domain containers own operation delegation and publication

Each ordinary domain container owns one concrete `ItemContainer`, exposed through an `IItemContainer Items` interface property. It does not also implement `IContainer` or forward indexer, capacity, enumeration, mutation, or query members. Generic behavior lives once on `ItemContainer`; storage algorithms remain on `ItemContainerStorage`. Equipment and MoneyPouch own `ItemContainerStorage` directly, expose their domain operations only, and do not expose `Items`; Equipment itself remains a read-only `IContainer<IItem?>`. TradeOffer, Duel, and Price Checker retain only domain state and compose `ItemContainer`; `GenericContainer` and script-local generic forwarding wrappers are removed. `ItemContainerTransfer` remains the shared cross-container coordinator. `IItemContainer` does not depend on concrete `ItemContainer`, and bulk transfer is not a generic container member. `TradeExchange` is a consumer of this generic boundary, not an item-container capability: it acquires the existing storage locks in stable order and stages exact storage operations directly, snapshots affected stores, restores them on failure, and publishes only after the transaction commits. Pouch staging methods, where required by this transaction boundary, use domain-neutral exact names. Gameplay interfaces do not expose raw storage, storage-owner capability, or equipment publication. Composition must not be replaced by default-interface implementation inheritance; item-container interfaces define contracts only. Do not add a forwarding base class, extension implementation layer, storage interface hierarchy, or generated forwarding code.

Domain hydration code maps persisted DTOs to physical `(slot, item)` entries and calls one narrow `ItemContainerStorage.RestoreItems` operation. Storage validates capacity bounds, duplicate slots, item null/count rules and replaces state only after the complete input is valid. Normal restored counts remain positive; MoneyPouch independently validates its coin-995 entry at physical slot zero and allows count zero. A GameWorld hydration mapper shares DTO projection and construction without moving DTO knowledge into storage.

### Trade settlement locks stores directly

`TradeExchange` resolves all participants' stores, deduplicates by store identity, orders by `MutationOrder`, and acquires `MutationLock` in that order. Snapshot records keep both the domain container for publication and storage for state capture/restoration. Settlement, escrow, pouch overflow and event/message sequencing remain local to trade/pouch domain code.

### Zero-count and equipment rules stay at their owners

Storage accepts a simple `countToResetTo` constructor option. MoneyPouch seeds and validates its coin sentinel; ShopStock retains original-stock and normalization behavior. Equipment keeps semantic slot mapping and equipment callbacks. It uses storage-only transfer, then runs callbacks, then publishes inventory/equipment updates in the established order.

## Risks / Trade-offs

- **Trade-off:** Domain containers expose generic operations through a composed `ItemContainer`. Specialized domain operations remain on the domain object, keeping the generic component independent of gameplay services.
- **Risk:** A concrete wrapper publishes at the wrong time or bypasses domain behavior. → **Mitigation:** Keep storage mutation and publication separate; container methods publish only after storage reports a successful commit, while equipment and trade callbacks remain domain-owned.
- **Risk:** Move/swap/replace and exact state replacement can bypass callback or revision behavior if split between storage and domain code. → **Mitigation:** Have storage return committed changed slots and advance only its private revision; domain wrappers preserve existing publication flags and equipment effects.
- **Risk:** TradeExchange snapshots and storage mutation may restore the right slots but publish through the wrong owner. → **Mitigation:** Snapshot both storage and its owning `IItemContainer`, and retain existing post-commit publication tests.
- **Risk:** MoneyPouch's zero-count coin sentinel and ShopStock's zero-count depleted entries look similar but have different domain rules. → **Mitigation:** Share only the configured reset count; keep sentinel validation and stock normalization in their domain classes.

## Migration Plan

1. Keep slot state, mutation mechanics, revision/enumeration, lock/order, and transfer planning/commit in `ItemContainerStorage`; replace base-class transfer with `ItemContainerTransfer` and provider lookup.
2. Add `ItemContainer` as the sole generic contract implementation composed over `ItemContainerStorage`; publish committed mutations through a simple optional callback.
3. Migrate all production containers and script-local temporary containers to compose `ItemContainer`. Migrate trade participants and `TradeExchange` together so every participant uses the same storage boundary.
4. Replace inheritance-based test fixtures with composed test containers and split core tests into storage and transfer suites; add focused ShopStock and Equipment characterization.
5. Delete both old base classes and verify no source/test references or replacement implementation base remain.
6. Run focused and affected test suites, solution build, strict OpenSpec validation, repository duplication/quality gate, and diff checks.
7. Audit all item-container interfaces and consumers to ensure contracts contain declarations only, generic dispatch flows through the composed component, special domain behavior remains intact, and the complete branch introduces no new clone pairs.

Rollback is a source revert; there is no data or protocol migration.
