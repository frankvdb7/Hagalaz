## ADDED Requirements

### Requirement: NPC composition uses explicit dependencies

The NPC composition graph MUST expose ordinary service dependencies through typed constructor inputs or narrow typed activation inputs, and `NpcBuilder` MUST compose the graph from the per-NPC scope.

#### Scenario: NPC creation uses the child scope

- **WHEN** `NpcBuilder` creates an NPC with a location, definition, and selected script type
- **THEN** it creates or reuses the child scope for that NPC
- **AND** it resolves the selected script and definition from that scope
- **AND** it composes `Npc` with its typed service dependencies and explicit runtime values

#### Scenario: NPC destruction disposes the child scope

- **WHEN** a created NPC is destroyed
- **THEN** the scope owned by that NPC is disposed by the existing creature lifetime
- **AND** scoped dependencies are not resolved from or promoted to the application root

### Requirement: NPC-owned components declare service requirements

`Npc`, `Movement`, `NpcAppearance`, `NpcStatistics`, `NpcCombat`, `NpcRenderInformation`, and `NpcHandle` MUST receive each ordinary service they use through required typed constructor or composition-boundary inputs. They MUST NOT use an owner `ServiceProvider` for ordinary service resolution.

#### Scenario: NPC movement and combat receive direct services

- **WHEN** `Npc` constructs movement and combat
- **THEN** it supplies the path finder, mediator, combat options, hit-splat builder, loot services, ground-item builder, and NPC service directly
- **AND** the components do not resolve those services through the NPC owner

#### Scenario: NPC appearance and statistics update

- **WHEN** an NPC transforms or receives poison damage
- **THEN** appearance uses its injected NPC service
- **AND** statistics uses its injected hit-splat builder
- **AND** neither operation performs a provider lookup

#### Scenario: NPC unregisters through a handle

- **WHEN** an `NpcHandle` unregisters its NPC
- **THEN** it uses the typed NPC service supplied by the composition boundary
- **AND** it does not resolve the service from the NPC

### Requirement: NPC script activation is typed and scoped

The common NPC script path MUST receive its NPC service, path finder, and character-widget activation capability through required typed constructor inputs. Runtime-selected NPC scripts and NPC-related dialogue/widget scripts MUST NOT use an arbitrary service provider at the point of use.

#### Scenario: NPC script activates with scoped services

- **WHEN** an NPC is composed with a runtime-selected script
- **THEN** the script is constructed with the NPC service from the same child scope, a path finder, and the typed widget script activator
- **AND** the owner is supplied through the owner-aware script construction or activation boundary
- **AND** any additional domain relationship values are supplied as typed activation arguments
- **AND** the script can use those inputs without accessing `INpc.ServiceProvider`

#### Scenario: NPC dialogue creates a character-scoped widget script

- **WHEN** an NPC script opens a dialogue or NPC-owned widget for a character
- **THEN** it creates the requested script through the typed widget activator for that character
- **AND** the created script comes from the character's scope

### Requirement: NPC behavior remains unchanged

The dependency refactor MUST preserve existing NPC spawn, unregister, script, movement, combat, familiar, player-NPC, rendering, and loot behavior.

#### Scenario: NPC spawn and unregister retain behavior

- **WHEN** an NPC is spawned and later unregistered
- **THEN** registration and removal use the same NPC service and child scope as before
- **AND** script callbacks and scope disposal occur in the existing lifecycle order

#### Scenario: NPC combat and loot retain behavior

- **WHEN** an NPC attacks, dies, or produces loot
- **THEN** the same combat calculations, callbacks, loot table, loot generator, and ground-item builder are used
- **AND** only dependency acquisition changes

#### Scenario: Familiar removal clears character state

- **WHEN** a familiar is dismissed, despawns, or is otherwise unregistered
- **THEN** the character's active familiar script and familiar ID are cleared together
- **AND** `HasFamiliar()` returns `false`
- **AND** the character can summon another familiar
