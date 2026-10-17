# Spec Delta

## ADDED Requirements

### Requirement: Committed item-container changes are consumable by boundary
Each mutation boundary MUST own one change tracker. The tracker MUST expose committed changes only, report whether unconsumed changes exist, atomically consume each retrieved batch, and preserve full-container changes separately from empty batches. A full-container change is represented by `null`; no changes are represented by an empty `HashSet<int>`.

#### Scenario: Standalone changes are accumulated and consumed
- **WHEN** standalone mutations change one or more slots and the caller retrieves the tracker batch
- **THEN** the tracker returns the union of changed slots, and a later retrieval returns an empty set unless another committed mutation occurred

#### Scenario: Full-container change dominates slot changes
- **WHEN** a full-container change is recorded with partial changes before or after it
- **THEN** the next retrieval returns `null` for the full container and subsequent retrieval returns an empty set

#### Scenario: Transaction changes are committed or discarded
- **WHEN** a transaction mutates a boundary and commits or rolls back
- **THEN** commit makes its changes consumable while rollback discards only its provisional changes and preserves earlier unconsumed committed changes

#### Scenario: Automatic publication consumes tracked changes once
- **WHEN** an automatically publishing boundary completes a changed mutation and its publisher succeeds or throws
- **THEN** the publisher receives the consumed batch before callback invocation and a later transaction or retrieval does not replay it

#### Scenario: Explicitly suppressed mutation is not tracked
- **WHEN** a mutation uses `publishChanges: false`
- **THEN** it does not add a change to the consumable tracker or produce a later notification

### Requirement: PriceChecker selection updates use tracked slots
PriceChecker MUST send a full selection on open, then consume tracked selection changes to refresh only affected slots and their price configurations. Projected inventory MUST remain a separate full view.

#### Scenario: Incremental selection refresh updates changed slots
- **WHEN** a selection mutation commits after the interface opens
- **THEN** component 90 and per-slot price configurations contain only the changed slots while the total price is refreshed

#### Scenario: Inventory reconciliation refreshes selection once
- **WHEN** an inventory change reduces one or more selected stacks
- **THEN** reconciliation records those slot changes and sends one incremental selection refresh after the loop

#### Scenario: Inventory changes without selection changes
- **WHEN** inventory changes but reconciliation does not alter the selection
- **THEN** PriceChecker does not send a component 90 update
