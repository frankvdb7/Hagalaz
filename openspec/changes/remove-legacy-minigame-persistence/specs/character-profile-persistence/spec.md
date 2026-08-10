## ADDED Requirements

### Requirement: Active minigame character state uses the profile store

The system SHALL continue to use the existing `Character.Profile` JSON store as
the authoritative persistence surface for Barrows, Duel Arena, Godwars, and
TzHaar character state.

#### Scenario: Profile-backed minigame state remains available

- **WHEN** any of the four minigame scripts reads or updates character state
- **THEN** it uses the existing profile keys and DTOs without consulting a
  removed relational minigame entity

### Requirement: Legacy character minigame entities are absent from the live model

The live EF model MUST NOT expose `MinigamesBarrow`, `MinigamesDuelArena`,
`MinigamesGodwar`, or `MinigamesTzhaarCave` as entity types, character
navigations, `DbSet`s, or table mappings.

#### Scenario: Current model excludes obsolete minigame entities

- **WHEN** the application builds the current EF model
- **THEN** none of the four obsolete entity types or their tables are included

### Requirement: Legacy tables are removed while static wave definitions remain

The database migration SHALL drop `minigames_barrows`,
`minigames_duel_arena`, `minigames_godwars`, and `minigames_tzhaar_cave` without
backfilling their rows, and SHALL preserve `minigames_tzhaar_cave_waves`.

#### Scenario: Cleanup migration removes only obsolete tables

- **WHEN** the latest migration is applied to a database containing the
  historical schema
- **THEN** the four legacy tables are absent, the wave-definition table remains,
  and no profile data conversion is performed

#### Scenario: Cleanup migration can recreate the former schema

- **WHEN** the cleanup migration is reverted
- **THEN** the four legacy tables are recreated with their former columns,
  indexes, and character foreign keys
