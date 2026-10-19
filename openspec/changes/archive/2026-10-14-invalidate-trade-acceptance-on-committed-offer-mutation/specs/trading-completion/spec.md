# Spec Delta

## MODIFIED Requirements

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
