# Item Container Storage

## Purpose

Defines the ownership boundary and correctness guarantees for item storage shared by gameplay containers, including atomic transfers and post-commit domain publication.

## Requirements

### Requirement: Equipment exposes semantic mutation operations
`IEquipmentContainer` MUST expose domain-specific mutation operations rather than generic `Add`, `Replace`, `Remove`, and `Clear` commands. Direct restoration, in-place transformation, removal, and clearing MUST use explicitly named equipment operations. Equipment replacement MUST require the caller's expected current item instance so a stale command cannot replace a different equipped item.

#### Scenario: Stale equipment replacement is rejected
- **WHEN** the expected item instance no longer occupies the requested slot
- **THEN** replacement returns false without changing storage, publishing an equipment update, or running lifecycle callbacks

### Requirement: MoneyPouch separates gameplay and mutation capabilities
`IMoneyPouchContainer` MUST expose normal coin-domain operations separately from transaction staging. Transaction participation MUST be available through `IMoneyPouchMutationBoundary` exposed by `MoneyPouch.Mutations`. The primary MoneyPouch API MUST NOT expose generic item-ID `Contains` operations or staging methods directly.

#### Scenario: MoneyPouch coin availability is domain-specific
- **WHEN** a caller checks whether a character has a positive coin amount
- **THEN** `HasCoins` compares the request against pouch coins and inventory coin item `995` using overflow-safe addition

### Requirement: ItemContainer storage stays encapsulated
Ordinary domain owners that compose `ItemContainer` MUST NOT recover or access its `ItemContainerStorage` instance. Persistence and specialized state maintenance MUST use narrow internal `ItemContainer` operations. Direct `ItemContainerStorage` ownership is reserved for domain containers whose invariants require bypassing the generic facade, currently `EquipmentContainer` and `MoneyPouchContainer`.

#### Scenario: Ordinary container persistence uses the facade
- **WHEN** an ordinary domain owner hydrates, dehydrates, or normalizes item state
- **THEN** it uses narrow `ItemContainer` operations and does not obtain raw storage

### Requirement: Containers compose one authoritative item store
Storage mechanics MUST be composed rather than inherited. `BaseItemContainer`, `TradeItemContainer`, `ITradeItemContainer`, `ItemContainerExtensions`, generic forwarding wrappers, `IItemContainerStorageOwner`, and `ItemContainerTransfer` MUST be removed. One concrete `ItemContainer` MUST implement `IItemContainer` and privately compose one `ItemContainerStorage` and one `ItemContainerMutationBoundary`, exposed as `IItemContainerMutationBoundary Mutations`. Ordinary domain implementations privately own concrete `ItemContainer`; ordinary domain interfaces MUST expose `IItemContainer Items` and MUST NOT copy generic forwarding operations. Equipment and MoneyPouch MUST compose storage directly with private mutation boundaries and MUST NOT expose generic `Items` or their boundaries. Equipment MUST itself remain a read-only `IContainer<IItem?>`. TradeOffer, Duel, and Price Checker MUST compose the concrete `ItemContainer` where generic behavior is required. `IItemContainer` MUST NOT reference concrete `ItemContainer`; bulk movement MUST NOT be part of its contract. Exact and multi-container mutations MUST use `ItemContainerTransaction` through `IItemContainerMutationBoundary` participants. No runtime cast may be required to recover storage infrastructure. `ItemContainerStorage` MUST own low-level slot and mutation mechanics and MUST NOT depend on character, trade, equipment, shop, UI, or persistence behavior. `ItemContainerTransaction` MUST own deterministic lock ordering, snapshots, rollback, changed-slot tracking, and post-commit publication. Domain containers MUST retain ownership of specialized gameplay callbacks and orchestration. Exact removal MUST be available through a neutral generic operation. MoneyPouch MUST stage pouch and inventory mutations through the same transaction using the dedicated `IMoneyPouchMutationBoundary`; it MUST NOT expose the concrete storage boundary or keep a separate rollback path. Concrete storage and mutation-boundary implementations SHOULD remain assembly-internal implementation details where friend-assembly access is sufficient.

#### Scenario: Domain mutation publishes committed slots
- **WHEN** a domain container successfully adds, removes, replaces, moves, swaps, sorts, clears, or restores items
- **THEN** the authoritative storage reflects the mutation before the container publishes its changed slots

#### Scenario: Rejected mutation leaves storage unchanged
- **WHEN** a single-container mutation or exact cross-container transfer cannot satisfy its quantity, capacity, stacking, or overflow rules
- **THEN** every affected storage retains its pre-operation slot contents and counts

