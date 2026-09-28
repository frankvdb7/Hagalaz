# Proposal

## Why

Item storage algorithms, synchronization and revision tracking currently live in `BaseItemContainer`, while trade settlement adds a second inheritance layer. Composition will give every container one storage owner while keeping gameplay publication, callbacks and transaction coordination at their existing domain boundaries.

## What Changes

- Add one `ItemContainerStorage` implementation for slots, mutations, revision, synchronization, restoration and transfer planning/commit.
- Add a narrow `IItemContainerStorageProvider` infrastructure contract and `ItemContainerTransfer` coordinator.
- Migrate every production consumer and test fixture to own storage directly; remove `BaseItemContainer` and `TradeItemContainer`.
- Preserve current `IItemContainer` and `ITradeItemContainer` APIs, transfer behavior, publication timing, trade settlement, equipment callbacks, persistence slots and special zero-count semantics.

## Capabilities

### New Capabilities

- `item-container-storage`: Defines storage ownership, mutation and transfer invariants while domain containers retain publication and gameplay behavior.

### Modified Capabilities

None. The change is an internal architecture refactor; existing gameplay behavior remains unchanged.

## Impact

Affected projects are `Hagalaz.Game.Abstractions`, `Hagalaz.Services.GameWorld`, `Hagalaz.Game.Scripts`, and their test projects. No package, persistence schema, protocol or gameplay API cleanup is intended. `IItemContainer` and `ITradeItemContainer` remain in place.

## Scope Boundary

Do not remove `OnUpdate`, redesign item-container interfaces or constructor semantics globally, change item mutability, or broadly revise `Replace`/`SetItems`. Stop if preserving existing behavior requires a second mutation implementation, a storage strategy hierarchy, or a change to gameplay semantics; revise this proposal before proceeding.

## Acceptance Criteria

- Every production and test container owns or test-composes `ItemContainerStorage`; neither old implementation base exists.
- Storage is the sole implementation of mutation and storage-to-storage transfer algorithms.
- Domain containers own events, UI publication, messages, persistence projection and equipment behavior.
- Trade settlement locks composed stores in stable order and keeps offer acceptance revision separate from storage revision.
- Existing and requested regression suites pass, strict OpenSpec validation passes, and the complete diff passes repository quality checks.
