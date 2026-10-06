# Spec Delta

## Purpose

Defines how a player trade reaches one safe terminal or retry state when completion, cancellation, publication, or lifecycle callbacks race or fail.

## ADDED Requirements

### Requirement: Committed trade operations reach a terminal state

Once completion storage commits, the trade MUST become completed before publication and clean up exactly once in `finally`, even if post-commit publication fails. Once refund or forced recovery storage commits, the trade MUST become cancelled before publication and clean up exactly once in `finally`, even if post-commit publication fails. Such publication failures MUST NOT make committed value eligible for another completion or refund. The original publication exception MUST propagate directly without being stored in the trade result.

#### Scenario: Completion publication fails after commit
- **WHEN** recipient credit and escrow clearing commit but a participant publication throws
- **THEN** the recipients retain the exchanged value, escrow stays cleared, the trade completes and cleans up once, and the publication exception propagates

#### Scenario: Refund publication fails after commit
- **WHEN** a refund commits but a participant publication throws
- **THEN** the owners retain their returned offers, the trade is cancelled and cleaned up once, and the publication exception propagates

#### Scenario: Forced recovery publication fails after commit
- **WHEN** untouched escrow commits to the existing recovery containers but a participant publication throws
- **THEN** recovery storage retains the escrow, the trade is cancelled and cleaned up once, and the publication exception propagates

### Requirement: Failed completion leaves a safe retry or terminal state

If completion does not commit, recipient storage MUST remain unchanged and escrow MUST remain available until refund or recovery commits. If ordinary refund cannot fit and the session remains active, both acceptances and their accepted offer revisions MUST be cleared, and the confirmation UI MUST require fresh acceptance before another completion attempt.

#### Scenario: Failed completion and refund leave a safe retry state
- **WHEN** completion and ordinary refund both fail because recipient capacity is insufficient
- **THEN** the trade remains active with unchanged escrow and recipients, both acceptances are cleared, and a fresh confirmation is required

#### Scenario: Failed completion refunds successfully
- **WHEN** completion fails before commit and ordinary refund succeeds
- **THEN** each owner receives its own offer once and the trade is cancelled and cleaned up

### Requirement: Confirmation callbacks belong to their trade session

Each accept callback MUST affect only the trade session whose widget registered it. Concurrent final accept callbacks for one session MUST produce at most one exchange, and a callback retained from a closed session MUST NOT mutate a later session.

#### Scenario: Both final accepts arrive concurrently
- **WHEN** both final confirmation callbacks are released together for one active trade
- **THEN** exactly one exchange commits and repeated final accepts have no further economic effect

#### Scenario: Old confirmation callback arrives during a later trade
- **WHEN** a callback from a closed trade is invoked after a new trade starts
- **THEN** the new trade's acceptance state and escrow remain unchanged

### Requirement: Lifecycle callbacks serialize with terminal trade operations

Completion, cancellation, destruction, and interruption MUST use the shared trade-session gate so their race produces one conserved outcome and cannot both exchange and refund the same escrow.

#### Scenario: Completion races with destruction or interruption
- **WHEN** completion races with owner destruction, target destruction, or a relevant interruption
- **THEN** the outcome is either one committed exchange or one committed refund/recovery, with every offered item and coin conserved exactly once
