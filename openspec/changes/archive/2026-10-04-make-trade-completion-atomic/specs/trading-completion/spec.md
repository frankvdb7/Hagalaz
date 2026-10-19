## ADDED Requirements

### Requirement: A trade has one serialized terminal transition

An active trade MUST serialize final acceptance, cancellation, interruption, and linked target cleanup through its owner session gate. Completion and cancellation MUST each be terminal and idempotent.

#### Scenario: Concurrent final accepts

- **WHEN** both participants release final confirmation concurrently
- **THEN** exactly one completion runs and the session performs one terminal cleanup

#### Scenario: Accept and cancel race

- **WHEN** completion and cancellation race for one active session
- **THEN** exactly one valid outcome wins and no value is transferred twice

#### Scenario: Independent trades

- **WHEN** two unrelated sessions complete concurrently
- **THEN** each session remains independently usable

### Requirement: Completion validates under one mutation boundary

Before changing recipients or escrow, completion MUST lock all involved `TradeItemContainer` participants in deterministic order, snapshot both offers, verify both accepted revisions, and verify complete recipient capacity including money-pouch overflow. Normal mutators on those participating containers MUST honor the same boundary; unrelated `BaseItemContainer` descendants are outside this synchronization scope.

#### Scenario: Capacity is insufficient

- **WHEN** either recipient cannot receive its complete opposite offer
- **THEN** completion returns failure without clearing either offer

#### Scenario: An offer changed after acceptance

- **WHEN** an offer revision differs from the accepted revision
- **THEN** both confirmations are invalidated and the old confirmation cannot complete the changed offer

### Requirement: Completion is complete or untouched

Completion MUST deliver both complete opposite offers and clear both escrow containers only after both checked deliveries succeed. It MUST NOT use recovery containers or settle a partial exchange.

#### Scenario: Items and coins are exchanged

- **WHEN** both offers contain stackable, non-stackable, or coin values
- **THEN** each recipient receives exactly the other offer and escrow is empty

#### Scenario: A checked delivery fails

- **WHEN** a preflight or checked delivery reports failure
- **THEN** completion returns failure, leaves escrow available for cancellation, and retains no compensation state

### Requirement: Cancellation returns escrow exactly once

Cancellation MUST use checked refund operations under the same container boundary and reach `Cancelled` only after both offers are returned. Forced destruction MAY move untouched escrow to the existing persisted Rewards/Bank recovery destination when normal refund cannot fit.

#### Scenario: Refund fits

- **WHEN** an active trade is cancelled
- **THEN** each owner receives its own offer once, both offers are cleared, and the session is cancelled

#### Scenario: Forced destruction cannot fit

- **WHEN** an owner is destroyed while untouched escrow cannot fit in its inventory or pouch
- **THEN** the existing recovery container receives the escrow and the session reaches terminal cleanup without relying on a later tick

### Requirement: Completed trades cannot be refunded

Once completion succeeds, later cancellation, disconnect, or logout callbacks MUST observe terminal state and MUST NOT return exchanged escrow to the original owners.

#### Scenario: Target disconnects during completion

- **WHEN** target cleanup races with completion
- **THEN** the shared session gate produces one completed exchange or one consistent cancellation, never both
