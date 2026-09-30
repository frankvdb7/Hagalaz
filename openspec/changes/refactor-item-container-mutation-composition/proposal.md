# Proposal

## Why

Before this change, item storage algorithms, synchronization and revision tracking lived in `BaseItemContainer`, while trade settlement added a second inheritance layer. Composition gives every container one storage owner while keeping gameplay publication, callbacks and transaction coordination at their existing domain boundaries.

## What Changes

- Add one `ItemContainerStorage` implementation for slots, mutations, revision, synchronization, restoration and transfer planning/commit.
- Add a concrete instance-based `ItemContainerMutationBoundary` that owns synchronization and post-commit publication for one storage; add a short-lived `ItemContainerTransaction` for multi-boundary work.
- Add one concrete `ItemContainer` that implements the contract-only `IItemContainer` API by composing `ItemContainerStorage`.
- Migrate ordinary domain containers, script objects, and test fixtures to own one concrete `ItemContainer`; ordinary domain interfaces expose `ItemContainer Items`. Remove generic container forwarding. Remove `BaseItemContainer`, `TradeItemContainer`, `ITradeItemContainer`, `ItemContainerExtensions`, redundant `GenericContainer`, and script-local generic forwarding wrappers.
- `ItemContainer` owns `ItemContainerStorage` and its public `Mutations` boundary. Equipment and MoneyPouch own storage and private boundaries, and expose only domain-safe operations; Equipment remains a read-only `IContainer<IItem?>`. Delete `IItemContainerStorageOwner` and `ItemContainerTransfer`. Two-container mutation is instance-based through boundaries; multi-container settlement composes a short-lived `ItemContainerTransaction`. Container infrastructure MUST NOT require runtime casts to recover storage.
- Preserve transfer behavior, publication timing, trade settlement, equipment callbacks, persistence slots and special zero-count semantics. `IItemContainer` remains a declaration-only generic contract and MUST NOT reference concrete `ItemContainer`. `IEquipmentContainer` MUST NOT expose generic update callbacks or raw publication delegates; it may expose named domain operations needed to publish a final state after a larger character operation. Pull forward only the minimum #439 cleanup required to make composition concrete; defer a wider operation-surface redesign.

## Capabilities

### New Capabilities

- `item-container-storage`: Defines storage ownership, mutation and transfer invariants while domain containers retain publication and gameplay behavior.

### Modified Capabilities

None. The change is an internal architecture refactor; existing gameplay behavior remains unchanged.

## Impact

Affected projects are `Hagalaz.Game.Abstractions`, `Hagalaz.Services.GameWorld`, `Hagalaz.Game.Scripts`, and their test projects. No package, persistence schema, protocol or successful-operation gameplay behavior changes are intended. Failed multi-container transactions restore state without publishing rollback notifications because no transaction committed. `IItemContainer` retains normal operations plus neutral exact removal, but no longer owns publication; no trade-specific item-container interface or operation remains.

## Scope Boundary

Do not redesign constructor semantics globally, change item mutability, or broadly revise low-level `Replace`/`ReplaceState`. The #439 overlap is limited to keeping publication out of `IItemContainer` while making composed containers explicitly implement its existing operation contract. Remaining #439 work includes deciding which mutation/query members should eventually leave `IItemContainer`, constructor behavior and the wider public API audit. Stop if preserving existing behavior requires a second mutation implementation, a storage strategy hierarchy, or a gameplay behavior change; revise this proposal before proceeding.

## Acceptance Criteria

- One concrete `ItemContainer` implements the generic container contracts and composes `ItemContainerStorage`; domain containers compose `ItemContainer` rather than forwarding the full generic API.
- Inventory, Bank, Reward, FamiliarInventory, ShopStock, TradeOffer, Duel, and Price Checker use the concrete generic implementation instead of reproducing the generic container contract or forwarding its operations.
- Ordinary domain interfaces expose concrete `ItemContainer Items` while implementations own those components.
- Special domains such as MoneyPouch and Equipment compose `ItemContainerStorage` directly and expose only their domain API; Equipment itself remains a read-only `IContainer<IItem?>`.
- Domain interfaces expose ordinary composed `ItemContainer` components but do not expose special-domain boundaries or raw storage. `IItemContainer` does not refer to concrete `ItemContainer`.
- Neither old implementation base nor an extension implementation layer exists.
- Storage is the sole implementation of mutation and storage-to-storage transfer algorithms.
- `IItemContainer` is declaration-only; no domain object implements it or forwards its generic surface. No behavior is inherited through interfaces or a shared extension implementation layer.
- `ItemContainer` delegates storage mechanics to `ItemContainerStorage` and invokes a simple callback after committed generic mutations; domain containers retain specialized publication and callback orchestration.
- Domain containers own events, UI publication, messages, persistence projection and equipment behavior.
- Two-container mutation is coordinated by `ItemContainerMutationBoundary`; multi-container trade settlement uses `ItemContainerTransaction`, with deterministic lock ordering and offer acceptance revision separate from storage revision.
- Trade is a consumer of the generic synchronous mutation/transfer boundary and MUST NOT be modeled as a capability inherited or implemented by ordinary item containers. Inventory, bank, reward and other generic domain containers MUST NOT expose trade-specific mutation contracts merely because trade can move items through them.
- No `ITradeItemContainer`, trade-specific item-container operation, or trade-named MoneyPouch API remains. Exact staged pouch operations are domain-neutral and used only where the settlement transaction boundary requires them.
- Existing and requested regression suites pass, strict OpenSpec validation passes, and the complete diff passes repository quality checks, including zero new jscpd clone pairs.
