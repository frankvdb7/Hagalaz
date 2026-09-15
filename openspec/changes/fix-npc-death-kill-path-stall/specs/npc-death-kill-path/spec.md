## ADDED Requirements

### Requirement: Death kill notifications do not block the game loop

Post-death kill notification code MUST return control to the creature task scheduler before awaiting asynchronous Slayer service work.

#### Scenario: Slayer definition lookup is pending

- **WHEN** a character with an active Slayer task receives a creature-kill notification and the task-definition lookup has not completed
- **THEN** the event callback returns without waiting for that lookup
- **AND** the lookup continuation remains scheduled on the character task scheduler

#### Scenario: Slayer task completion is pending

- **WHEN** the final Slayer kill starts completion reward processing
- **THEN** task-definition and master-table lookups are awaited asynchronously
- **AND** the game-loop thread is not synchronously blocked by either lookup
