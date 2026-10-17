# Proposal

## Why

`ItemContainer` and `ItemContainerMutationBoundary` still invoke domain publication through a constructor delegate. This hides publication ownership in generic storage code and lets callers mutate domain-owned storage without going through the domain owner.

The existing `ItemContainerChangeTracker` provides the change batches needed to remove that delegate, provided domain-owned mutation and publication remain ordered around the existing transaction boundary.

## What Changes

- Remove publication delegates from generic item-container construction and mutation boundaries.
- Keep `ItemContainerChangeTracker` as the only changed-slot accumulator; raw item containers expose committed batches through `GetSlots()`.
- Expose domain-owned containers through read-only item views and add only the mutation commands used by existing production callers.
- Make domain mutation commands publish their own committed changes. Explicit multi-owner transactions publish through their participating domain owners before the transaction scope is disposed.
- Keep a committed transaction bound after `Commit()` releases physical locks, so domain owners can consume and publish committed changes without an overlapping mutation interleaving. `Dispose()` then releases the binding; disposal before commit still rolls back.
- Keep Equipment lifecycle effects and MoneyPouch typed notifications with their domain owners. Keep rollback cleanup for their pending facts.
- Migrate production call sites and revise the canonical and active item-container specifications to match the callback-free architecture.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `item-container-storage`: publication ownership moves to domain containers; their public item views become read-only; explicit transaction completion retains bindings through caller-owned publication.
- `trading-completion`: terminal trade state is established before commit, and the trade owner publishes participating domains before releasing the committed scope.

## Impact

The change affects the Abstractions item-container and transaction APIs, Inventory/Bank/Reward/Familiar/Shop/Equipment/MoneyPouch owners, existing domain mutation call sites in GameWorld and Scripts, container tests, and active OpenSpec deltas. It adds no package or generic publisher/observer mechanism. PriceChecker and temporary UI-owned containers continue using `ItemContainerChangeTracker` directly.

## Non-goals

- Redesigning item storage algorithms, ChangeTracker semantics, or exact transfer rules.
- Reworking Equipment, MoneyPouch, Shop, or trade domain behavior beyond the publication path required by callback removal.
- Adding a second transaction scope, general event bus, publisher interface, callback registry, or generic mutation gateway.
- Editing archived OpenSpec changes or checking remote PR/CI state.

## Acceptance Criteria

- ItemContainer constructors and ItemContainerMutationBoundary have no generic publication delegate.
- Domain-owned Items properties are read-only and existing production mutations use domain-owned commands.
- All committed Inventory, Bank, Reward, Familiar, Shop, Equipment, and MoneyPouch notifications retain their required ordering, rollback, and failure behavior.
- An explicit transaction can be committed and published by its domain owners before disposal, while foreign mutations remain blocked until disposal.
- Standalone domain operations remain atomic and publish once; raw ItemContainer operations remain directly usable and tracker-driven.
- The requested tests, solution build, strict OpenSpec validation, duplication gate, and diff checks pass.

## Stop Conditions

If callback removal cannot preserve an existing notification or ownership guarantee without adding a disguised callback mechanism or broad generic mutation API, stop before the architectural commit and report the specific conflict and smallest viable alternative.
