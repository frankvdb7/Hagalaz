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

1. **Use `NpcBuilder` as the only NPC composition boundary.** The builder keeps the child scope, resolves the selected script, definition, and typed NPC dependencies from that scope, and calls the `Npc` constructor directly with runtime values. Construction and script activation run inside the builder's cleanup boundary; if construction fails before an NPC owns the scope, the builder disposes the child scope, while successful construction transfers scope ownership to `Npc`. This keeps nullable runtime values typed at the composition boundary and makes the constructor dependency list explicit.

2. **Keep the scope as a lifetime input, not a lookup API.** `Npc` continues to pass the scope to `Creature` so destruction disposes the same scope. `Npc` and its owned components receive typed services and never use `ServiceProvider` for ordinary work. Removing the scope from `Creature` would expand this issue into the shared creature lifetime refactor.

3. **Pass each owned component its actual services.** `Movement`, `NpcAppearance`, `NpcStatistics`, and `NpcCombat` receive only the services they use. `NpcCombat` uses the explicit `CreatureCombat` constructor already established by the character dependency work. A shared `NpcDependencies` object was rejected because it would hide the dependency graph behind a service bag.

4. **Inject shared script services and use narrow typed relationship activation.** `NpcBuilder` supplies a narrow `INpcScriptActivator` to `Npc`, and the activator creates the selected script with the newly constructed NPC as an explicit runtime argument. Ordinary scripts use `Create(Type, INpc)`; the Glacyte parent relationship uses the dedicated generic `CreateWithParent<TScript>(INpc, INpc)` contract. No arbitrary constructor-argument bag is exposed. All ordinary services remain resolved from the NPC's own child scope. `NpcScriptBase` therefore receives `INpc`, `INpcService`, `IPathFinder`, and `IWidgetScriptActivator` through its required constructor, stores them for callbacks, and exposes typed `CreateWidgetScript<T>`. The generic script initialization lifecycle method is removed. Setup that belongs to a concrete NPC is performed in its constructor, while familiar setup is performed by the domain-specific `IFamiliarScript.AttachToSummoner` operation before registration. Persisted familiar state is applied after registration and its `OnSpawn` lifecycle has completed so spawn initialization cannot overwrite it. `OnCreate` remains an independent overridable registration callback. A service bag, `IServiceProvider` in scripts, arbitrary activation arguments, and optional constructor dependencies were rejected.

5. **Keep script-specific dependencies in script constructors.** Familiar scripts receive their item builder directly, player-NPC scripts receive `INpcService`, and specialized NPC scripts receive their map, loot, and path-finding services directly. Existing constructors remain required arguments; no optional compatibility overloads are added.

6. **Pass the resolved NPC service into `NpcHandle`.** The builder already owns the child-scope service resolution during spawn. The handle retains that typed service instead of resolving it again from `Npc` when unregistering. The handle does not create or own a second scope.

7. **Keep familiar composition outside the character entity.** `FamiliarFactory` owns familiar NPC builder use, runtime script selection, owner-aware activation, summoner attachment, registration, and post-registration restoration hydration. Scoped `FamiliarRestorationState` holds persisted familiar data between character hydration and familiar composition. `Character` exposes only the identity-bearing `AttachFamiliar` and `DetachFamiliar` state transitions; it does not depend on NPC activation, summoning definitions, or pending restoration state.

   Familiar attachment is provisional until registration succeeds. `NpcService` reports a rejected store add as a registration failure, destroys the NPC through the common lifecycle, and removes a store entry again if `OnRegistered()` fails before destroying that NPC, preserving the original exception even when rollback logging is required. This registration-boundary cleanup also covers direct `Build()` callers such as `MapRegionLoader`; `NpcBuilder` does not return a handle for any failed registration, and `FamiliarFactory` detaches the same familiar on registration or restoration failure. This keeps normal summoning from treating a rejected spawn as successful and prevents failed restoration from leaving active character state behind.

8. **Use a startup-loaded summoning definition store for synchronous familiar registration.** `FamiliarCharacterScript.OnRegistered` remains synchronous, so it reads the familiar definition from a singleton store populated by the existing startup service executor. The general `ISummoningService` remains asynchronous for skill operations; blocking an EF query from a character callback is not an acceptable lookup path.

9. **Keep familiar teardown in the familiar lifecycle.** `NpcService` remains generic and only manages NPC registration, destruction, and storage. `FamiliarScriptBase` owns the character event registrations it creates, removes them during `OnDestroy`, then asks its summoner to detach that specific NPC. `Character.DetachFamiliar(INpc)` clears state only when that NPC is still the active familiar. This prevents stale handlers and delayed unregisters of an older familiar from affecting a newer one.

10. **Use the familiar factory for both summoning and restoration.** The summoning skill service delegates familiar creation to the scoped `IFamiliarFactory`, while `FamiliarCharacterScript` delegates restoration to the same factory during character registration. This keeps all familiar composition in one application boundary and keeps the factory's restoration state scoped to one character.

## Risks / Trade-offs

- [Risk] Runtime arguments could be bound incorrectly when several arguments share `ILocation` or are null. → Resolve typed services in `NpcBuilder` and call the constructor directly; validate the ordinary spawn path with omitted optional values.
- [Risk] Moving component construction may change initialization order. → Set location before components that read it, construct the owned components, activate and attach the script before registration, let registration and `OnSpawn` complete, then apply persisted familiar state; keep `OnCreate` as a separate registration callback.
- [Risk] A familiar may be created successfully but fail during summoner attachment, registration, or post-registration restoration. → Keep `AttachToSummoner` immediately after owner-aware script activation, apply persisted state only after registration, make rejected registration fail explicitly, unregister a handle when post-registration restoration fails, and detach the identity-bearing familiar association on every failed path.
- [Risk] Remaining provider matches may be mistaken for NPC-domain lookups. → Run a final scan limited to the NPC composition graph and document the two allowed boundaries: `NpcBuilder` composition and the shared `CreatureCombat` constructor outside this change.

## Migration Plan

1. Add the explicit NPC dependency capability and update the script activation contract.
2. Change `NpcBuilder`, `Npc`, `NpcHandle`, and NPC-owned components to use typed services from the child scope.
3. Migrate common, familiar, player-NPC, and directly affected concrete NPC script paths, then update direct callers and focused tests.
4. Run focused GameWorld and Game.Scripts tests, a solution build, strict OpenSpec validation, and the final provider scan.
5. Rollback is a source revert. No persisted data or deployment migration is required.

## Open Questions

None.
