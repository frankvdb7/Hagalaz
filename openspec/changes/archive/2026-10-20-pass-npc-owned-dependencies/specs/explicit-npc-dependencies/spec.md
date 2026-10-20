## ADDED Requirements

### Requirement: NPC composition uses explicit dependencies

The NPC composition graph MUST expose ordinary service dependencies through
typed constructor inputs or narrow typed activation inputs, and `NpcBuilder`
MUST compose the graph from the per-NPC scope.

#### Scenario: NPC creation uses the child scope

- **WHEN** `NpcBuilder` creates an NPC with a location, definition, and
  selected script type
- **THEN** it creates the child scope for that NPC
- **AND** it resolves the selected script and definition from that scope
- **AND** it composes `Npc` with typed service dependencies and explicit runtime
  values

### Requirement: NPC-owned components declare service requirements

This requirement MUST be satisfied by the NPC composition graph.

`Npc`, `Movement`, `NpcAppearance`, `NpcStatistics`, `NpcCombat`,
`NpcRenderInformation`, and `NpcHandle` SHALL receive each ordinary service
they use through required typed constructor or composition-boundary inputs.
They MUST NOT use an owner `ServiceProvider` for ordinary service resolution.

#### Scenario: NPC movement, combat, appearance, and statistics use direct services

- **WHEN** an NPC moves, attacks, transforms, or receives poison damage
- **THEN** its owned components use the services supplied by `NpcBuilder`
- **AND** those operations do not perform provider lookups through the NPC
  owner

#### Scenario: NPC unregisters through a handle

- **WHEN** an `NpcHandle` unregisters its NPC
- **THEN** it uses the typed NPC service supplied by the composition boundary
- **AND** it does not resolve the service from the NPC

### Requirement: NPC script activation is typed and scoped

The common NPC script path MUST receive its NPC service, path finder, and
character-widget activation capability through required typed constructor
inputs. Runtime-selected NPC scripts and NPC-related dialogue/widget scripts
MUST NOT use an arbitrary service provider at the point of use.

#### Scenario: NPC script activates with scoped services

- **WHEN** an NPC is composed with a runtime-selected script
- **THEN** the script is constructed with services from the same child scope
- **AND** the owner is supplied through the owner-aware activation boundary
- **AND** `NpcBuilder` performs script activation with
  `INpcScriptActivator.Create(Type, owner)`
- **AND** optional caller configuration receives only the activated script
- **AND** Glacor/Glacyte membership is composed by local Glacor gameplay code
  rather than by enlarging the generic activator
- **AND** no arbitrary argument bag is introduced

#### Scenario: NPC dialogue creates a character-scoped widget script

- **WHEN** an NPC script opens a dialogue or NPC-owned widget for a character
- **THEN** it creates the requested script through the typed widget activator
- **AND** the created script comes from the character's scope

### Requirement: Active familiar composition supplies the NPC owner

Active familiar summoning MUST compose the familiar through `NpcBuilder` and
the NPC child scope. The familiar script MUST receive its NPC owner during
construction, ordinary services MUST remain explicit, and no generic
activation argument bag may be introduced.

#### Scenario: Familiar creation supplies an owner-aware script

- **WHEN** a character summons a familiar
- **THEN** `NpcBuilder` activates the selected familiar from the NPC child
  scope with its newly-created NPC owner
- **AND** it attaches the summoner and familiar definition before registration
- **AND** the script receives the newly-created NPC as its owner before it is used
- **AND** it records the active familiar on the character

### Requirement: Familiar teardown remains domain-local

The generic NPC registration service MUST NOT know about familiar-specific
character state. Familiar scripts MUST own the event handlers they register
and MUST detach only their matching active NPC during teardown.

#### Scenario: Familiar removal clears active character state

- **WHEN** a familiar is dismissed, despawns, or is otherwise destroyed
- **THEN** its familiar lifecycle removes its summoner handlers
- **AND** it detaches that specific familiar from the character
- **AND** the character can summon another familiar

#### Scenario: Stale familiar removal preserves the newer familiar

- **WHEN** familiar A is replaced by familiar B and a delayed removal for A is
  processed
- **THEN** generic NPC unregister does not mutate familiar state
- **AND** identity-aware detachment leaves B active

#### Scenario: Familiar attachment failure is locally cleaned up

- **WHEN** handler registration or familiar-specific setup fails during
  `AttachToSummoner`
- **THEN** the handlers registered before the failure are removed
- **AND** the familiar script resets its partial attachment state

### Requirement: Existing NPC behavior remains unchanged

The dependency refactor MUST preserve existing NPC spawn, unregister, script,
movement, combat, rendering, loot, and active familiar behavior. Owner-aware
familiar restoration MUST retain its existing hydration and registration
behavior; persisted data may be staged until the owner-aware NPC composition
boundary creates the script, but this change MUST NOT replace restoration with
a new subsystem.

#### Scenario: NPC spawn and unregister retain behavior

- **WHEN** an NPC is spawned and later unregistered
- **THEN** registration and removal use the same NPC service and child scope
- **AND** script callbacks and scope disposal retain their existing lifecycle

#### Scenario: NPC combat and loot retain behavior

- **WHEN** an NPC attacks, dies, or produces loot
- **THEN** the same calculations, callbacks, loot table, loot generator, and
  ground-item builder are used
  - **AND** only dependency acquisition changes

#### Scenario: Persisted familiar restoration remains active

- **WHEN** character hydration contains a persisted familiar
- **THEN** `FamiliarHydrator` invokes the existing familiar hydration contract
- **AND** the existing familiar character script retains the persisted familiar
  data without constructing an ownerless familiar script
- **AND** the character's familiar registration script composes the familiar
  through `NpcBuilder` with the newly-created NPC as owner
- **AND** the persisted familiar runtime state and inventory are effective
  after the NPC registration and spawn callbacks complete
- **AND** no replacement familiar factory, restoration coordinator, or
  persistence state store is introduced
