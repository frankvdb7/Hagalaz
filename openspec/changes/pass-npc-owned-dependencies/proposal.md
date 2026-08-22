## Why

`Npc` still retains a per-NPC service scope and lets its owned components resolve ordinary dependencies through `IServiceProvider`. The same hidden lookup pattern remains in the common NPC script path, so constructors do not describe what the NPC graph needs and focused tests must assemble provider infrastructure.

## What Changes

- Keep per-NPC scope creation and disposal in `NpcBuilder`, and compose `Npc` from that scope with typed dependencies and explicit runtime inputs.
- Resolve typed NPC constructor dependencies at `NpcBuilder` and call the constructor directly so omitted nullable builder options remain valid.
- **BREAKING**: Make `Npc` and its NPC-specific partial/component construction use injected services instead of resolving them from the stored scope.
- **BREAKING**: Pass typed dependencies into `Movement`, `NpcAppearance`, `NpcStatistics`, `NpcCombat`, `NpcHandle`, and the shared combat constructor used by NPC combat.
- Move runtime NPC script type selection and definition lookup to `NpcBuilder`, which remains the composition boundary.
- Keep `INpcScript.Initialize(INpc owner)` as the narrow owner-binding lifecycle needed by owner-independent familiar hydration, while creating common NPC scripts through a narrow owner-aware activator.
- Move concrete NPC setup into constructors where it is safe to do so, and keep familiar setup in the domain-specific summoner attachment operation before registration.
- Remove ordinary provider lookups from the common NPC script activation path and the directly affected specialized NPC scripts by passing their concrete services through typed construction or activation contracts.
- Update focused NPC, movement, combat, script, spawn, and lifecycle tests to provide direct substitutes.

## Capabilities

### New Capabilities

- `explicit-npc-dependencies`: NPC composition and NPC-owned components expose their required typed services while preserving the per-NPC scope and runtime behavior.

### Modified Capabilities

- None.

## Impact

- Affected production code is under `Hagalaz.Services.GameWorld/Builders`, `Factories`, and `Model/Creatures`, plus the common NPC script and directly affected NPC script paths in `Hagalaz.Game.Scripts`.
- Constructor signatures for NPC components and shared movement/combat paths change, so focused tests and direct callers must be updated.
- No new package, generic resolver, service bag, background worker, persistence migration, or lifetime owner is introduced.
- NPC spawn, unregister, script activation, scope disposal, rendering, movement, combat, and loot behavior must remain unchanged.

## Acceptance criteria

- `NpcBuilder` remains responsible for creating and owning the per-NPC scope and resolves scoped services from that scope.
- `Npc`, its owned components, and the covered NPC script paths contain no ordinary service-provider lookup at the point of use.
- Required services appear as required typed constructor or narrow typed activation inputs, with no optional dependency parameters or compatibility overloads.
- Runtime NPC script activation stays at `NpcBuilder` or a narrow typed boundary.
- Focused tests can construct migrated components with direct substitutes, and existing NPC behavior remains unchanged.

## Non-goals and stop conditions

- Do not refactor the complete `Creature` or character/NPC inheritance hierarchy.
- Do not remove every `IServiceProvider` use from the repository or redesign unrelated character, item, widget, area, or game-object graphs.
- Do not change gameplay, persistence, protocol, or service lifetime policy.
- Stop and record follow-up work if a provider lookup belongs to an unrelated graph and cannot be changed without broadening this issue.
