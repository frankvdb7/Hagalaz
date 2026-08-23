## ADDED Requirements

### Requirement: Character composition uses explicit dependencies

The character composition graph MUST expose ordinary service dependencies through typed constructors, and `CharacterFactory` MUST compose the graph from the nested per-character scope.

#### Scenario: Character creation resolves from the character scope

- **WHEN** `CharacterFactory` creates a character with a session and game client
- **THEN** the factory creates one nested scope
- **AND** the character receives the scope, session, client, and typed services from that scope
- **AND** the character's owned components receive their required typed services directly

#### Scenario: Character destruction ends the nested scope

- **WHEN** a created character is destroyed
- **THEN** the nested scope created for that character is disposed
- **AND** scoped dependencies from that character are not promoted to the application root

### Requirement: Character methods do not resolve ordinary services

`Character` and its partial implementations MUST use constructor-injected fields or properties for normal service dependencies and MUST NOT call `ServiceProvider.GetRequiredService<T>()`, `ServiceProvider.GetService`, or equivalent provider lookup for those dependencies.

#### Scenario: Region and event operations use injected services

- **WHEN** a character updates its map, handles a console command, or reacts to a region change
- **THEN** the operation uses the service supplied during character construction
- **AND** no provider lookup occurs at the point of use

### Requirement: Character-owned components declare service requirements

Character-owned containers, appearance, rendering, statistics, prayers, combat, magic, music, slayer, widgets, and farming tasks MUST receive each ordinary service they use through required constructor parameters.

#### Scenario: A component is unit-tested with direct substitutes

- **WHEN** a focused test constructs a character-owned component
- **THEN** the test supplies the component's required services directly
- **AND** the test does not need to create or configure an `IServiceProvider` solely for that component

### Requirement: Runtime-selected character scripts use narrow activators

Familiar scripts, character-NPC scripts, and other character scripts selected by runtime type MUST be created through a type-specific activator composed from the character scope. Character-owned components MUST NOT store or use an arbitrary service provider for this activation.

#### Scenario: Hydration creates the selected familiar script

- **WHEN** familiar state is hydrated for a character
- **THEN** the familiar provider maps the familiar ID to its script type
- **AND** the familiar activator creates that script in the character scope
- **AND** the hydrated script receives the same character-scoped dependencies as other character scripts

#### Scenario: Appearance creates the selected character-NPC script

- **WHEN** a character transforms into an NPC without a supplied script instance
- **THEN** the character-NPC provider maps the NPC ID to its script type
- **AND** the character-NPC activator creates that script in the character scope
- **AND** the appearance component initializes the created script as before

### Requirement: Gameplay behavior remains unchanged

The refactor MUST preserve existing character gameplay, persistence, protocol, and construction-order behavior while changing only how dependencies enter the graph.

#### Scenario: Character hydration and registration retain their order

- **WHEN** a character is hydrated and registered
- **THEN** appearance, inventory, statistics, script, map, and event behavior follows the existing lifecycle order
- **AND** no component observes an uninitialized required sibling caused by the constructor wiring

#### Scenario: Character-owned runtime actions keep their services

- **WHEN** combat, item drops, music, slayer completion, widget validation, or farming updates run
- **THEN** each action uses the same service implementation and scope as before
- **AND** the action produces the existing gameplay result
