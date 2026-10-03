# Item container storage delta

## ADDED Requirements

### Requirement: Transaction commit and publication are explicit phases
`ItemContainerTransaction` MUST expose one storage-commit method and one publication method without execution overloads or arbitrary post-commit callback registration. Commit MUST preserve deterministic locking, snapshots, exact rollback, and revision restoration. Committed status MUST become visible before any publication. Domain owners MUST execute their required effects directly in the established phase. Publication MUST run affected participants in registration order at most once and propagate the first exception immediately without collection or aggregation. A failed transaction or repeated publication MUST emit no additional effects.

#### Scenario: Storage commits before publication
- **WHEN** all staged mutations succeed
- **THEN** committed status is true and storage has its final contents before the caller executes domain effects or publishes changes

#### Scenario: Staging fails
- **WHEN** staging returns false or throws
- **THEN** every affected store and revision is restored, committed status is false, and publication emits no effects

#### Scenario: A participant publisher fails
- **WHEN** one participant throws during publication
- **THEN** later participants are skipped, storage remains committed, the original exception propagates directly, and a repeated publication emits nothing

### Requirement: Explicit economic publication preserves domain behavior
Money-pouch staging MUST return immutable publication facts on success and no facts on failure. Successful pouch messages and events MUST retain their existing amounts and previous-count values and follow participant publication. A pouch addition that only changes inventory MUST NOT publish a pouch effect. Equipment MUST attempt its lifecycle and profile effects after commit and before participant publication using its existing lifecycle cleanup policy. Shop purchases MUST sort stock and send the purchase event after successful participant and pouch publication. Transaction, pouch, and shop publication MUST propagate the first exception directly and stop later publication, without undoing committed storage.

#### Scenario: Pouch removal consumes inventory overflow
- **WHEN** a committed exact removal consumes pouch and inventory coins
- **THEN** the removal message retains the full requested amount and the pouch event retains its pre-operation count

#### Scenario: Equipment lifecycle fails after commit
- **WHEN** an equipment lifecycle effect throws after storage commits
- **THEN** later required lifecycle effects and transaction publication are still attempted under the equipment owner's existing cleanup policy and storage is not rolled back

#### Scenario: Shop publication fails after purchase commit
- **WHEN** container or pouch publication throws after a purchase commits
- **THEN** later stock sorting and the purchase event are skipped, storage remains committed, and the original publication exception propagates directly

## MODIFIED Requirements

### Requirement: Storage and trade revisions have distinct purposes
Storage MUST own its mutation revision, which invalidates active enumerators after committed storage changes. `ItemContainerTransaction` MUST own lock ordering; `ItemContainerMutationBoundary.TryTransferTo` MUST delegate exact transfers to a short-lived transaction. TradeExchange MUST use a short-lived transaction rather than acquire storage locks directly. A trade offer's acceptance `Revision` MUST remain domain-owned and MUST advance according to its existing publication semantics, independently of storage revision.

#### Scenario: Storage mutation invalidates enumeration
- **WHEN** storage changes after an enumerator is created
- **THEN** the enumerator detects the storage revision mismatch

#### Scenario: Empty bulk addition is not a mutation
- **GIVEN** an item container and an enumerator created before the operation
- **WHEN** `TryAddRange` receives no effective items, including an empty range or a range containing only null entries
- **THEN** the operation succeeds with no changed slots
- **AND** storage contents and mutation revision remain unchanged
- **AND** the existing enumerator remains valid
- **AND** no participant mutation publication occurs

#### Scenario: Failed transaction restores storage revision
- **GIVEN** enumerators exist before a multi-container transaction begins
- **WHEN** the transaction stages one or more storage changes but later rolls back
- **THEN** every participant's slots, item identities, counts, and mutation revision are restored
- **AND** the pre-existing enumerators remain valid
- **AND** no participant publishes a committed mutation or runs a committed domain effect

#### Scenario: Trade publication invalidates acceptance
- **WHEN** a trade offer publishes a content update
- **THEN** its acceptance revision advances independently of the storage enumeration revision

#### Scenario: Post-commit failures do not change transaction outcome
- **WHEN** staging succeeds and publication is requested after transaction locks are released
- **THEN** the transaction remains committed and runs changed participant publishers at most once in registration order, stopping at the first exception and propagating it directly

### Requirement: Multi-container mutations use an instance transaction
`ItemContainerTransaction` MUST coordinate explicit mutation boundaries as a concrete short-lived object implementing `IItemContainerTransaction`. It MUST own transaction execution, deduplicate participants, acquire locks in deterministic storage order, capture snapshots, own rollback and changed-slot tracking, release locks before publication, and publish only committed changes. `IItemContainerTransaction.Include` MUST support participant enlistment before execution. Its storage-staging methods MUST be used only during the operation callback passed to concrete `ItemContainerTransaction.TryCommit`. The interface MUST NOT expose transaction execution, changed-slot bookkeeping, or post-commit callback registration. Domain participants MUST stage all enlisted storage changes through the active transaction instead of mutating storage independently and reporting changed slots afterward. It MUST NOT introduce ambient state, asynchronous work, service lookup, or distributed transactions. Special-domain boundaries MUST remain private and participate only through a narrow domain-owned operation.

#### Scenario: A transaction participant receives a staging context
- **WHEN** a domain participant receives `IItemContainerTransaction` inside the `TryCommit` operation callback
- **THEN** it can stage item changes, while participant enlistment through `Include` remains available only before execution

### Requirement: MoneyPouch uses one transaction rollback owner
MoneyPouch exact additions and removals MUST stage both pouch and inventory storage through the same active transaction. Coin, slot-zero sentinel, balance, message, and event rules remain owned by MoneyPouch. Its domain effects MUST be explicitly published after commit, and MoneyPouch MUST NOT snapshot and restore pouch storage as a separate rollback mechanism.

#### Scenario: A later participant rejects a staged pouch mutation
- **WHEN** pouch and inventory mutations are staged but a later participant rejects its operation
- **THEN** transaction rollback restores both stores and no pouch message or event is published

### Requirement: Weapon and shield replacement commits conflicting items atomically
After all required `CanUnEquipItem` checks succeed, Weapon and Shield replacement MUST remove the incoming inventory item, move each conflicting equipped item into inventory, and place the same incoming item instance in its equipment slot within one `ItemContainerTransaction`. If any staged storage operation fails, inventory and equipment MUST be restored unchanged and no lifecycle callback or mutation publication may occur. A successful replacement MUST NOT invoke conflicting items' `UnEquipItem` commands. After commit and unlock, it MUST run `OnUnequipped` for a conflicting weapon, then a conflicting shield, then weapon profile/special-attack logic when a weapon is removed, then the incoming item's `OnEquipped`, all before participant publication.

#### Scenario: A second conflicting item cannot fit
- **WHEN** Weapon and Shield are both displaced but inventory can accept only one of them
- **THEN** the incoming item and both equipped items remain in their original slots and no lifecycle callback or mutation publication occurs

#### Scenario: A weapon and shield are displaced successfully
- **WHEN** both conflicting items fit after the incoming item leaves inventory
- **THEN** both original item instances are in inventory, the same incoming instance occupies its equipment slot, and lifecycle callbacks run in the specified order after commit and before publication

#### Scenario: A participant publisher throws after replacement commit
- **WHEN** replacement storage commits and a participant publisher throws
- **THEN** committed storage remains in place, all required lifecycle effects have been attempted, and later participant publishers are skipped
