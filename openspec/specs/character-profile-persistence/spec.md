# character-profile-persistence Specification

## Purpose
Defines the observable behavior and guarantees for character profile persistence in Hagalaz.

## Requirements

### Requirement: Character preferences use the profile persistence boundary

The system SHALL use the character profile store as the authoritative live persistence boundary for character preferences and SHALL NOT expose the legacy relational preferences entity to application code.

#### Scenario: Profile-backed character state is persisted

- **GIVEN** a character profile is loaded or updated
- **WHEN** the character persistence workflow reads or writes profile data
- **THEN** it uses the existing `CharactersProfile` profile store
- **AND** it does not read or write the legacy `characters_preferences` table

#### Scenario: No live legacy preference references remain

- **GIVEN** the current application and EF model source is inspected
- **WHEN** the character persistence model is built
- **THEN** it contains no live `CharactersPreference` entity or `CharactersPreferences` `DbSet`
- **AND** the current model snapshot contains no `characters_preferences` table

### Requirement: Legacy preferences storage is removed by migration

The system SHALL remove the obsolete `characters_preferences` table through the normal EF migration sequence while preserving the profile table.

#### Scenario: Cleanup migration is applied

- **GIVEN** a database contains the historical `characters_preferences` table
- **WHEN** the current migration set is applied
- **THEN** the `characters_preferences` table is removed
- **AND** the `characters_profiles` table remains available
- **AND** no other character persistence table is removed by this cleanup migration

#### Scenario: Cleanup migration is reverted

- **GIVEN** the cleanup migration has been applied
- **WHEN** it is reverted
- **THEN** the historical `characters_preferences` schema is recreated
- **AND** no runtime application path is restored to use that table

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
