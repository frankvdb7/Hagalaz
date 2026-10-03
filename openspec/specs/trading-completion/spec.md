# Trading Completion Specification

## Purpose

Defines safe confirmation, exchange, cancellation, recovery, and cleanup outcomes for in-memory player trades.

## Requirements

### Requirement: A trade has one serialized terminal transition

An active trade MUST serialize final acceptance, cancellation, interruption, and linked target cleanup. Completion and cancellation MUST each be terminal and idempotent. Independent trades MUST remain independent.

#### Scenario: Concurrent final accepts
- **WHEN** both participants release final confirmation concurrently
- **THEN** exactly one completion runs and the session performs one terminal cleanup

#### Scenario: Completion races with cancellation or lifecycle cleanup
- **WHEN** completion races with cancellation, owner or target destruction, or interruption
- **THEN** exactly one valid exchange, refund, or recovery outcome wins and the same escrow is never transferred twice

#### Scenario: Independent trades
- **WHEN** two unrelated sessions complete concurrently
- **THEN** each session completes independently without cross-session interference

### Requirement: Confirmation requires the current offers and session

Both participants MUST accept the current offers before completion. An offer change after acceptance MUST invalidate both confirmations. Each confirmation callback MUST affect only the trade session that created it.

#### Scenario: An offer changes after acceptance
- **WHEN** an offer changes after it was accepted
- **THEN** both confirmations are invalidated and the old confirmation cannot complete the changed offer

#### Scenario: A stale confirmation callback arrives during a later trade
- **WHEN** a callback from a closed trade is invoked after a new trade starts
- **THEN** the new trade's acceptance state and escrow remain unchanged

#### Scenario: Both final accepts arrive concurrently
- **WHEN** both final confirmation callbacks are released together for one active trade
- **THEN** exactly one exchange commits and repeated final accepts have no further economic effect

### Requirement: Completion is complete or untouched

Completion MUST deliver both complete opposite offers and clear both escrow containers only after both checked deliveries succeed. A failed completion MUST leave recipient storage unchanged and retain escrow for cancellation or recovery.

#### Scenario: Items and coins are exchanged
- **WHEN** both offers contain stackable, non-stackable, or coin values
- **THEN** each recipient receives exactly the other offer and escrow is empty

#### Scenario: A checked delivery fails
- **WHEN** a preflight or checked delivery reports failure
- **THEN** no recipient receives partial credit and both offers remain available

### Requirement: Cancellation returns or recovers escrow exactly once

Cancellation MUST return each offer to its owner exactly once and become terminal only after the value is returned or moved to the existing recovery destination. Forced destruction MAY use the existing Rewards or Bank recovery destination when an ordinary refund cannot fit.

#### Scenario: Refund fits
- **WHEN** an active trade is cancelled
- **THEN** each owner receives its own offer once, both offers are cleared, and the session is cancelled

#### Scenario: Forced destruction cannot fit a refund
- **WHEN** a character is destroyed while untouched escrow cannot fit in its inventory or pouch
- **THEN** the existing recovery container receives the escrow and the session reaches terminal cleanup

### Requirement: Committed trade operations reach a terminal state

Once completion storage commits, the trade MUST become completed before publication and clean up exactly once in `finally`, even if post-commit publication fails. Once refund or forced recovery storage commits, the trade MUST become cancelled before publication and clean up exactly once in `finally`, even if post-commit publication fails. Such publication failures MUST NOT make committed value eligible for another completion or refund. The original publication exception MUST propagate directly without being stored in the trade result.

#### Scenario: Completion publication fails after commit
- **WHEN** recipient credit and escrow clearing commit but a participant publication throws
- **THEN** the recipients retain the exchanged value, escrow stays cleared, the trade completes and cleans up once, and the publication exception propagates

#### Scenario: Refund or recovery publication fails after commit
- **WHEN** a refund or forced recovery commits but a participant publication throws
- **THEN** returned or recovered value remains committed, the trade is cancelled and cleaned up once, and the publication exception propagates

### Requirement: Failed completion leaves a safe retry or terminal state

If completion does not commit, recipient storage MUST remain unchanged and escrow MUST remain available until refund or recovery commits. If ordinary refund cannot fit and the session remains active, both acceptances and accepted offer revisions MUST be cleared, and the confirmation UI MUST require fresh acceptance before another completion attempt.

#### Scenario: Failed completion and refund leave a safe retry state
- **WHEN** completion and ordinary refund both fail because recipient capacity is insufficient
- **THEN** the trade remains active with unchanged escrow and recipients, both acceptances are cleared, and a fresh confirmation is required

#### Scenario: Failed completion refunds successfully
- **WHEN** completion fails before commit and ordinary refund succeeds
- **THEN** each owner receives its own offer once and the trade is cancelled and cleaned up
