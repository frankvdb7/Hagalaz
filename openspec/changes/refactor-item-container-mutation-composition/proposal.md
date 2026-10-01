# Proposal

## Why

Before this change, item storage algorithms, synchronization and revision tracking lived in `BaseItemContainer`, while trade settlement added a second inheritance layer. Composition gives every container one storage owner while keeping gameplay publication, callbacks and transaction coordination at their existing domain boundaries.

## What Changes

- Add one `ItemContainerStorage` implementation for slots, mutations, revision, synchronization, restoration and transfer planning/commit.
- Add a concrete instance-based `ItemContainerMutationBoundary` as the exact-transfer and enlistment entrypoint; use `ItemContainerTransaction` as the sole owner of locking and post-commit publication for coordinated mutations.
- Add one concrete `ItemContainer` that implements the contract-only `IItemContainer` API by composing `ItemContainerStorage`.
- Migrate ordinary domain containers, script objects, and test fixtures to privately own one concrete `ItemContainer`; ordinary domain interfaces expose `IItemContainer Items`. Remove generic container forwarding. Remove `BaseItemContainer`, `TradeItemContainer`, `ITradeItemContainer`, `ItemContainerExtensions`, redundant `GenericContainer`, and script-local generic forwarding wrappers.
- `ItemContainer` owns `ItemContainerStorage` and privately composes a public `IItemContainerMutationBoundary`. Ordinary domain interfaces expose `IItemContainer Items`; Equipment and MoneyPouch own storage and private concrete boundaries and expose only domain-safe operations; Equipment remains a read-only `IContainer<IItem?>`. Delete `IItemContainerStorageOwner` and `ItemContainerTransfer`. Two-container mutation is instance-based through boundary interfaces; multi-container settlement composes a short-lived `ItemContainerTransaction` implementing `IItemContainerTransaction`. Container infrastructure MUST NOT require runtime casts to recover storage or concrete boundaries.
- Preserve transfer behavior, publication timing, trade settlement, equipment callbacks, persistence slots and special zero-count semantics. `IItemContainer` remains a declaration-only generic contract and MUST NOT reference concrete `ItemContainer`. `IEquipmentContainer` MUST NOT expose generic update callbacks, raw publication delegates, or publication-only methods; defer equipment mutation until the enclosing operation reaches its correct publication point. Pull forward only the minimum #439 cleanup required to make composition concrete; defer a wider operation-surface redesign.
- Trade settlement, refund, escrow recovery, and money-offer coordination MUST be performed by a concrete `TradeExchange` instance explicitly composed and owned by `TradingCharacterScript`. `TradeExchange` MUST own its `IItemBuilder` dependency, depend on container abstractions, and MUST NOT expose static orchestration methods or access raw storage/publication internals.

## Capabilities

### New Capabilities

- `item-container-storage`: Defines storage ownership, mutation and transfer invariants while domain containers retain publication and gameplay behavior.

### Modified Capabilities

- `item-container-storage`: Defines transaction point-of-no-return and exhaustive post-commit work, atomic MoneyPouch/bank/shop/duel economic mutations, composed duel stake operations, and equipment replacement preflight while preserving domain policy.

## Impact

Affected projects are `Hagalaz.Game.Abstractions`, `Hagalaz.Services.GameWorld`, `Hagalaz.Game.Scripts`, and their test projects. No package, persistence schema, or protocol changes are intended. Existing amount, capacity, price, message, partial-count, and custom equipment-command policies remain domain-owned while the named cross-container operations become atomic or preflighted. Failed reversible transactions restore state without publishing rollback notifications. Once a successful operation callback returns and participant locks are released, storage is committed; post-commit failures do not roll it back and all remaining post-commit actions are attempted. `IItemContainer` retains normal operations plus neutral exact removal, but no longer owns publication; no trade-specific item-container interface or operation remains.

## Scope Boundary

Do not redesign constructor semantics globally, change item mutability, or broadly revise low-level `Replace`/`ReplaceState`. The #439 overlap is limited to keeping publication out of `IItemContainer` while making composed containers explicitly implement its existing operation contract. Remaining #439 work includes deciding which mutation/query members should eventually leave `IItemContainer`, constructor behavior and the wider public API audit. Stop if preserving existing behavior requires a second mutation implementation, a storage strategy hierarchy, or a gameplay behavior change; revise this proposal before proceeding.

