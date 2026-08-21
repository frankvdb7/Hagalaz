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

#### Scenario: NPC construction failure disposes the child scope

- **WHEN** `NpcBuilder` has created a child scope but dependency resolution, script activation, or NPC construction throws
- **THEN** the builder disposes that child scope
- **AND** scoped disposable dependencies are released even though no NPC was constructed

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
- **AND** domain relationships use a dedicated typed activation contract rather than an arbitrary argument bag
- **AND** the script can use those inputs without accessing `INpc.ServiceProvider`

#### Scenario: NPC dialogue creates a character-scoped widget script

- **WHEN** an NPC script opens a dialogue or NPC-owned widget for a character
- **THEN** it creates the requested script through the typed widget activator for that character
- **AND** the created script comes from the character's scope

### Requirement: Familiar composition stays at the application boundary

Familiar creation and restoration MUST be coordinated by `IFamiliarFactory`. `ICharacter` MUST expose only the domain state transitions for attaching an already-created familiar and detaching the specific active familiar; it MUST NOT depend on NPC script activation, summoning definitions, or pending restoration state.

#### Scenario: Familiar creation supplies an owner-aware script

- **WHEN** a character summons a familiar
- **THEN** the familiar factory creates the NPC through the NPC builder
- **AND** it activates the selected script with the newly-created NPC as owner
- **AND** it attaches the summoner and familiar definition before NPC registration
- **AND** it attaches the created familiar to the character through an explicit state transition

#### Scenario: Familiar restoration supplies persisted state

- **WHEN** character hydration contains familiar and familiar-inventory data
- **THEN** the hydrators store that data in scoped restoration state
- **AND** the familiar factory resolves the definition and composes the owner-aware familiar during character registration
- **AND** the restored familiar's persisted runtime state, including remaining ticks and inventory, is effective after the complete registration and `OnSpawn` lifecycle
- **AND** the character does not create or activate the familiar itself

#### Scenario: Familiar registration failure rolls back active state

- **WHEN** familiar script activation attaches a familiar to the character but NPC store registration rejects the NPC or NPC registration throws
- **THEN** familiar spawning fails without returning a successful NPC handle
- **AND** the character detaches that same familiar instance
- **AND** failed restoration clears its scoped pending familiar state and inventory
- **AND** a failed normal summon is not treated as a successful summon by its caller

#### Scenario: Missing familiar definition rejects restoration atomically

- **WHEN** character hydration contains a familiar identifier that is absent from the startup-loaded summoning definition store
- **THEN** familiar restoration returns without attaching an active familiar
- **AND** the character reports no familiar through `HasFamiliar()`
- **AND** the scoped pending familiar state and inventory are cleared
- **AND** a later normal summon does not receive the rejected restoration data

### Requirement: NPC behavior remains unchanged

The dependency refactor MUST preserve existing NPC spawn, unregister, script, movement, combat, familiar, player-NPC, rendering, and loot behavior.

#### Scenario: NPC spawn and unregister retain behavior

- **WHEN** an NPC is spawned and later unregistered
- **THEN** registration and removal use the same NPC service and child scope as before
- **AND** script callbacks and scope disposal occur in the existing lifecycle order

#### Scenario: Rejected NPC registration is not successful

- **WHEN** the NPC store rejects an NPC during registration
- **THEN** registration reports failure to the composition boundary
- **AND** `NpcBuilder.Spawn()` does not return an NPC handle
- **AND** the partially composed NPC is destroyed at the common registration boundary so direct `Build()` callers also release its scope and owned lifecycle resources

#### Scenario: NPC registration rolls back after initialization failure

- **WHEN** the NPC store accepts an NPC but `OnRegistered()` throws
- **THEN** the same NPC is removed from the store
- **AND** the same NPC is destroyed at the common registration boundary so direct `Build()` callers do not retain map-region membership or its child scope
- **AND** the original registration exception is propagated
- **AND** a destroyed or partially registered NPC is not retained by the world store

#### Scenario: NPC combat and loot retain behavior

- **WHEN** an NPC attacks, dies, or produces loot
- **THEN** the same combat calculations, callbacks, loot table, loot generator, and ground-item builder are used
- **AND** only dependency acquisition changes

#### Scenario: Familiar removal clears character state

- **WHEN** a familiar is dismissed, despawns, or is otherwise unregistered
- **THEN** the familiar lifecycle asks the character to detach the specific familiar being removed
- **AND** the character's active familiar script and familiar ID are cleared together only when that familiar is still active
- **AND** `HasFamiliar()` returns `false`
- **AND** the character can summon another familiar

#### Scenario: Stale familiar removal preserves the newer familiar

- **WHEN** familiar A is replaced by familiar B and a delayed or duplicate removal for A is processed
- **THEN** the generic NPC unregister path does not mutate character familiar state
- **AND** identity-aware familiar detachment leaves B active

#### Scenario: Familiar teardown releases summoner handlers

- **WHEN** familiar A is dismissed and destroyed before familiar B is summoned
- **THEN** A unregisters every event handler it registered on the summoner during its teardown
- **AND** a subsequent combat-target event is handled only by B
