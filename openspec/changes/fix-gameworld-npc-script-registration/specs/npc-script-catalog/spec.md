## ADDED Requirements

### Requirement: Discover NPC metadata from an explicit catalog

GameWorld NPC metadata discovery MUST consume explicit script type catalogs and
MUST NOT enumerate every assembly loaded into the AppDomain.

#### Scenario: The game-script plugin is configured

- **WHEN** the game-script plugin is configured
- **THEN** it MUST register a catalog containing types from its own assembly
- **AND** owner-aware concrete `INpcScript` implementations MUST NOT be ordinary
  DI service descriptors

#### Scenario: A catalog contains metadata-bearing scripts

- **WHEN** the metadata factory reads one or more catalogs
- **THEN** it MUST return each applicable NPC script type and NPC id
- **AND** duplicate types MUST be evaluated only once

#### Scenario: A catalog assembly is partially loadable

- **WHEN** type enumeration raises `ReflectionTypeLoadException`
- **THEN** the catalog MUST retain all loadable types and continue discovery

#### Scenario: An unrelated loaded assembly contains an NPC script

- **WHEN** an NPC script type exists in an assembly absent from the catalogs and
  service descriptors
- **THEN** the metadata factory MUST exclude it

### Requirement: Preserve owner-aware activation

The NPC script activator MUST remain the construction boundary that supplies the
owning NPC to scripts and all existing NPC script providers MUST continue to
resolve their supported scripts.

#### Scenario: An NPC is composed with a discovered script

- **WHEN** an NPC provider selects a metadata-discovered script type
- **THEN** the existing owner-aware activator MUST create it with the owning
  NPC and the provider MUST expose the resulting script mapping
