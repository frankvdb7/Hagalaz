# Proposal

## Why

Generic `ItemContainer` currently accepts a domain completion owner in internal constructors and wires that owner into a new mutation boundary itself. This makes the generic container know about domain completion even though the boundary owns completion coordination.

## What Changes

- Replace the completion-owner constructor overloads with one internal constructor that accepts an existing mutation boundary.
- Preserve both public `ItemContainer` constructor signatures and make them compose their usual storage and ownerless mutation boundary.
- Move owner-aware storage/boundary composition into the five domain containers that currently pass themselves to `ItemContainer`.
- Update owner-aware test construction to use the same explicit composition.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. Observable item-container, transaction, and publication behavior remains unchanged; `skip_specs: true` is set for this implementation-only refactor.

## Impact

This affects internal construction in Game Abstractions and the GameWorld domain owners that use completion publication. It does not change public constructors, public container contracts, storage algorithms, mutation boundaries, transaction behavior, or gameplay semantics.

## Non-Goals

- Do not change synchronization, storage mutation, transaction, completion, or publication behavior.
- Do not expose internal storage, mutation boundaries, or completion-owner contracts.
- Do not migrate Equipment or MoneyPouch, which already compose their storage and mutation boundaries.
- Do not modify canonical behavior specifications when no observable behavior changes.

## Acceptance Criteria

- `ItemContainer` no longer names or accepts `IItemContainerCompletionOwner`.
- Each affected domain owner attaches its completion implementation to one boundary and wraps that same boundary in `ItemContainer`.
- Existing public constructor behavior and initial storage configuration are preserved.
- Existing behavior tests pass after their owner-aware construction is migrated.

## Stop Conditions

Stop if the refactor requires a public API change, a second storage/boundary, transaction or publication behavior changes, or changes to unrelated item-container algorithms.
