## ADDED Requirements

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
