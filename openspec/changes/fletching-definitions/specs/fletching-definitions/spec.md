# Spec Delta

## Purpose

Defines how Fletching recipes are persisted and resolved so scripts do not hard-code them.

## ADDED Requirements

### Requirement: Fletching recipes are persisted
The system SHALL persist every Fletching recipe (resource, tool, category, products, counts, experience, required levels, optional animation) and expose it to scripts through a synchronous service.

#### Scenario: Known recipe in either orientation
- **WHEN** a script looks up a seeded (resource, tool) pair of a category in either order
- **THEN** the recipe of that category is returned

#### Scenario: Wrong category or unknown pair
- **WHEN** a pair only exists in another category, or does not exist
- **THEN** no recipe is returned

#### Scenario: No animation
- **WHEN** a recipe has no animation
- **THEN** its animation id is -1 and no animation is queued

### Requirement: Existing recipes are seeded
The system SHALL seed all current recipes verbatim, inserting absent rows, never changing a row already seeded, and claiming legacy rows with no category.

#### Scenario: Idempotent seeding
- **WHEN** the seeder runs twice, or after an operator changed a value
- **THEN** no duplicate rows exist and the changed value is preserved