### Requirement: Exact restoration validates before replacing state
`ItemContainerStorage` MUST own exact slot restoration validation and replacement. It MUST reject out-of-range slots and invalid counts with `ArgumentOutOfRangeException`, reject duplicate slots with `ArgumentException`, and leave the previous state unchanged if any input entry is invalid. Ordinary containers MUST reject zero counts; MoneyPouch MAY allow zero while validating its coin ID and physical slot at the domain boundary.

#### Scenario: Invalid restored data leaves the old contents intact
- **WHEN** restoration contains an invalid slot, duplicate slot, null item, or disallowed count
- **THEN** storage throws the established exception category and retains all previous slots, counts, and revision

### Requirement: Cross-container transfers commit both stores atomically
An exact cross-container transfer MUST enter through `IItemContainerMutationBoundary` and use `ItemContainerTransaction` to validate and plan source removal and destination insertion before changing either store, acquire distinct locks in stable order, call the single low-level `ItemContainerStorage` transfer algorithm, commit both stores together, advance each storage revision once, and publish changed slots only after success.

#### Scenario: Exact transfer succeeds
- **WHEN** the source has the requested quantity and the destination can accept the exact result
- **THEN** both stores commit the transfer before either domain container publishes an update

#### Scenario: Transfer fails validation
- **WHEN** the source quantity is insufficient or the destination cannot accept the result
- **THEN** neither store changes and neither container publishes a committed mutation

#### Scenario: Opposite transfers acquire locks consistently
- **WHEN** two operations transfer in opposite directions between the same stores
- **THEN** both operations acquire store locks in the same stable order and complete without lock-order deadlock

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
- **AND** no participant publishes a committed mutation or runs a committed callback

#### Scenario: Trade publication invalidates acceptance
- **WHEN** a trade offer publishes a content update
- **THEN** its acceptance revision advances independently of the storage enumeration revision

#### Scenario: Post-commit failures do not change transaction outcome
- **WHEN** the operation callback succeeds and transaction locks are released
- **THEN** the transaction remains committed, attempts all pre-publication callbacks, changed participant publishers in registration order, and committed callbacks exactly once, then rethrows one captured exception or aggregates multiple exceptions

### Requirement: Multi-container mutations use an instance transaction
`ItemContainerTransaction` MUST coordinate explicit mutation boundaries as a concrete short-lived object implementing `IItemContainerTransaction`. It MUST own transaction execution, deduplicate participants, acquire locks in deterministic storage order, capture snapshots, own rollback and changed-slot tracking, release locks before publication, and publish only committed changes. `IItemContainerTransaction.Include` MUST support participant enlistment before execution. Its storage-staging methods and `OnCommitted` MUST be used only during the operation callback passed to concrete `ItemContainerTransaction.TryExecute`. The interface MUST NOT expose transaction execution or changed-slot bookkeeping. Domain participants MUST stage all enlisted storage changes through the active transaction instead of mutating storage independently and reporting changed slots afterward. It MUST NOT introduce ambient state, asynchronous work, service lookup, or distributed transactions. Special-domain boundaries MUST remain private and participate only through a narrow domain-owned operation.

#### Scenario: A transaction participant receives a staging context
- **WHEN** a domain participant receives `IItemContainerTransaction` inside the `TryExecute` operation callback
- **THEN** it can stage item changes and register post-commit effects, while participant enlistment through `Include` remains available only before execution

### Requirement: MoneyPouch uses one transaction rollback owner
MoneyPouch exact additions and removals MUST stage both pouch and inventory storage through the same active transaction. Coin, slot-zero sentinel, balance, message, and event rules remain owned by MoneyPouch. Its committed domain effects MUST use `OnCommitted`, and MoneyPouch MUST NOT snapshot and restore pouch storage as a separate rollback mechanism.

#### Scenario: A later participant rejects a staged pouch mutation
- **WHEN** pouch and inventory mutations are staged but a later participant rejects its operation
- **THEN** transaction rollback restores both stores and no pouch message or event is published

### Requirement: Familiar inventory owns partial withdrawal policy
Generic mutation infrastructure MUST NOT expose partial bulk movement through `TransferAll` or `AddAndRemoveFrom`. `IFamiliarInventoryContainer` MUST expose an operation that attempts every familiar item in one transaction, commits fitting transfers together, and leaves items that do not fit in familiar storage.

#### Scenario: Familiar withdrawal has fitting and non-fitting items
- **WHEN** only some familiar items fit in the owner's inventory
- **THEN** fitting items move, non-fitting items remain, and each changed container publishes once

