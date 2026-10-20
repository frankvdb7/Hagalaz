## ADDED Requirements

### Requirement: Default character scripts resolve after context assignment

The GameWorld default character-script provider MUST defer creation of
context-dependent default scripts until `GetAllScripts` is called.

#### Scenario: Provider is resolved during character construction

- **WHEN** dependency injection resolves the default character-script provider
  before the `Character` constructor assigns its context
- **THEN** resolving the provider does not construct any default character
  script

#### Scenario: Character scripts are requested after context assignment

- **WHEN** `Character` calls `GetAllScripts` after assigning its scoped context
- **THEN** the provider returns the default character scripts from the current
  character scope
