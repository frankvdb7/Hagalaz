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

- Redesigning familiar restoration or introducing pending restoration state.
- Removing `IServiceProvider` from the shared `Creature` hierarchy or unrelated
  graphs.
- Replacing script metadata providers, adding a service bag, or changing game
  rules, persistence, protocol, or service lifetimes.

## Decisions

1. **Use `NpcBuilder` as the NPC composition boundary.** The builder creates
   the child scope, resolves the selected script, definition, and typed NPC
   dependencies, and calls `Npc` directly with the runtime values.

2. **Keep the scope as a lifetime input, not a lookup API.** `Npc` continues to
   pass the scope to `Creature` so the existing lifetime implementation owns
   disposal. NPC-owned components receive the services they use directly.

3. **Use a narrow typed activation contract.** Ordinary scripts use
   `INpcScriptActivator.Create(Type, INpc)`. The Glacyte parent relationship
   uses its dedicated typed `CreateWithParent<TScript>` method. No arbitrary
   constructor argument bag or service bag is introduced.

4. **Remove the generic owner-binding lifecycle.** `INpcScript.Initialize` is
   removed. Concrete NPC setup belongs in constructors. Active familiar setup
   uses `IFamiliarScript.AttachToSummoner` after owner-aware construction and
   before registration.

5. **Keep familiar creation close to its existing caller.**
   `SummoningSkillService` continues to use `NpcBuilder`; its script factory
   activates the selected familiar in the NPC child scope, attaches the
   summoner, and records the active familiar on the character. No familiar
   factory, restoration coordinator, or persistence state store is added.

6. **Keep familiar teardown local to the familiar script.**
   `FamiliarScriptBase` owns the handlers it registers, cleans them up when
   attachment fails or the familiar is destroyed, and detaches only the
   matching active NPC from its summoner. `NpcService` remains generic.

7. **Leave the pre-existing restoration boundary unchanged.** The former
   restoration path constructs a familiar script before an NPC owner exists.
   Making that path owner-aware would require a separate composition design,
   so restoration changes are explicitly deferred rather than added here.

## Risks / Trade-offs

- Runtime arguments could be bound incorrectly when several arguments share a
  type or are null. `NpcBuilder` resolves typed services and passes constructor
  values directly.
- Active familiar attachment can fail while registering handlers. The local
  `AttachToSummoner` cleanup resets the partially attached script and removes
  the handlers it already registered.
- Familiar restoration still has the pre-existing owner-before-construction
  limitation. It is recorded as follow-up work instead of being coupled to
  this dependency refactor.

## Migration Plan

1. Add the explicit NPC dependency capability and owner-aware activator.
2. Migrate `NpcBuilder`, `Npc`, NPC-owned components, and common script paths.
3. Adapt active familiar composition and retain local familiar teardown.
4. Run focused tests, affected builds, strict OpenSpec validation, and the
   final provider scan.

Rollback is a source revert. No persisted data or deployment migration is
required.

## Open Questions

Owner-aware familiar restoration is a separate follow-up issue.
