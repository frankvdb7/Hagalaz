## ADDED Requirements

### Requirement: Discover NPC metadata from the existing service descriptor source

GameWorld NPC metadata discovery MUST consume plugin assemblies exposed through
the existing `IServiceDescriptorProvider` and MUST NOT enumerate every assembly
loaded into the AppDomain or NPC script service descriptors.

#### Scenario: The game-script plugin is configured

- **WHEN** the game-script plugin is configured
- **THEN** the plugin host MUST expose its assembly as an infrastructure
  assembly descriptor
- **AND** owner-aware concrete `INpcScript` implementations MUST NOT be ordinary
  DI service descriptors

#### Scenario: A plugin assembly contains metadata-bearing scripts

- **WHEN** the metadata factory reads one or more plugin assemblies
- **THEN** it MUST return each applicable NPC script type and NPC id
- **AND** duplicate types MUST be evaluated only once

#### Scenario: A plugin assembly is partially loadable

- **WHEN** type enumeration raises `ReflectionTypeLoadException`
- **THEN** the metadata factory MUST retain the non-null loadable types
- **AND** it MUST log the loader exception
- **AND** startup MUST continue with those usable types

#### Scenario: An unrelated loaded assembly contains an NPC script

- **WHEN** an NPC script type exists in an assembly absent from the plugin
  assembly descriptors and NPC service descriptors
- **THEN** the metadata factory MUST exclude it

### Requirement: Preserve owner-aware activation

The NPC script activator MUST remain the construction boundary that supplies the
owning NPC to scripts and all existing NPC script providers MUST continue to
resolve their supported scripts.

#### Scenario: An NPC is composed with a discovered script

- **WHEN** an NPC provider selects a metadata-discovered script type
- **THEN** the existing owner-aware activator MUST create it with the owning
  NPC and the provider MUST expose the resulting script mapping
