# Spec Delta

## MODIFIED Requirements

### Requirement: Equipment exposes semantic mutation operations
`IEquipmentContainer` MUST expose domain-specific mutation operations rather than generic `Add`, `Replace`, `Remove`, and `Clear` commands. Direct restoration, in-place transformation, removal, and clearing MUST use explicitly named equipment operations. Equipment replacement MUST require the caller's expected current item instance so a stale command cannot replace a different equipped item. Simple Equipment storage operations MUST participate when Equipment storage is enlisted; typed lifecycle completion MUST be deferred through the owning transaction. When unbound, lifecycle completion and publication occur immediately. `TryMoveTo(...)` MUST use direct two-boundary ownership when standalone, participate without committing or disposing when both boundaries belong to the same active current-thread transaction, and throw before mutation for partial or conflicting participation. Full removal MUST defer lifecycle completion until the owning transaction commits or run the standalone lifecycle effect before publication. `EquipItem()` and `UnEquipItem()` MUST remain standalone gameplay commands and MUST NOT participate in a caller-owned transaction over their required storage.

#### Scenario: Stale equipment replacement is rejected
- **WHEN** the expected item instance no longer occupies the requested slot
- **THEN** replacement returns false without changing storage, publishing an equipment update, or running lifecycle callbacks

#### Scenario: Equipment storage mutation participates in an active scope
- **WHEN** restoration, replacement, removal, or clearing is called while Equipment storage belongs to an active transaction
- **THEN** the storage mutation participates in that transaction, with lifecycle completion and publication deferred until commit

#### Scenario: Equipment transfer uses direct standalone ownership
- **WHEN** `TryMoveTo(...)` moves an equipped item while neither Equipment nor destination belongs to a current-thread transaction
- **THEN** it atomically mutates both storages without creating an `ItemContainerTransaction`, runs `OnUnequipped` before publication for full removal, and retains both boundaries' ownership through publication

#### Scenario: Equipment transfer participates in a caller-owned transaction
- **WHEN** `TryMoveTo(...)` moves an equipped item while Equipment and destination belong to the same active current-thread transaction
- **THEN** it stages the move and defers lifecycle completion and publication until the caller commits

#### Scenario: Equipment transfer rejects partial or conflicting participation
- **WHEN** only one required storage is enlisted or required storages belong to different active current-thread transactions
- **THEN** `TryMoveTo(...)` throws before changing either storage

#### Scenario: Equipment gameplay commands remain standalone
- **WHEN** a caller invokes `EquipItem()` or `UnEquipItem()` for an operation requiring inventory and Equipment
- **THEN** the command owns its workflow and does not join a caller-owned transaction over those storages

### Requirement: Cross-container transfers commit both stores atomically
`IItemContainer.TryTransferTo(...)` MUST provide one atomic exact cross-container operation using the single low-level `ItemContainerStorage` transfer algorithm. When neither storage participates in a current-thread transaction, the method MUST acquire both mutation boundaries in deterministic lock order, mutate both storages once, and retain standalone publication ownership for both until publication completes; it MUST NOT create an `ItemContainerTransaction`. When both storages already belong to the same active current-thread transaction, the method MUST participate without creating, committing, or disposing that transaction. Partial or conflicting participation MUST be rejected before mutation. A valid transfer rejection MUST return false without mutating either storage. Success MUST advance each storage revision once. Standalone publication MUST occur after both locks are released in source-then-destination order. Each supplied pre-publication action and both publishers MUST be attempted once after commit even if an earlier stage throws. Failures MUST leave storage committed and release both ownership claims; one failure propagates as its original exception and multiple failures are aggregated. `ItemContainerMutationBoundary.TryTransferTo(...)` MUST require both storages to already belong to the same active current-thread transaction.

#### Scenario: Standalone transfer uses direct two-boundary ownership
- **WHEN** neither storage participates in a current-thread transaction and the source has the requested quantity while the destination can accept the exact result
- **THEN** `TryTransferTo(...)` mutates both stores atomically without creating an `ItemContainerTransaction`, releases both physical locks, and publishes source before destination while retaining both logical ownership claims

#### Scenario: Standalone transfer publication failure leaves storage committed
- **WHEN** the source publisher throws after a successful standalone transfer
- **THEN** the destination publisher is attempted once, both stores remain changed, both ownership claims are released, and the original publication exception propagates

#### Scenario: Standalone transfer attempts publishers after a before-publication failure
- **WHEN** a supplied before-publication action throws after the transfer commits
- **THEN** source and destination publishers are both attempted once in order, ownership is released, and the single or multiple failures propagate

#### Scenario: Standalone transfer aggregates independent publisher failures
- **WHEN** both source and destination publishers throw
- **THEN** both are attempted once and an `AggregateException` retains both failures

#### Scenario: Transfer participates in an existing transaction
- **WHEN** both storages belong to the same active current-thread transaction
- **THEN** `TryTransferTo(...)` stages the transfer without creating, committing, or disposing that transaction, and the caller controls publication or rollback

#### Scenario: Caller rollback restores a transfer
- **WHEN** a transfer succeeds inside an existing transaction that is disposed without commit
- **THEN** both stores are restored and neither container publishes a committed mutation

#### Scenario: Partial or conflicting enlistment rejects
- **WHEN** only one storage is enlisted or source and destination belong to different active transactions
- **THEN** `TryTransferTo(...)` throws before either store changes and does not create a nested transaction

#### Scenario: Transfer fails validation
- **WHEN** the source quantity is insufficient or the destination cannot accept the result
- **THEN** neither store changes and neither container publishes a committed mutation

#### Scenario: Internal transfer boundaries require one active transaction
- **WHEN** `ItemContainerMutationBoundary.TryTransferTo(...)` is called without a transaction shared by source and destination, or from a thread other than the transaction owner
- **THEN** it throws before either store changes

#### Scenario: Opposite transfers acquire locks consistently
- **WHEN** two operations transfer in opposite directions between the same stores
- **THEN** both acquire boundaries in the same stable lock order, and an independent operation waits until the current operation releases its ownership

#### Scenario: Transfer is exposed by the container
- **WHEN** a caller inspects the public `IItemContainer` contract
- **THEN** it finds `TryTransferTo(...)` on the interface; transaction membership is declared by passing the aggregate object directly to `Begin(...)`
