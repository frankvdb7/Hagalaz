# Proposal

## Why

Before this change, item storage algorithms, synchronization and revision tracking lived in `BaseItemContainer`, while trade settlement added a second inheritance layer. Composition gives every container one storage owner while keeping gameplay publication, callbacks and transaction coordination at their existing domain boundaries.

## What Changes

- Add one `ItemContainerStorage` implementation for slots, mutations, revision, synchronization, restoration and transfer planning/commit.
- Add a narrow `IItemContainerStorageOwner` infrastructure contract and `ItemContainerTransfer` coordinator.
- Add one concrete `ItemContainer` that implements the contract-only `IItemContainer` API by composing `ItemContainerStorage`.
- Migrate domain containers and test fixtures to compose `ItemContainer`; expose it through the narrow `Items` contract they need. Remove `BaseItemContainer`, `TradeItemContainer`, and `ItemContainerExtensions`.
- Keep `IItemContainer` and `IItemContainerStorageOwner` as contracts only. Trade is a consumer of the generic synchronous mutation and transfer boundary; it MUST NOT be modeled as a capability implemented by ordinary item containers. `ItemContainer` owns generic mutation publication through a domain-supplied callback; domain containers retain their specialized operations and orchestration.
- Preserve transfer behavior, publication timing, trade settlement, equipment callbacks, persistence slots and special zero-count semantics. Ordinary domain container `Items` properties MUST be typed as `IItemContainer` and MUST NOT expose trade-named mutation APIs. `TradeExchange` stages generic storage operations under the existing deterministic locks. Pull forward only the minimum #439 cleanup required to make composition concrete; defer a wider operation-surface redesign.

## Capabilities

### New Capabilities

- `item-container-storage`: Defines storage ownership, mutation and transfer invariants while domain containers retain publication and gameplay behavior.

### Modified Capabilities

None. The change is an internal architecture refactor; existing gameplay behavior remains unchanged.

## Impact

Affected projects are `Hagalaz.Game.Abstractions`, `Hagalaz.Services.GameWorld`, `Hagalaz.Game.Scripts`, and their test projects. No package, persistence schema, protocol or gameplay behavior changes are intended. `IItemContainer` retains normal operations plus neutral exact removal, but no longer owns publication; no trade-specific item-container interface or operation remains.

## Scope Boundary

Do not redesign constructor semantics globally, change item mutability, or broadly revise low-level `Replace`/`ReplaceState`. The #439 overlap is limited to keeping publication out of `IItemContainer` while making composed containers explicitly implement its existing operation contract. Remaining #439 work includes deciding which mutation/query members should eventually leave `IItemContainer`, constructor behavior and the wider public API audit. Stop if preserving existing behavior requires a second mutation implementation, a storage strategy hierarchy, or a gameplay behavior change; revise this proposal before proceeding.

## Acceptance Criteria

- One concrete `ItemContainer` implements the generic container contracts and composes `ItemContainerStorage`; domain containers compose `ItemContainer` rather than forwarding the full generic API.
- Neither old implementation base nor an extension implementation layer exists.
- Storage is the sole implementation of mutation and storage-to-storage transfer algorithms.
- Item-container interfaces contain declarations only; no behavior is inherited through interfaces or a shared extension implementation layer.
- `ItemContainer` delegates storage mechanics to `ItemContainerStorage` and invokes a simple callback after committed generic mutations; domain containers retain specialized publication and callback orchestration.
- Domain containers own events, UI publication, messages, persistence projection and equipment behavior.
- Trade settlement locks composed stores in stable order and keeps offer acceptance revision separate from storage revision.
- Trade is a consumer of the generic synchronous mutation/transfer boundary and MUST NOT be modeled as a capability inherited or implemented by ordinary item containers. Inventory, bank, reward and other generic domain containers MUST NOT expose trade-specific mutation contracts merely because trade can move items through them.
- No `ITradeItemContainer`, trade-specific item-container operation, or trade-named MoneyPouch API remains. Exact staged pouch operations are domain-neutral and used only where the settlement transaction boundary requires them.
- Existing and requested regression suites pass, strict OpenSpec validation passes, and the complete diff passes repository quality checks.
