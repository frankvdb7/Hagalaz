# Proposal

## Why

`EquipmentContainer.TryMoveTo()` always starts a transaction, so it cannot compose with a caller-owned transaction even though the same operation is already supported by Equipment's transaction boundary. This makes the public Equipment transfer API inconsistent with `IItemContainer.TryTransferTo()`.

## What Changes

- Make `TryMoveTo()` own a short transaction when neither Equipment nor destination is enlisted, and participate when both belong to the same current-thread transaction.
- Reject partial or conflicting participation before changing either container.
- Keep Equipment lifecycle completion deferred to the transaction that owns the move.
- Document that `EquipItem()` and `UnEquipItem()` remain standalone gameplay commands and do not participate in a caller-owned transaction.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `item-container-storage`: specify composable `TryMoveTo()` participation and the standalone ownership of Equipment gameplay commands.

## Impact

The change affects `IEquipmentContainer` API documentation, `EquipmentContainer.TryMoveTo()`, focused GameWorld regression tests, and the canonical item-container storage specification. It reuses `ItemContainer.TryTransferTo()` participation checks, `ItemContainerMutationBoundary.TryTransferTo()`, and existing typed Equipment completion.

## Scope and Non-goals

In scope: transaction ownership for exact Equipment-to-container movement and clear documentation of Equipment command ownership.

Out of scope: making `EquipItem()` or `UnEquipItem()` composable; adding a generic begin-or-join helper; changing item transfer rules, Equipment callbacks, transaction infrastructure, or read-only `Items` access.

## Acceptance Criteria

- A standalone successful `TryMoveTo()` commits and publishes through its own transaction.
- `TryMoveTo()` inside a transaction containing Equipment and destination stages the move and defers callbacks/publication until the caller commits; disposal without commit restores both containers.
- Partial enlistment and participants in different active transactions throw before mutation.
- `EquipItem()` and `UnEquipItem()` are documented as standalone commands.
- Focused Equipment transfer tests, strict OpenSpec validation, and `git diff --check` pass.

## Stop Conditions

Stop and reassess if this requires a new public participant API, dynamic enlistment, or changes to the established exact-transfer semantics.
