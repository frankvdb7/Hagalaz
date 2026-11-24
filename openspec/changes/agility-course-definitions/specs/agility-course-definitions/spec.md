# Spec Delta

## Purpose

Defines how Agility obstacle experience and course completion experience are persisted and resolved so content values are not hard-coded in obstacle scripts.

## ADDED Requirements

### Requirement: Obstacle experience is persisted
The system SHALL persist the Agility experience granted by an obstacle, including its required level, keyed by the obstacle's game object id, and expose it to scripts through a service.

#### Scenario: Known obstacle object
- **WHEN** a script requests the definition for a seeded Gnome obstacle object id
- **THEN** the service returns that obstacle's experience (for example 7.5 for the log balance, 25 for the tree)

#### Scenario: Required level
- **WHEN** a script requests the definition for the Gnome tree obstacle
- **THEN** the service returns the required Agility level of 85

#### Scenario: Unknown object
- **WHEN** a script requests a definition for an object id with no row
- **THEN** the service returns no definition and no experience is granted

### Requirement: Course completion experience is persisted
The system SHALL persist the completion experience of each Agility course and expose it through the same service.

#### Scenario: Completion rewards
- **WHEN** a character completes the basic or advanced Gnome course
- **THEN** the persisted completion experience is granted (39 basic, 605 advanced) and the course states are cleared

### Requirement: Existing Gnome values are seeded without overwriting
The system SHALL seed the current Gnome course values during database migration, adding only rows that are absent and never changing existing rows.

#### Scenario: Idempotent seeding
- **WHEN** the seeder runs twice, or after an operator changed a value
- **THEN** no duplicate rows are created and the changed value is preserved
