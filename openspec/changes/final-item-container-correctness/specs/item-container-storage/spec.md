# Spec Delta

## MODIFIED Requirements

### Requirement: Cross-container transfers commit both stores atomically
`IItemContainer.TryTransferTo(...)` MUST provide one atomic exact cross-container operation using the single low-level `ItemContainerStorage` transfer algorithm. When neither storage participates in a current-thread transaction, the method MUST acquire both mutation boundaries in deterministic lock order, mutate both storages once, and retain standalone publication ownership for both until publication completes; it MUST NOT create an `ItemContainerTransaction`. When both storages already belong to the same active current-thread transaction, the method MUST participate without creating, committing, or disposing that transaction. Partial or conflicting participation MUST be rejected before mutation. A valid transfer rejection MUST return false without mutating either storage. Success MUST advance each storage revision once. Standalone publication MUST occur after both locks are released, source before destination, and every supplied pre-publication action and both publishers MUST be attempted once even if an earlier one throws. Publication failure MUST leave storage committed, MUST NOT trigger retry, and MUST release both ownership claims. `ItemContainerMutationBoundary.TryTransferTo(...)` MUST require both storages to already belong to the same active current-thread transaction.

#### Scenario: Standalone transfer uses direct two-boundary ownership
- **WHEN** neither storage participates in a current-thread transaction and the source has the requested quantity while the destination can accept the exact result
- **THEN** `TryTransferTo(...)` mutates both stores atomically without creating an `ItemContainerTransaction`, releases both physical locks, and publishes source before destination while retaining both logical ownership claims

#### Scenario: Standalone transfer publication failure leaves storage committed
- **WHEN** the source publisher throws after a successful standalone transfer
- **THEN** the destination publisher is still attempted once, both stores remain changed, both ownership claims are released, and the original publication exception propagates

#### Scenario: Standalone transfer attempts publishers after a before-publication failure
- **WHEN** a supplied pre-publication action throws after storage commits
- **THEN** source and destination publishers are both attempted once in order, ownership is released, and the original failure propagates unless another stage also fails

#### Scenario: Standalone transfer aggregates independent publisher failures
- **WHEN** multiple independent pre-publication or publisher stages throw
- **THEN** all stages are attempted once and a flattened `AggregateException` retains the failures in encounter order

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
- **THEN** their owner scopes acquire store locks in the same stable order, and an independent scope waits until the current scope releases its locks

#### Scenario: Transfer is exposed by the container
- **WHEN** a caller inspects the public `IItemContainer` contract
- **THEN** it finds `TryTransferTo(...)` on the interface; transaction membership is declared by passing the aggregate object directly to `Begin(...)`

### Requirement: Commit completes domain effects and publication in order
Commit MUST run pre-publication completion, changed container publication, then post-publication completion. Completion owners run once per owner-bearing resolved boundary in participant encounter and contribution order before lock sorting; each owner processes its facts in FIFO order, with no cross-owner fact-interleaving guarantee. Container publication uses its existing observable order independently of lock order. Every independent owner and changed boundary in each phase MUST be attempted once even if an earlier operation fails. Pending facts MUST be discarded before bindings are released. A failure within one owner's own fact processing MAY stop that owner's remaining facts, but MUST NOT prevent later independent owners from being attempted.

An exception after the irreversible point leaves storage committed and is never retried. A single failure propagates as its original exception; multiple failures propagate as a flattened `AggregateException` containing the failures in encounter order. Validation needed to determine whether a mutation is allowed MUST happen before Commit. The transaction MUST NOT store executable domain callbacks or expose callback registration.

#### Scenario: Container publication fails
- **WHEN** one changed container publisher throws
- **THEN** later changed container publishers and every post-publication completion owner are still attempted once, storage remains committed, and the failure propagates after cleanup

#### Scenario: Post-publication completion fails
- **WHEN** one completion owner throws after container publication succeeds
- **THEN** later independent owners are attempted once, the failed owner's remaining facts are discarded, and storage remains committed

#### Scenario: Pre-publication completion failure does not suppress later phases
- **WHEN** one pre-publication completion owner throws
- **THEN** later owners, changed container publishers, and post-publication completion owners are attempted once

#### Scenario: Multiple committed stages fail
- **WHEN** more than one independent hook, publisher, or completion owner throws
- **THEN** every independent stage is attempted once and a flattened `AggregateException` retains all failures in encounter order

#### Scenario: Equipment hooks and publication both fail
- **WHEN** an Equipment hook batch and normal publication both throw
- **THEN** all required Equipment hooks and independent container publishers are attempted, and an `AggregateException` retains both original failures

#### Scenario: Rollback discards owner completion
- **WHEN** Equipment and pouch mutations record pending effects but the scope exits without Commit
- **THEN** storage is restored, pending facts are discarded, and a later unrelated scope runs none of those effects

#### Scenario: Completion owners follow resolved boundary order
- **WHEN** participants are supplied in an order different from lock acquisition and mutation order
- **THEN** owner-bearing resolved boundaries complete in participant encounter and contribution order

#### Scenario: One owner preserves its pending fact order
- **WHEN** one completion owner records multiple facts in a transaction
- **THEN** that owner completes its facts in the order recorded

#### Scenario: Completion starts a disjoint scope
- **WHEN** post-unlock completion synchronously starts a scope over storage not owned by the committing transaction
- **THEN** the disjoint scope can acquire, mutate, and complete before the outer transaction releases its existing bindings

#### Scenario: Completion cannot re-enter an overlapping scope
- **WHEN** committed completion attempts same-thread Begin or mutation against storage still bound to the committing transaction
- **THEN** it throws `InvalidOperationException` without altering the committed scope

## ADDED Requirements

### Requirement: Equipment commands revalidate expected item instances
Equipment replacement and unequip commands MUST revalidate the item instances captured before eligibility callbacks while owning all required storage boundaries, before performing any storage mutation.

#### Scenario: Weapon replacement rejects stale incoming or conflicting equipment
- **WHEN** the incoming inventory instance, captured weapon, or captured shield changes during preflight
- **THEN** replacement returns false without consuming an equivalent item, changing equipment, or running additional lifecycle callbacks or publication

#### Scenario: Two-handed replacement validates both equipment slots
- **WHEN** a two-handed replacement captured a weapon and shield and either slot changes before transaction ownership
- **THEN** replacement rejects without recomputing from the new equipment state

#### Scenario: Unequip rejects a stale equipped instance
- **WHEN** the requested equipped instance changes during `CanUnEquipItem`
- **THEN** unequip returns false without transferring the replacement item or emitting an inventory-full message

#### Scenario: Unequip selects its preferred slot under ownership
- **WHEN** unequip is given an in-range preferred inventory slot
- **THEN** it uses that slot if still empty after acquiring both storage boundaries and otherwise uses the normal destination selection
