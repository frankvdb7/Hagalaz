# Proposal

## Why

Standalone exact transfers currently create a short `ItemContainerTransaction` even though the operation only needs to coordinate two storages. The extra transaction lifecycle obscures the boundary that owns synchronization and publication. Equipment transfers have the same issue while also needing their unequip lifecycle effect before publication.

## What Changes

- Make standalone `IItemContainer.TryTransferTo` use direct atomic ownership of both mutation boundaries, without creating a transaction.
- Keep transfer participation in an explicitly created transaction unchanged.
- Apply the same standalone ownership path to `EquipmentContainer.TryMoveTo`, preserving validation under both locks and running `OnUnequipped` before publication.
- Document and test ownership, ordering, contention, publication, and failure behavior.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `item-container-storage`: Specify direct standalone two-boundary transfer ownership and Equipment transfer lifecycle ordering.

## Impact

The change affects `ItemContainerMutationBoundary`, `ItemContainer`, `EquipmentContainer`, their focused tests, public XML documentation, and the canonical item-container storage specification. Existing `ItemContainerStorage.TryTransferTo` and explicit multi-operation transactions remain the authoritative transfer algorithm and transaction mechanism.

## Acceptance Criteria

- Standalone item and Equipment transfers do not begin or commit an `ItemContainerTransaction`.
- A transfer inside an existing shared transaction participates without committing, publishing, or releasing its locks.
- Standalone transfers acquire boundaries in `LockOrder`, retain both publication ownership claims through lifecycle and publication, publish source before destination, and release both claims on success or failure.
- Equipment validates the expected instance under both locks and invokes `OnUnequipped` before either publisher only for full removal.
- Focused and repository validation passes, with any unavailable or pre-existing failure reported.

## Non-Goals

- Redesigning `ItemContainerTransaction`, MoneyPouch, or other domain workflows.
- Adding transaction propagation, a coordinator, a new public API, or another storage-transfer algorithm.
- Changing EquipItem, UnEquipItem, or interactive equipment commands.
- Committing, pushing, or checking the remote PR.

## Stop Conditions

- Stop if the existing storage transfer algorithm cannot preserve the required mutation semantics without adding a second storage algorithm or speculative rollback engine.
- Stop if the implementation requires changing the public transfer signatures or altering explicit transaction semantics.
