# Trading Completion Specification

## Purpose

Defines safe confirmation, exchange, cancellation, and cleanup outcomes for in-memory player trades.

## Requirements

### Requirement: A trade has one serialized terminal transition

An active trade MUST serialize final acceptance, cancellation, interruption, and linked target cleanup. Completion and cancellation MUST each be terminal and idempotent. Independent trades MUST remain independent.

#### Scenario: Concurrent final accepts
- **WHEN** both participants release final confirmation concurrently
- **THEN** exactly one completion runs and the session performs one terminal cleanup

#### Scenario: Completion races with cancellation or lifecycle cleanup
- **WHEN** completion races with cancellation, owner or target destruction, or interruption
- **THEN** exactly one valid exchange or refund wins and the same offer is never transferred twice

#### Scenario: Independent trades
- **WHEN** two unrelated sessions complete concurrently
- **THEN** each session completes independently without cross-session interference

### Requirement: Confirmation requires the current offers and session

Both participants MUST accept the current offers before completion. Any committed offer change MUST invalidate both confirmations independently of whether its notifications are successfully published. Each confirmation callback MUST affect only the trade session that created it.

#### Scenario: An offer changes after acceptance
- **WHEN** an offer changes after it was accepted
- **THEN** both confirmations are invalidated and the old confirmation cannot complete the changed offer

#### Scenario: An offer change commits before publication fails
- **WHEN** an offer change commits and Inventory publication throws before the changed offer is published
- **THEN** the changed offer remains committed, both previous confirmations are invalid, and the old acceptance cannot complete the trade

#### Scenario: A stale confirmation callback arrives during a later trade
- **WHEN** a callback from a closed trade is invoked after a new trade starts
- **THEN** the new trade's acceptance state and escrow remain unchanged

#### Scenario: Both final accepts arrive concurrently
- **WHEN** both final confirmation callbacks are released together for one active trade
- **THEN** exactly one exchange commits and repeated final accepts have no further economic effect

### Requirement: Completion is complete or untouched

Completion MUST deliver both complete opposite offers and clear both escrow containers only after both checked deliveries succeed. A failed completion MUST leave recipient storage unchanged and retain the original offers for cancellation.

#### Scenario: Items and coins are exchanged
- **WHEN** both offers contain stackable, non-stackable, or coin values
- **THEN** each recipient receives exactly the other offer and escrow is empty

#### Scenario: A checked delivery fails
- **WHEN** a recipient cannot receive an offered item or coin value
- **THEN** the transaction rolls back recipient changes and both original offers remain available for cancellation

### Requirement: Cancellation returns each offer to its owner

Cancellation MUST return each offer to its original owner exactly once in one transaction and become terminal only after both refunds commit. Inventory capacity freed by funding a non-coin offer MUST remain available for cancellation while the trade is active. GameFrame money-pouch withdrawals and Summoning Orb/Summoning Tab Take BoB actions MUST NOT consume that capacity during an active trade, regardless of which interface is open. A familiar inventory screen's own overlay MUST NOT block normal withdrawals when no gameplay action is busy. Equipment-tab aura unequip actions MUST interrupt and refund the trade before moving the aura into Inventory.

#### Scenario: An active trade is cancelled
- **WHEN** either participant cancels an active trade
- **THEN** each owner receives their own offer, both offers are cleared, and the session is cancelled

#### Scenario: A failed completion is cancelled
- **WHEN** recipient capacity rejects completion before commit
- **THEN** completion rolls back, the original offers are refunded to their owners, and the session is cancelled

#### Scenario: A pouch withdrawal is submitted during an active trade
- **WHEN** a character submits a GameFrame money-pouch withdrawal amount while a trade is active
- **THEN** the busy check rejects the withdrawal before Inventory or MoneyPouch changes, leaving capacity available to refund the offer

#### Scenario: A familiar withdrawal is requested during an active trade
- **WHEN** a character uses Take BoB from the Summoning Orb or Summoning Tab while a trade is active
- **THEN** the active trade keeps both participants gameplay-busy, the withdrawal is rejected before the familiar or Inventory changes, and capacity remains available to refund the offer

#### Scenario: A familiar withdrawal is requested from its open inventory screen
- **WHEN** a character withdraws an item or uses Take BoB from the familiar inventory screen outside a gameplay-busy state
- **THEN** the familiar's inventory overlay does not block the withdrawal and fitting items move to Inventory

#### Scenario: An aura is unequipped during an active trade
- **WHEN** a character chooses an aura unequip option from the Equipment tab while a funded offer is active
- **THEN** the normal interruption cancels and refunds the offer before the aura is moved into Inventory

#### Scenario: A funded offer is refunded before the detach snapshot
- **WHEN** an Inventory-funded offer is active, a GameFrame pouch withdrawal is attempted, and the character detaches
- **THEN** the busy check leaves Inventory capacity available, detach refunds the offer before dehydration, and the snapshot contains the refunded item

### Requirement: Committed trade operations reach a terminal state
Trade completion and refund MUST perform checked domain mutations under their existing session gate and one disposable transaction. The session MUST own state changes through intent-specific transitions: completion enters `Completing` from `Active`, successful completion enters `Completed`, failed completion returns to `Active`, and cancellation enters `Cancelled` from `Active` only after refund mutations succeed. Immediately before Commit, the owner MUST transition to `Completed` or `Cancelled` without an intervening external action. Commit MUST make storage irreversible and release physical locks while retaining transaction bindings. The trade owner MUST explicitly publish each affected domain in its established order before disposing the committed scope. Existing finally cleanup MUST run exactly once even when publication throws. A failure from a transaction-owned completion or publication callback MUST propagate as `ItemContainerPostCommitException`, preserving its phase and exact original callback exception as `InnerException`; such failures MUST NOT make committed value eligible for another completion or refund. Publications explicitly invoked by the trade owner after Commit retain their existing direct exception behavior. Unsuccessful attempts MUST dispose before another operation starts.

#### Scenario: Completion publication fails after commit
- **WHEN** recipient credit and escrow clearing commit but an explicit domain publisher throws
- **THEN** the recipients retain the exchanged value, escrow stays cleared, the trade completes and cleans up once, remaining required publishers follow the established attempt policy, and the publication exception propagates

#### Scenario: Refund publication fails after commit
- **WHEN** an owner refund commits but an explicit domain publisher throws
- **THEN** returned value remains committed, the trade is cancelled and cleaned up once, remaining required publishers follow the established attempt policy, and the publication exception propagates

#### Scenario: Transaction-owned publication fails after commit
- **WHEN** a transaction-owned publisher throws after the trade's storage changes commit
- **THEN** the trade remains terminal and is cleaned up exactly once, and `ItemContainerPostCommitException` exposes phase `Publication` and the exact publisher exception as `InnerException`

#### Scenario: Trade-owned publication fails after Commit returns
- **WHEN** an explicit trade-owned publication throws after transaction Commit has returned
- **THEN** the trade remains terminal and is cleaned up once, and the publisher's exception follows its existing direct propagation behavior

### Requirement: Failed completion leaves a safe retry or terminal state

If completion does not commit, recipient storage MUST remain unchanged and both original offers MUST remain available for cancellation. Cancellation uses the same refund path; it relies on active-trade Inventory-credit guards preserving capacity released by funded offers, not on source removal alone or a separate capacity-based recovery destination.

#### Scenario: Insufficient recipient capacity rejects completion
- **WHEN** a recipient cannot accept the complete offer
- **THEN** recipient state is unchanged, both offers remain intact, and cancellation refunds each offer to its original owner
