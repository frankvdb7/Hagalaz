# Item container storage delta

## MODIFIED Requirements

### Requirement: Storage and trade revisions have distinct purposes
Storage MUST own its mutation revision, which invalidates active enumerators after committed storage changes. `ItemContainerTransaction` MUST own deterministic lock ordering; `ItemContainerMutationBoundary.TryTransferTo` MUST use boundaries enlisted in one caller-owned transaction. `TradingCharacterScript` MUST own terminal completion, refund, and forced recovery transaction scopes, including every required participant, and MUST set the terminal `TradeState` before `Commit()`. TradeExchange MUST stage those mutations inside the caller-owned transaction and MUST NOT begin or commit it. Terminal movement MUST use exact transaction-required transfers: `IItemContainer.TryTransferTo(...)` for non-coins and recovery items, and `IMoneyPouchContainer.TryTransferCoinsFrom(...)` for coins. It MUST NOT use destination `AddRange` or offer `Clear` for authoritative terminal escrow movement. `Commit()` MUST make storage irreversible, release mutation locks before committed completion/publication while retaining scope bindings through that work, then clear bindings under ordered storage locks and wake overlapping waiters. A trade offer's acceptance `Revision` MUST remain domain-owned and MUST advance according to its existing publication semantics, independently of storage revision.

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
- **WHEN** a mutation succeeds and `Commit()` reaches publication after releasing transaction locks
- **THEN** the transaction remains committed and runs changed participant publishers at most once in registration order, stopping at the first exception and propagating it directly

### Requirement: MoneyPouch uses one transaction rollback owner
MoneyPouch exact additions and removals MUST stage both pouch and inventory storage through the same active transaction. Coin, slot-zero sentinel, balance, message, and event rules remain owned by MoneyPouch. MoneyPouch effects MUST be published automatically by `ItemContainerTransaction` from immutable MoneyPouch-owned completion facts during committed post-lock completion. Callers MUST NOT manually publish MoneyPouch receipts or effects, and MoneyPouch MUST NOT snapshot and restore pouch storage as a separate rollback mechanism.

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
