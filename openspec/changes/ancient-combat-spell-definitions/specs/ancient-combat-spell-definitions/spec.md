# Spec Delta

## Purpose

Defines how Ancient combat spell configuration is persisted and resolved so it is not duplicated in scripts.

## ADDED Requirements

### Requirement: Combat spell definitions are keyed per spellbook
The system SHALL persist combat spell definitions keyed by button id and spellbook, so spells of different books can share a button id.

#### Scenario: Colliding button ids
- **WHEN** a standard-book and an Ancient-book spell use the same button id
- **THEN** both definitions exist and the standard-book loop only wires the standard definition

### Requirement: Ancient spell configuration is persisted
The system SHALL persist the runes, rune counts, required level, experience, base damage and autocast config of every Ice, Blood, Smoke, Shadow and Miasmic spell, and Ancient spells SHALL read those values from the persisted definitions.

#### Scenario: Representative casts
- **WHEN** a character checks requirements, casts, or autocasts an Ancient spell
- **THEN** level, runes, experience, damage input and autocast config equal the persisted row for that spell's button id

#### Scenario: Spell-specific restriction
- **WHEN** a character without Zuriel's staff equipped casts a Miasmic spell
- **THEN** the cast is refused with the existing message

### Requirement: Existing values are seeded without overwriting
The system SHALL seed the current Ancient values, adding only absent rows and never changing existing rows.

#### Scenario: Idempotent seeding
- **WHEN** the seeder runs twice or after an operator changed a value
- **THEN** no duplicate rows exist and the changed value is preserved
