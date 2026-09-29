# Design

## Context

Before this change, `BaseItemContainer` owned slot state and mutation algorithms. Its lock and stable order were shared by ordinary mutation and #437 transfer. `TradeItemContainer` exposed the same synchronization boundary and wrapped the shared range/removal helpers for trade settlement. See proposal.md and the `item-container-storage` spec delta for goals and invariants.

## Goals / Non-Goals

**Goals:**

- Move the single authoritative mutation implementation into `ItemContainerStorage` and use composition in all current production containers.
- Keep `IItemContainer`, `ITradeItemContainer`, and `IItemContainerStorageOwner` as contract-only interfaces. Each concrete domain container implements its operations and delegates generic mechanics to its owned storage. `OnUpdate` remains domain-owned; the broader #439 decision about which operations should remain on `IItemContainer` is deferred.
- Keep `TradeExchange` as the owner of trade-specific settlement and multi-container restoration.

**Non-Goals:**

- Redesign constructor semantics, item mutability, or the remaining broad low-level state replacement API. The minimum #439 overlap removes `OnUpdate` from `IItemContainer` and delegates its common operations to composed storage rather than inherited algorithms; it does not decide which operations should eventually leave that interface.
- Redesign item mutability, shop transaction behavior, or trade acceptance rules.

## Decisions

### One concrete storage, plus a narrow transfer boundary

Create one public sealed `ItemContainerStorage` in Abstractions. `IItemContainerStorageOwner` exposes the concrete store to cross-assembly transfer and trade coordination. Its `PublishChanges` declaration remains because `ItemContainerTransfer` must notify both domain containers after a successful atomic commit; ordinary mutations publish directly from their concrete owner. Do not add a storage interface or expose a `Storage` member on `IItemContainer`.

### Store methods mutate and return changed slots

Move add, remove, exact removal, range insertion, replace/move/swap/sort/clear, exact-state replacement, queries, and version-aware enumeration into storage. Mutators return changed slots rather than invoking domain code. `AddRange` applies the existing simulation against a cloned slot state and commits only after full validation, eliminating trade-only rollback around a partial apply.

### Transfer planning belongs to storage

Move the existing #437 plan/simulate/commit algorithm into a storage-to-storage static operation. `ItemContainerTransfer` resolves provider stores and publishes updates after a successful commit; its storage-only entry point returns slot sets so equipment can run domain callbacks before publication. `AddAndRemoveFrom` delegates to the coordinator while preserving its per-source-slot exact-transfer behavior.

### Domain containers own operation delegation and publication

Each container owns one private store and explicitly implements the `IItemContainer` contract, including the inherited `IContainer<IItem?>` projection. Small concrete forwarding members delegate generic queries and mutations to that container's store; successful mutations publish through the domain container according to the existing update flag. `ITradeItemContainer` declares checked operations only, and each concrete trade participant implements them so interface dispatch reaches specialized implementations. `ItemContainerTransfer` remains the shared cross-container coordinator. The gameplay interface does not expose raw storage or `OnUpdate`. Composition must not be replaced by default-interface implementation inheritance; item-container interfaces define contracts only. Do not add a common forwarding base class, extension implementation layer, storage interface hierarchy, or generated forwarding code.

Domain hydration code maps persisted DTOs to physical `(slot, item)` entries and calls one narrow `ItemContainerStorage.RestoreItems` operation. Storage validates capacity bounds, duplicate slots, item null/count rules and replaces state only after the complete input is valid. Normal restored counts remain positive; MoneyPouch independently validates its coin-995 entry at physical slot zero and allows count zero. A GameWorld hydration mapper shares DTO projection and construction without moving DTO knowledge into storage.

### Trade settlement locks stores directly

`TradeExchange` resolves all participants' stores, deduplicates by store identity, orders by `MutationOrder`, and acquires `MutationLock` in that order. Snapshot records keep both the domain container for publication and storage for state capture/restoration. Settlement, escrow, pouch overflow and event/message sequencing remain local to trade/pouch domain code.

### Zero-count and equipment rules stay at their owners

Storage accepts a simple `countToResetTo` constructor option. MoneyPouch seeds and validates its coin sentinel; ShopStock retains original-stock and normalization behavior. Equipment keeps semantic slot mapping and equipment callbacks. It uses storage-only transfer, then runs callbacks, then publishes inventory/equipment updates in the established order.

## Risks / Trade-offs

- **Trade-off:** Concrete domain containers repeat small storage-forwarding members to keep implementation ownership visible. Review whether any low-level operation can leave the broad `IItemContainer` contract in #439, but do not introduce a shared implementation layer to avoid forwarding.
- **Risk:** A concrete wrapper publishes at the wrong time or bypasses domain behavior. → **Mitigation:** Keep storage mutation and publication separate; container methods publish only after storage reports a successful commit, while equipment and trade callbacks remain domain-owned.
- **Risk:** Move/swap/replace and exact state replacement can bypass callback or revision behavior if split between storage and domain code. → **Mitigation:** Have storage return committed changed slots and advance only its private revision; domain wrappers preserve existing publication flags and equipment effects.
- **Risk:** TradeExchange snapshots and storage mutation may restore the right slots but publish through the wrong owner. → **Mitigation:** Snapshot both storage and its owning `IItemContainer`, and retain existing post-commit publication tests.
- **Risk:** MoneyPouch's zero-count coin sentinel and ShopStock's zero-count depleted entries look similar but have different domain rules. → **Mitigation:** Share only the configured reset count; keep sentinel validation and stock normalization in their domain classes.

## Migration Plan

1. Move storage state, the mutation implementation, revision/enumeration, lock/order, and transfer planning/commit into `ItemContainerStorage`; replace base-class transfer with `ItemContainerTransfer` and provider lookup.
2. Migrate all production containers and script-local temporary containers directly to composition. Migrate trade participants and `TradeExchange` together so every participant uses the same storage boundary.
3. Replace inheritance-based test fixtures with composed test containers and split core tests into storage and transfer suites; add focused ShopStock and Equipment characterization.
4. Delete both old base classes and verify no source/test references or replacement implementation base remain.
5. Run focused and affected test suites, solution build, strict OpenSpec validation, repository duplication/quality gate, and diff checks.
6. Audit all item-container interfaces and consumers to ensure contracts contain declarations only, concrete containers own delegation, special interface dispatch remains intact, and the complete branch introduces no new clone pairs.

Rollback is a source revert; there is no data or protocol migration.