## Acceptance Criteria

- One concrete `ItemContainer` implements the generic container contracts and composes `ItemContainerStorage`; domain containers compose `ItemContainer` rather than forwarding the full generic API.
- Inventory, Bank, Reward, FamiliarInventory, ShopStock, TradeOffer, Duel, and Price Checker use the concrete generic implementation instead of reproducing the generic container contract or forwarding its operations.
- Ordinary domain interfaces expose `IItemContainer Items` while implementations privately own concrete `ItemContainer` components.
- Special domains such as MoneyPouch and Equipment compose `ItemContainerStorage` directly and expose only their domain API; Equipment itself remains a read-only `IContainer<IItem?>`.
- Domain interfaces expose ordinary composed `IItemContainer` components but do not expose special-domain boundaries, raw storage, or concrete transaction infrastructure. `IItemContainer` does not refer to concrete `ItemContainer`.
- `TradingCharacterScript` owns a `TradeExchange` collaborator; trade settlement and recovery use instance methods, and `TradeExchange` owns `IItemBuilder` without accepting concrete containers or raw storage.
- Neither old implementation base nor an extension implementation layer exists.
- Storage is the sole implementation of mutation and storage-to-storage transfer algorithms.
- `IItemContainer` is declaration-only; no domain object implements it or forwards its generic surface. No behavior is inherited through interfaces or a shared extension implementation layer.
- `ItemContainer` delegates storage mechanics to `ItemContainerStorage` and invokes a simple callback after committed generic mutations; domain containers retain specialized publication and callback orchestration.
- Domain containers own events, UI publication, messages, persistence projection and equipment behavior.
- Exact transfer and multi-container mutation use `ItemContainerTransaction` for deterministic lock ordering, snapshots, rollback, and publication; offer acceptance revision remains separate from storage revision.
- `ItemContainerTransaction` owns execution and changed-slot bookkeeping. `IItemContainerTransaction.Include` enlists participants before execution; storage-staging methods and `OnCommitted` are used inside the callback passed to concrete `TryExecute`. Domain storage changes are staged through that transaction. MoneyPouch stages pouch and inventory storage in the same transaction.
- Post-commit work runs after unlocking in this order: every pre-publication callback, every changed participant publisher in enlistment order, then every `OnCommitted` callback. Each action is attempted once. One failure is rethrown with its original stack; multiple failures are aggregated. Rollback is limited to the locked reversible phase.
- MoneyPouch Add/Remove/AddFromInventory/MoveToInventory, bank deposit from pouch, shop buy/sell, and duel stake/unstake/refund use one mutation boundary or transaction for each economic ownership change. Existing amount, capacity, messaging, and partial-count policies remain domain-owned.
- Duel stake movement is composed through a concrete `DuelStakeExchange`; it owns no duel UI/session state. Cancellation retains both stake containers and the active session if a combined refund cannot commit.
- Equipment replacement preflights every conflicting item's `CanUnEquipItem` permission before the first mutation and preserves the existing custom `UnEquipItem` command behavior.
- Generic mutation infrastructure exposes exact transfer but no partial bulk movement. `FamiliarInventoryContainer.WithdrawAvailableToInventory` owns the partial-success policy: each item is attempted, fitting items move together, and items that do not fit remain in familiar storage.
- Concrete `ItemContainerStorage` and `ItemContainerMutationBoundary` remain assembly-internal; cross-assembly gameplay composition uses their public contracts.
- Trade is a consumer of the generic synchronous mutation/transfer boundary and MUST NOT be modeled as a capability inherited or implemented by ordinary item containers. Inventory, bank, reward and other generic domain containers MUST NOT expose trade-specific mutation contracts merely because trade can move items through them.
- No `ITradeItemContainer`, trade-specific item-container operation, or trade-named MoneyPouch API remains. Exact staged pouch operations are domain-neutral and used only where the settlement transaction boundary requires them.
- Do not add a generic transaction factory/context, transfer shortcut, shop transaction, or `DuelStakeExchange` interface. UI orchestration delegates domain mutations; only complex multi-container flows construct `ItemContainerTransaction`.
- Existing and requested regression suites pass, strict OpenSpec validation passes, and the complete diff passes `git diff --check`. jscpd duplication cleanup is a separate follow-up.
