# Design

## Context

`BaseItemContainer` currently owns slot state and mutation algorithms. Its lock and stable order are shared by ordinary mutation and #437 transfer. `TradeItemContainer` exposes the same synchronization boundary and wraps the shared range/removal helpers for trade settlement. See proposal.md and the `item-container-storage` spec delta for goals and invariants.

## Goals / Non-Goals

**Goals:**

- Move the single authoritative mutation implementation into `ItemContainerStorage` and use composition in all current production containers.
- Keep normal `IItemContainer` operations as default interface delegations to the shared extensions while removing repeated forwarding blocks. `OnUpdate` is removed from the gameplay interface; the composed domain container/infrastructure owner publishes only after committed changes. The broader #439 decision about which operations should remain on `IItemContainer` is deferred.
- Keep `TradeExchange` as the owner of trade-specific settlement and multi-container restoration.

**Non-Goals:**

- Redesign constructor semantics, item mutability, or the remaining broad low-level state replacement API. The minimum #439 overlap removes `OnUpdate` from `IItemContainer` and delegates its common operations to composed storage rather than inherited algorithms; it does not decide which operations should eventually leave that interface.
- Redesign item mutability, shop transaction behavior, or trade acceptance rules.

## Decisions

### One concrete storage, plus a provider boundary

Create one public sealed `ItemContainerStorage` in Abstractions. The public `IItemContainerStorageOwner` exposes the concrete store only to cross-assembly infrastructure such as transfer and trade coordination. Do not add a storage interface or expose a `Storage` member on `IItemContainer`.

### Store methods mutate and return changed slots

Move add, remove, exact removal, range insertion, replace/move/swap/sort/clear, exact-state replacement, queries, and version-aware enumeration into storage. Mutators return changed slots rather than invoking domain code. `AddRange` applies the existing simulation against a cloned slot state and commits only after full validation, eliminating trade-only rollback around a partial apply.

### Transfer planning belongs to storage

Move the existing #437 plan/simulate/commit algorithm into a storage-to-storage static operation. `ItemContainerTransfer` resolves provider stores and publishes updates after a successful commit; its storage-only entry point returns slot sets so equipment can run domain callbacks before publication. `AddAndRemoveFrom` delegates to the coordinator while preserving its per-source-slot exact-transfer behavior.

### Domain containers own publication; extensions share storage operations

Each container owns one private store and implements `IItemContainerStorageOwner`. The owner exposes the storage-backed `IContainer<IItem?>` read projection directly from its store, without a cast from `IItemContainer`; `IItemContainer` declares its remaining read properties. The single `ItemContainerExtensions` surface implements common queries and mutations against that store, then calls the owner's domain publication hook only after a committed mutation and according to the existing update flag. `IItemContainer` default methods delegate to those extensions so calls through container interfaces retain dynamic dispatch to any domain override. `ITradeItemContainer` retains checked operations with default delegations to the same shared layer. The gameplay interface does not expose raw storage or `OnUpdate`. Do not add a common forwarding base class, storage interface hierarchy, or storage mutation algorithms to default interface methods.

Domain hydration code maps persisted DTOs to physical `(slot, item)` entries and calls one narrow `ItemContainerStorage.RestoreItems` operation. Storage validates capacity bounds, duplicate slots, item null/count rules and replaces state only after the complete input is valid. Normal restored counts remain positive; MoneyPouch independently validates its coin-995 entry at physical slot zero and allows count zero. A GameWorld hydration mapper shares DTO projection and construction without moving DTO knowledge into storage.

### Trade settlement locks stores directly

`TradeExchange` resolves all participants' stores, deduplicates by store identity, orders by `MutationOrder`, and acquires `MutationLock` in that order. Snapshot records keep both the domain container for publication and storage for state capture/restoration. Settlement, escrow, pouch overflow and event/message sequencing remain local to trade/pouch domain code.

### Zero-count and equipment rules stay at their owners

Storage accepts a simple `countToResetTo` constructor option. MoneyPouch seeds and validates its coin sentinel; ShopStock retains original-stock and normalization behavior. Equipment keeps semantic slot mapping and equipment callbacks. It uses storage-only transfer, then runs callbacks, then publishes inventory/equipment updates in the established order.

## Risks / Trade-offs

- **Risk:** Shared extensions publish at the wrong time or route through the wrong owner. → **Mitigation:** Extensions publish through the provider only after storage reports a successful commit; domain-specific callbacks remain in the owner.
- **Risk:** Move/swap/replace and exact state replacement can bypass callback or revision behavior if split between storage and domain code. → **Mitigation:** Have storage return committed changed slots and advance only its private revision; domain wrappers preserve existing publication flags and equipment effects.
- **Risk:** TradeExchange snapshots and storage mutation may restore the right slots but publish through the wrong owner. → **Mitigation:** Snapshot both storage and its owning `IItemContainer`, and retain existing post-commit publication tests.
- **Risk:** MoneyPouch's zero-count coin sentinel and ShopStock's zero-count depleted entries look similar but have different domain rules. → **Mitigation:** Share only the configured reset count; keep sentinel validation and stock normalization in their domain classes.

## Migration Plan

1. Move storage state, the mutation implementation, revision/enumeration, lock/order, and transfer planning/commit into `ItemContainerStorage`; replace base-class transfer with `ItemContainerTransfer` and provider lookup.
2. Migrate all production containers and script-local temporary containers directly to composition. Migrate trade participants and `TradeExchange` together so every participant uses the same storage boundary.
3. Replace inheritance-based test fixtures with composed test containers and split core tests into storage and transfer suites; add focused ShopStock and Equipment characterization.
4. Delete both old base classes and verify no source/test references or replacement implementation base remain.
5. Run focused and affected test suites, solution build, strict OpenSpec validation, repository duplication/quality gate, and diff checks.

Rollback is a source revert; there is no data or protocol migration.