#### Scenario: No familiar item fits
- **WHEN** the owner's inventory cannot accept any familiar item
- **THEN** both containers remain unchanged and publish no mutation

#### Scenario: Failed settlement restores every participant
- **WHEN** settlement fails after one or more staged mutations
- **THEN** every participant returns to its pre-transaction state and no partial settlement is published

#### Scenario: Successful settlement publishes after unlocking
- **WHEN** every staged mutation succeeds
- **THEN** all storage commits, locks are released, and changed domains receive post-commit publication

### Requirement: Domain-specific empty-count semantics remain explicit
Storage MUST support removing depleted slots or retaining the item at a configured reset count. Money pouch and shop containers MUST retain their own domain behavior when using a zero reset count.

#### Scenario: Money pouch retains its coin sentinel
- **WHEN** pouch coins reach zero or an empty persisted pouch is restored
- **THEN** slot zero contains coin item 995 with count zero

#### Scenario: Shop stock normalizes depleted entries
- **WHEN** shop normalization processes depleted non-original stock
- **THEN** storage retains the configured zero-count entry and the shop applies its existing normalization and sorting behavior

### Requirement: Domain callbacks follow committed storage state
Equipment callbacks, trade settlement publication, inventory/bank/reward events, money-pouch messages, and UI updates MUST remain owned by their domain operations and MUST observe committed storage state.

#### Scenario: Equipment transfer runs callbacks before publication
- **WHEN** an equip or unequip transfer commits successfully
- **THEN** the required equipment callbacks run after both stores commit and before equipment and inventory updates publish

#### Scenario: Trade consumes generic container operations
- **WHEN** TradeExchange stages item mutations for settlement
- **THEN** it uses generic storage operations under its deterministic storage locks and publishes through domain owners only after commit

### Requirement: MoneyPouch and economic flows use one transaction owner
MoneyPouch additions, removals, inventory transfers, bank deposits, shop purchases and sales, and duel stake, return, and cancellation refunds MUST stage all affected storage through one `ItemContainerTransaction` or exact two-container mutation boundary. A failed operation MUST leave every participant unchanged. Existing amount clamping, prices, messages, callbacks, and stock normalization MUST remain owned by their domain operations. MoneyPouch MUST retain its slot-zero coin sentinel and partial `Remove`, `AddFromInventory`, and `MoveToInventory` behavior.

#### Scenario: Pouch overflow cannot partially commit
- **WHEN** a pouch addition overflows but inventory cannot accept the overflow
- **THEN** pouch and inventory remain unchanged and no committed pouch message or event is emitted

#### Scenario: Bank deposit from pouch is rejected
- **WHEN** bank storage cannot accept a staged deposit
- **THEN** bank and pouch remain unchanged

#### Scenario: Shop payment or delivery fails
- **WHEN** a shop purchase or sale cannot stage every payment, item, or payout change
- **THEN** inventory, pouch, and shop storage remain unchanged and no purchase event is emitted

#### Scenario: Duel cancellation refund fails
- **WHEN** either player's stake cannot be returned
- **THEN** both stake containers and both players' destination stores remain unchanged and the duel session remains active

### Requirement: Duel stake mutations use a composed domain collaborator
`DuelArenaScript` MUST compose one concrete `DuelStakeExchange` for item and pouch staking, return, and cancellation refund. The collaborator MUST use exact mutation-boundary transfers for simple items and one `ItemContainerTransaction` for pouch or two-player refund operations. It MUST NOT own UI or session state or require an interface or service registration.

#### Scenario: Duel stake transfer fails
- **WHEN** the stake or destination cannot accept the exact transfer
- **THEN** source and destination remain unchanged and the script does not publish a stake change

### Requirement: Equipment replacement preserves command behavior outside weapon and shield conflicts
Before its first mutation, `EquipmentContainer` MUST identify all conflicting equipped items and require `CanUnEquipItem` to succeed for each. Rejection MUST leave equipment and inventory unchanged and invoke no equip or unequip callback. For occupied replacement slots other than Weapon and Shield, a successful preflight MUST preserve the existing `UnEquipItem` command behavior, including custom and interactive behavior.

#### Scenario: A later conflict rejects replacement
- **WHEN** an earlier conflicting item allows unequipping but a later conflict rejects it
- **THEN** equipment and inventory remain unchanged and no mutation publication or callback occurs

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
- **THEN** committed storage remains in place, all registered lifecycle callbacks have run, and remaining participant publishers are still attempted
