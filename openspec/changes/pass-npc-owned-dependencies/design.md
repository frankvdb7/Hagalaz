## Context

`Npc` retains a per-NPC service scope, but its owned components and common
scripts were using that scope as a service locator. The refactor makes the
NPC graph explicit while keeping `NpcBuilder` responsible for child-scope
composition and preserving the existing lifecycle.

## Goals / Non-Goals

**Goals:**

- Keep scope creation, runtime script selection, and NPC composition at
  `NpcBuilder`.
- Make `Npc` and its owned components use required typed dependencies.
- Make owner-aware NPC script activation explicit and narrow.
- Preserve NPC spawn, unregister, scope, and gameplay behavior.
- Adapt active familiar summoning only where the owner-aware constructor
  requires it.

**Non-Goals:**

- Replacing the existing familiar restoration behavior with a new subsystem. The existing familiar character script may retain persisted familiar data until the owner-aware NPC composition boundary can create the script.
- Removing `IServiceProvider` from the shared `Creature` hierarchy or unrelated
  graphs.
- Replacing script metadata providers, adding a service bag, or changing game
  rules, persistence, protocol, or service lifetimes.

## Decisions

1. **Use `NpcBuilder` as the NPC composition boundary.** The builder creates
   the child scope, resolves the selected script, definition, and typed NPC
   dependencies, activates the selected script with the real NPC owner, and
   calls `Npc` directly with the runtime values. Callers may provide only an
   optional domain-specific configuration action for the activated script.

2. **Keep the scope as a lifetime input, not a lookup API.** `Npc` continues to
   pass the scope to `Creature` so the existing lifetime implementation owns
   disposal. NPC-owned components receive the services they use directly.

3. **Use a narrow typed activation contract.** NPC scripts use only
   `INpcScriptActivator.Create(Type, INpc)`. The Glacor/Glacyte relationship
   is composed by local Glacor gameplay code after owner-aware activation; it
   does not enlarge the generic activator. No arbitrary constructor argument
   bag or service bag is introduced.

4. **Construct scripts with their owners.** Ordinary and familiar NPC scripts
   use the typed owner-aware activation boundary. `NpcBuilder` performs that
   activation and does not expose the script activator through its fluent API.
   There is no generic post-construction owner-binding lifecycle. Active
   familiar setup uses `IFamiliarScript.AttachToSummoner` before registration.
   The Glacor encounter binds the Enduring Glacyte to its owning Glacor
   gameplay relationship after activation.

5. **Keep familiar creation close to its existing caller.**
   `SummoningSkillService` continues to use `NpcBuilder`; its script
   configuration attaches the summoner and records the active familiar on the
   character after builder-owned activation. No familiar
   factory, restoration coordinator, or persistence state store is added.

6. **Keep familiar teardown local to the familiar script.**
   `FamiliarScriptBase` owns the handlers it registers, cleans them up when
   attachment fails or the familiar is destroyed, and detaches only the
   matching active NPC from its summoner. `NpcService` remains generic.

7. **Preserve familiar restoration without ownerless scripts.**
   `FamiliarHydrator` forwards persisted familiar data through the existing
   hydration contracts. `FamiliarCharacterScript` retains that data locally,
   composes the familiar through `NpcBuilder`, attaches the summoner, and
   applies the state through the existing familiar hydration contracts. No
   familiar factory, restoration coordinator, or persistence state store is
   added.

## Risks / Trade-offs

- Runtime arguments could be bound incorrectly when several arguments share a
  type or are null. `NpcBuilder` resolves typed services and passes constructor
  values directly.
- Active familiar attachment can fail while registering handlers. The local
  `AttachToSummoner` cleanup resets the partially attached script and removes
  the handlers it already registered.
- Familiar restoration remains on its existing character hydration and
  registration path. Only the timing of script construction changes so the
  script receives its real NPC owner at construction time.

## Migration Plan

1. Add the explicit NPC dependency capability and owner-aware activator.
2. Migrate `NpcBuilder`, `Npc`, NPC-owned components, and common script paths.
3. Adapt active familiar composition and retain local familiar teardown.
4. Run focused tests, affected builds, strict OpenSpec validation, and the
   final provider scan.

Rollback is a source revert. No persisted data or deployment migration is
required.
