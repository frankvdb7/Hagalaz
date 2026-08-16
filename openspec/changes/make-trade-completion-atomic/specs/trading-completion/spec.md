## ADDED Requirements

### Requirement: Trade completion has one serialized terminal transition

An active trade MUST have one serialized owner for acceptance, completion, cancellation, and linked target cleanup. Completion MUST transition an active trade to a terminal outcome at most once.

#### Scenario: Two final accepts arrive concurrently

- **WHEN** both accepted players release their final confirmation through a controlled barrier
- **THEN** exactly one completion operation transfers the exchange and exactly one terminal cleanup occurs

#### Scenario: A final accept is repeated after completion begins

- **WHEN** a repeated final-accept packet arrives while completion is already completing or terminal
- **THEN** it performs no additional transfer, refund, or cleanup

#### Scenario: Accept and cancel race

- **WHEN** a final accept and cancellation are released concurrently for the same active trade
- **THEN** the trade has exactly one valid terminal outcome and no item or coin is transferred and refunded twice

#### Scenario: Two unrelated trades are active

- **WHEN** completion operations run concurrently for two different trade sessions
- **THEN** each trade progresses independently without sharing terminal state or blocking the other trade's correctness

### Requirement: A confirmed exchange is validated before mutation

The trade owner MUST confirm that the session is active, both participants are accepted, and the offers have not changed since confirmation before mutating either recipient. It MUST verify that both inventories and money pouches can receive their complete opposite offers.

#### Scenario: Destination capacity changes before completion

- **WHEN** either recipient loses the capacity required for the accepted offer before completion
- **THEN** completion fails without clearing escrow or partially changing either recipient, and the trade follows the consistent cancellation/refund outcome when refund is possible

#### Scenario: An offer changes after acceptance

- **WHEN** an accepted participant modifies an offer before completion
- **THEN** both confirmations are invalidated through the existing trade-change flow and the old confirmation cannot complete the changed offer

#### Scenario: An offered container no longer matches confirmation

- **WHEN** a trade-container revision differs from the revision recorded at final confirmation
- **THEN** completion is rejected without consuming the offer containers

### Requirement: The exchange and refund preserve all value

The exchange MUST either apply the complete opposite offers to both recipients or leave both recipients and both existing trade containers consistent. Every inventory, trade-container, and money-pouch mutation result MUST be checked.

#### Scenario: Stackable and non-stackable items are exchanged

- **WHEN** both participants offer stackable quantities and non-stackable item quantities
- **THEN** the recipients receive exactly those quantities and the total quantity across participants and escrow is conserved

#### Scenario: Money-pouch coins are exchanged

- **WHEN** either participant offers coins from the money pouch
- **THEN** the recipient receives the exact coin count, including any supported money-pouch overflow behavior, and total coins are conserved

#### Scenario: A recipient mutation fails

- **WHEN** adding the complete offer to either recipient reports failure or throws
- **THEN** only the exact applied trade item and coin deltas are compensated, unrelated recipient contents are not replaced, escrow remains consistent, and no successful partial exchange is later refunded as if it were still offered; if compensation cannot complete, the session enters compensation-pending and refund is blocked until compensation succeeds

#### Scenario: Destruction finds compensation still pending

- **WHEN** destruction cannot safely roll back a recorded recipient mutation
- **THEN** an exchange completes the remaining opposite-side value into the intended recipient's inventory or existing Rewards/Bank recovery container, while a refund returns remaining value to its original owner; no partial exchange/refund outcome is exposed and terminal cleanup does not depend on another character tick

#### Scenario: A cancelled trade returns escrow

- **WHEN** an active trade is cancelled, interrupted, or disconnected before successful completion
- **THEN** all escrowed items and coins are returned exactly once and the trade reaches the cancelled terminal state only after checked refunds succeed; if capacity temporarily prevents an ordinary refund, cancellation remains pending for a linked lifecycle retry, while destruction synchronously stores the escrow in the participant's persisted rewards container or bank before cleanup

### Requirement: Disconnect and cleanup cannot refund a completed trade

Completion, cancellation, disconnect, and logout MUST converge on one terminal cleanup implementation. Once completion succeeds, no later lifecycle callback may return the exchanged escrow to its original owner.

#### Scenario: The target disconnects during completion

- **WHEN** target destruction/logout races with the owner's exchange operation
- **THEN** the target lifecycle uses the same session gate and the result is either one completed exchange or one consistent cancellation, never both

#### Scenario: Completion cleanup runs

- **WHEN** both recipients have received their complete offers
- **THEN** escrow is cleared, interfaces and input handlers are reset, the session link is removed, and no refund branch is reachable
