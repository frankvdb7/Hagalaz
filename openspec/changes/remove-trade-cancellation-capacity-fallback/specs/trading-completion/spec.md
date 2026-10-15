# Spec Delta

## MODIFIED Requirements

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

### Requirement: Completion is complete or untouched

Completion MUST deliver both complete opposite offers and clear both escrow containers only after both checked deliveries succeed. A failed completion MUST leave recipient storage unchanged and retain the original offers for cancellation.

#### Scenario: Items and coins are exchanged
- **WHEN** both offers contain stackable, non-stackable, or coin values and each recipient can receive them
- **THEN** each recipient receives exactly the other offer and both offers are empty

#### Scenario: A checked delivery fails
- **WHEN** a recipient cannot receive an offered item or coin value
- **THEN** the transaction rolls back recipient changes and both original offers remain available for cancellation

### Requirement: Cancellation returns each offer to its owner

Cancellation MUST return each offer to its original owner exactly once in one transaction. A cancellation MUST become terminal only after both refunds commit. The trade offer's source removals provide the capacity needed to restore those same offered items and coins.

#### Scenario: An active trade is cancelled
- **WHEN** either participant cancels an active trade
- **THEN** each owner receives their own offer, both offers are cleared, and the session is cancelled

#### Scenario: A failed completion is cancelled
- **WHEN** recipient capacity rejects completion before commit
- **THEN** completion rolls back, the original offers are refunded to their owners, and the session is cancelled

### Requirement: Committed trade operations reach a terminal state

Trade completion and refund MUST perform checked domain mutations under their existing session gate and one disposable transaction. The session MUST own state changes through intent-specific transitions: completion enters `Completing` from `Active`, successful completion enters `Completed`, failed completion returns to `Active`, and cancellation enters `Cancelled` from `Active` only after refund mutations succeed. Immediately before Commit, the owner MUST transition to `Completed` or `Cancelled` without an intervening external action. Commit MUST make storage irreversible and release physical locks while retaining transaction bindings. The trade owner MUST explicitly publish each affected domain in its established order before disposing the committed scope. Existing finally cleanup MUST run exactly once even when publication throws. Such failures MUST NOT make committed value eligible for another completion or refund, and the original publication exception MUST propagate without a transaction result or public committed-state query. Unsuccessful attempts MUST dispose before another operation starts.

#### Scenario: Completion publication fails after commit
- **WHEN** recipient credit and offer clearing commit but an explicit domain publisher throws
- **THEN** recipients retain the exchanged value, offers stay cleared, the trade completes and cleans up once, remaining required publishers follow the established attempt policy, and the publication exception propagates

#### Scenario: Refund publication fails after commit
- **WHEN** an owner refund commits but an explicit domain publisher throws
- **THEN** returned value remains committed, the trade is cancelled and cleaned up once, remaining required publishers follow the established attempt policy, and the publication exception propagates

### Requirement: Failed completion leaves a safe retry or terminal state

If completion does not commit, recipient storage MUST remain unchanged and both original offers MUST remain available for cancellation. Since offers are removed from their owners' Inventory or MoneyPouch, cancellation MUST be able to refund them without a separate capacity-based recovery destination.

#### Scenario: Insufficient recipient capacity rejects completion
- **WHEN** a recipient cannot accept the complete offer
- **THEN** recipient state is unchanged, both offers remain intact, and cancellation refunds each offer to its original owner

## REMOVED Requirements

### Requirement: Cancellation returns or recovers escrow exactly once

**Reason**: Production offers are funded by removing items from the owner's Inventory or MoneyPouch, which frees the capacity needed to return them. A capacity-based Rewards/Bank fallback treats an unsupported state as an expected cancellation path.
**Migration**: Use the existing transactional refund to return each offer to its owner. Forced destruction uses that same refund path and does not select a separate recovery destination.
