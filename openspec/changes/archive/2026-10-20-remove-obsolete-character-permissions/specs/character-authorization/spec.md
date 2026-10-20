## ADDED Requirements

### Requirement: Character authorization names come from Identity roles

The system SHALL use the character's ASP.NET Identity role assignments as the
authoritative source of authorization names exposed to contact-message
consumers.

#### Scenario: Character lookup includes assigned role names

- **GIVEN** a character has one or more assignments in the Identity role
  store
- **WHEN** the Contacts service looks up that character for a contact message
- **THEN** the existing sender claim payload contains the assigned role names
- **AND** the payload is sufficient for the existing client-rights mapping

#### Scenario: Character lookup with no roles

- **GIVEN** a character has no Identity role assignments
- **WHEN** the Contacts service looks up that character for a contact message
- **THEN** the sender claim payload contains no authorization names
- **AND** the existing client-rights mapping returns the default rights value

### Requirement: Legacy character permissions are not live authorization state

The system MUST NOT use the legacy `characters_permissions` storage as a live
source for character authorization names after the cleanup migration is
applied.

#### Scenario: Cleanup migration is applied

- **GIVEN** a database contains the historical `characters_permissions` table
- **WHEN** the current migration set is applied
- **THEN** the legacy table is removed
- **AND** the Identity role and role-assignment tables remain available

#### Scenario: Cleanup migration is reverted

- **GIVEN** the cleanup migration has been applied
- **WHEN** it is reverted
- **THEN** the legacy table schema is recreated
- **AND** no runtime authorization path is restored to read that table
