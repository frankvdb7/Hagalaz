## ADDED Requirements

### Requirement: Character state identifiers remain string identifiers

The Characters service MUST preserve persisted character state identifiers as strings when projecting character hydration responses and applying character snapshots.

#### Scenario: Hydrate a persisted string state identifier

- **WHEN** a character has a `CharactersState` row whose `StateId` is a string identifier
- **THEN** the Characters service projects the row successfully and returns the same identifier in `StateDto`

#### Scenario: Persist a string state identifier

- **WHEN** a character snapshot contains a string state identifier
- **THEN** snapshot persistence compares and stores that identifier without numeric conversion
