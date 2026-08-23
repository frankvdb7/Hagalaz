## Purpose

This capability prevents queued ground-item interactions from granting an item after the clicked visible instance has already been removed.

## ADDED Requirements

### Requirement: Pickup requires removal of the exact active instance

The system SHALL report ground-item removal as successful only when the exact active ground-item instance supplied by the pickup is present at its location and is removed. A failed or stale removal SHALL produce no removal side effects.

#### Scenario: Normal pickup removes and grants one item

- **WHEN** a player with inventory capacity picks up an active public ground item
- **THEN** the exact visible instance is removed and the player receives one copy of its item

#### Scenario: A second queued pickup is rejected

- **WHEN** a second queued pickup uses the same ground-item instance after the first pickup removed it
- **THEN** removal reports failure and the second player receives no item

#### Scenario: A stale instance cannot remove a replacement

- **WHEN** a stale ground-item reference targets a location where a different ground-item instance is active
- **THEN** removal reports failure, leaves the replacement active, and performs no destroy, respawn, or client-removal side effect for the stale reference

#### Scenario: Inventory-full pickup does not remove the item

- **WHEN** a player without inventory capacity attempts a pickup
- **THEN** the item remains active and no removal is attempted

#### Scenario: Successful respawning item removal preserves respawn behavior

- **WHEN** a visible respawning ground item is successfully picked up
- **THEN** that visible instance is consumed once and the existing respawn behavior creates the replacement instance
