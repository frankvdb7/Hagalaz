## Context

`NpcBuilder` is a singleton builder that creates one child `IServiceScope` for each NPC. The scope is currently retained by `Creature`, but `Npc` and its owned components repeatedly use the scope as a service locator. `NpcBuilder` also resolves the selected script and NPC definition before constructing the object.

The change must preserve that lifetime boundary, runtime-selected script types, construction order, and the shared `Creature` lifetime implementation. The NPC path also includes the common NPC script callbacks and the directly affected familiar and player-NPC script paths.

## Goals / Non-Goals

**Goals:**

- Keep scope creation, runtime script selection, and NPC composition at `NpcBuilder`.
- Make `Npc` and its owned movement, rendering, appearance, statistics, combat, and handle dependencies explicit.
- Make the common NPC script services and the NPC owner required constructor dependencies.
- Preserve construction order, scope identity, spawn/unregister behavior, and gameplay results.
- Keep familiar character registration free of synchronous database I/O by using startup-loaded summoning definitions.
- Leave the general `Creature` hierarchy and unrelated provider-based services unchanged.

**Non-Goals:**

- Removing `IServiceProvider` from the repository or from the shared `Creature` base.
- Replacing the existing script metadata providers or adding a generic resolver, service bag, or dependency context.
- Refactoring all concrete NPC behavior or changing game rules, persistence, protocol, or service lifetimes.

## Decisions

1. **Use `NpcBuilder` as the only NPC composition boundary.** The builder keeps the child scope, resolves the selected script, definition, and typed NPC dependencies from that scope, and calls the `Npc` constructor directly with runtime values. This keeps nullable runtime values typed at the composition boundary and makes the constructor dependency list explicit.

2. **Keep the scope as a lifetime input, not a lookup API.** `Npc` continues to pass the scope to `Creature` so destruction disposes the same scope. `Npc` and its owned components receive typed services and never use `ServiceProvider` for ordinary work. Removing the scope from `Creature` would expand this issue into the shared creature lifetime refactor.

3. **Pass each owned component its actual services.** `Movement`, `NpcAppearance`, `NpcStatistics`, and `NpcCombat` receive only the services they use. `NpcCombat` uses the explicit `CreatureCombat` constructor already established by the character dependency work. A shared `NpcDependencies` object was rejected because it would hide the dependency graph behind a service bag.

4. **Inject shared script services and runtime values through construction.** `NpcBuilder` supplies a narrow `INpcScriptActivator` to `Npc`, and the activator creates the selected script with the newly constructed NPC as an explicit runtime argument. It also accepts additional typed runtime values for domain relationships, such as a Glacyte's parent Glacor; all ordinary services remain resolved from the NPC's own child scope. `NpcScriptBase` therefore receives `INpc`, `INpcService`, `IPathFinder`, and `IWidgetScriptActivator` through its required constructor, stores them for callbacks, and exposes typed `CreateWidgetScript<T>`. The generic script initialization lifecycle method is removed. Setup that belongs to a concrete NPC is performed in its constructor, while familiar setup is performed by the domain-specific `IFamiliarScript.AttachToSummoner` operation before hydration and registration. `OnCreate` remains an independent overridable registration callback. A service bag, `IServiceProvider` in scripts, and optional constructor dependencies were rejected.

5. **Keep script-specific dependencies in script constructors.** Familiar scripts receive their item builder directly, player-NPC scripts receive `INpcService`, and specialized NPC scripts receive their map, loot, and path-finding services directly. Existing constructors remain required arguments; no optional compatibility overloads are added.

6. **Pass the resolved NPC service into `NpcHandle`.** The builder already owns the child-scope service resolution during spawn. The handle retains that typed service instead of resolving it again from `Npc` when unregistering. The handle does not create or own a second scope.

7. **Keep familiar restoration compatible with owner-first construction.** Character hydration stores the familiar type and persisted state until the familiar NPC is composed. The character then creates the owner-aware script through the same typed activation boundary, attaches its summoner data and familiar-specific handlers/inventory, applies persisted state, and exposes the script before NPC registration. This avoids constructing an NPC script without its required owner or adding a generic owner-binding lifecycle API.

8. **Use a startup-loaded summoning definition store for synchronous familiar registration.** `FamiliarCharacterScript.OnRegistered` remains synchronous, so it reads the familiar definition from a singleton store populated by the existing startup service executor. The general `ISummoningService` remains asynchronous for skill operations; blocking an EF query from a character callback is not an acceptable lookup path.

## Risks / Trade-offs

- [Risk] Runtime arguments could be bound incorrectly when several arguments share `ILocation` or are null. → Resolve typed services in `NpcBuilder` and call the constructor directly; validate the ordinary spawn path with omitted optional values.
- [Risk] Moving component construction may change initialization order. → Set location before components that read it, construct the owned components, activate the script, and perform concrete-script constructor setup before registration and `OnSpawn`; keep `OnCreate` as a separate registration callback.
- [Risk] A familiar may be created successfully but fail during summoner attachment. → Keep `AttachToSummoner` immediately after owner-aware script activation and before hydration/registration, and let the existing builder scope-disposal path handle construction failures.
- [Risk] Remaining provider matches may be mistaken for NPC-domain lookups. → Run a final scan limited to the NPC composition graph and document the two allowed boundaries: `NpcBuilder` composition and the shared `CreatureCombat` constructor outside this change.

## Migration Plan

1. Add the explicit NPC dependency capability and update the script activation contract.
2. Change `NpcBuilder`, `Npc`, `NpcHandle`, and NPC-owned components to use typed services from the child scope.
3. Migrate common, familiar, player-NPC, and directly affected concrete NPC script paths, then update direct callers and focused tests.
4. Run focused GameWorld and Game.Scripts tests, a solution build, strict OpenSpec validation, and the final provider scan.
5. Rollback is a source revert. No persisted data or deployment migration is required.

## Open Questions

None.
