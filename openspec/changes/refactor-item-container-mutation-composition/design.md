# Design

## Context

`BaseItemContainer` currently owns slot state and mutation algorithms. Its lock and stable order are shared by ordinary mutation and #437 transfer. `TradeItemContainer` exposes the same synchronization boundary and wraps the shared range/removal helpers for trade settlement. See proposal.md and the `item-container-storage` spec delta for goals and invariants.

## Goals / Non-Goals

**Goals:**

- Move the single authoritative mutation implementation into `ItemContainerStorage` and use composition in all current production containers.
- Keep the existing public container contracts and domain publication behavior.
- Keep `TradeExchange` as the owner of trade-specific settlement and multi-container restoration.

**Non-Goals:**

- Change `IItemContainer`, `ITradeItemContainer`, `OnUpdate`, or general constructor semantics beyond changes required to remove the old base classes.
- Redesign item mutability, shop transaction behavior, or trade acceptance rules.

## Decisions

### One concrete storage, plus a provider boundary

Create one public sealed `ItemContainerStorage` in Abstractions. The public `IItemContainerStorageProvider` exposes the concrete store only to cross-assembly infrastructure such as transfer and trade coordination. Do not add a storage interface or expose a `Storage` member on `IItemContainer`.

### Store methods mutate and return changed slots

Move add, remove, exact removal, range insertion, replace/move/swap/sort/clear, exact-state replacement, queries, and version-aware enumeration into storage. Mutators return changed slots rather than invoking domain code. `AddRange` applies the existing simulation against a cloned slot state and commits only after full validation, eliminating trade-only rollback around a partial apply.

### Transfer planning belongs to storage

Move the existing #437 plan/simulate/commit algorithm into a storage-to-storage static operation. `ItemContainerTransfer` resolves provider stores and publishes updates after a successful commit; its storage-only entry point returns slot sets so equipment can run domain callbacks before publication. `AddAndRemoveFrom` delegates to the coordinator while preserving its per-source-slot exact-transfer behavior.

### Domain containers forward the unchanged contract

Each container implements its current gameplay interface and `IItemContainerStorageProvider` directly, owns one private store, forwards `IItemContainer` queries and mutations, then calls its own `OnUpdate` according to existing flags and success semantics. Do not add a common forwarding base class or default-interface machinery.

Hydration remains local: domain code validates persisted entries, builds capacity-sized slot state, and invokes a narrow storage replacement operation. Normal restored counts remain positive; MoneyPouch validates its coin-995 sentinel and allows count zero.

### Trade settlement locks stores directly

`TradeExchange` resolves all participants' stores, deduplicates by store identity, orders by `MutationOrder`, and acquires `MutationLock` in that order. Snapshot records keep both the domain container for publication and storage for state capture/restoration. Settlement, escrow, pouch overflow and event/message sequencing remain local to trade/pouch domain code.

### Zero-count and equipment rules stay at their owners

Storage accepts a simple `countToResetTo` constructor option. MoneyPouch seeds and validates its coin sentinel; ShopStock retains original-stock and normalization behavior. Equipment keeps semantic slot mapping and equipment callbacks. It uses storage-only transfer, then runs callbacks, then publishes inventory/equipment updates in the established order.

## Risks / Trade-offs

- **Risk:** Direct composition repeats forwarding methods across containers. → **Mitigation:** Keep forwards explicit and mechanical; do not introduce inheritance or code generation.
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
