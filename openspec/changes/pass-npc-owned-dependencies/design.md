## Context

`NpcBuilder` is a singleton builder that creates one child `IServiceScope` for each NPC. The scope is currently retained by `Creature`, but `Npc` and its owned components repeatedly use the scope as a service locator. `NpcBuilder` also resolves the selected script and NPC definition before constructing the object.

The change must preserve that lifetime boundary, the existing initialization order, runtime-selected script types, and the shared `Creature` lifetime implementation. The NPC path also includes the common NPC script callbacks and the directly affected familiar and player-NPC script paths.

## Goals / Non-Goals

**Goals:**

- Keep scope creation, runtime script selection, and NPC composition at `NpcBuilder`.
- Make `Npc` and its owned movement, rendering, appearance, statistics, combat, and handle dependencies explicit.
- Make the common NPC script services and the NPC owner required constructor dependencies.
- Preserve construction order, scope identity, spawn/unregister behavior, and gameplay results.
- Leave the general `Creature` hierarchy and unrelated provider-based services unchanged.

**Non-Goals:**

- Removing `IServiceProvider` from the repository or from the shared `Creature` base.
- Replacing the existing script metadata providers or adding a generic resolver, service bag, or dependency context.
- Refactoring all concrete NPC behavior or changing game rules, persistence, protocol, or service lifetimes.

## Decisions

1. **Use `NpcBuilder` as the only NPC composition boundary.** The builder keeps the child scope, resolves the selected script and definition from that scope, and calls `ActivatorUtilities.CreateInstance<Npc>` with runtime values. This reuses the existing scope instead of duplicating the dependency graph in a second factory. Direct `new Npc(...)` plus a manually maintained list of every typed service was rejected because it would make the builder a second constructor definition.

2. **Keep the scope as a lifetime input, not a lookup API.** `Npc` continues to pass the scope to `Creature` so destruction disposes the same scope. `Npc` and its owned components receive typed services and never use `ServiceProvider` for ordinary work. Removing the scope from `Creature` would expand this issue into the shared creature lifetime refactor.

3. **Pass each owned component its actual services.** `Movement`, `NpcAppearance`, `NpcStatistics`, and `NpcCombat` receive only the services they use. `NpcCombat` uses the explicit `CreatureCombat` constructor already established by the character dependency work. A shared `NpcDependencies` object was rejected because it would hide the dependency graph behind a service bag.

4. **Inject shared script services and the owner through construction.** `NpcBuilder` supplies a narrow `INpcScriptActivator` to `Npc`, and the activator creates the selected script with the newly constructed NPC as an explicit runtime argument. `NpcScriptBase` therefore receives `INpc`, `INpcService`, `IPathFinder`, and `IWidgetScriptActivator` through its required constructor, stores them for callbacks, and exposes typed `CreateWidgetScript<T>`. The public owner-binding lifecycle method is removed; the existing protected initialization hook is invoked by `OnCreate` so behavior-specific setup still runs at the existing registration boundary. A service bag, `IServiceProvider` in scripts, and optional constructor dependencies were rejected.

5. **Keep script-specific dependencies in script constructors.** Familiar scripts receive their item builder directly, player-NPC scripts receive `INpcService`, and specialized NPC scripts receive their map, loot, and path-finding services directly. Existing constructors remain required arguments; no optional compatibility overloads are added.

6. **Pass the resolved NPC service into `NpcHandle`.** The builder already owns the child-scope service resolution during spawn. The handle retains that typed service instead of resolving it again from `Npc` when unregistering. The handle does not create or own a second scope.

7. **Keep familiar restoration compatible with owner-first construction.** Character hydration stores the familiar type and persisted state until the familiar NPC is composed. The character then creates the owner-aware script through the same typed activation boundary, initializes its summoner data, applies persisted state, and exposes the script before NPC registration. This avoids constructing an NPC script without its required owner or adding a second owner-binding API.

## Risks / Trade-offs

- [Risk] `ActivatorUtilities` could bind runtime arguments incorrectly when several arguments share `ILocation`. → Keep the runtime arguments in constructor order and validate NPC construction through the focused spawn/lifecycle path and a build.
- [Risk] Moving component construction may change initialization order. → Preserve the current order: viewport and movement, definition and bounds, rendering information, statistics and appearance, combat, location, then script initialization.
- [Risk] A script may be created successfully but fail during owner binding. → Keep owner binding in the NPC constructor after all owned components exist, and let the existing builder scope-disposal path handle construction failures.
- [Risk] Remaining provider matches may be mistaken for NPC-domain lookups. → Run a final scan limited to the NPC composition graph and document the two allowed boundaries: `NpcBuilder` composition and the shared `CreatureCombat` constructor outside this change.

## Migration Plan

1. Add the explicit NPC dependency capability and update the script activation contract.
2. Change `NpcBuilder`, `Npc`, `NpcHandle`, and NPC-owned components to use typed services from the child scope.
3. Migrate common, familiar, player-NPC, and directly affected concrete NPC script paths, then update direct callers and focused tests.
4. Run focused GameWorld and Game.Scripts tests, a solution build, strict OpenSpec validation, and the final provider scan.
5. Rollback is a source revert. No persisted data or deployment migration is required.

## Open Questions

None.
