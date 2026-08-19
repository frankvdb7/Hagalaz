## Context

`CharacterFactory` already creates the nested `IServiceScope` whose lifetime ends in `Creature.Destroy`. `Character` currently passes that scope to `Creature`, then manually creates its component graph while each component resolves extra services through the owner's provider. The refactor must keep the nested scope and the existing manually ordered initialization, because rendering information, containers, appearance, and combat have observable construction order dependencies.

The covered graph is `Character`, its partial methods, the components it constructs, the farming patch task it creates, and the explicit constructor inputs needed by character `Movement` and `CharacterCombat`. General `Creature` and `Npc` composition remains outside this change.

## Goals / Non-Goals

**Goals:**

- Make ordinary character dependencies visible in the constructors that own them.
- Resolve the graph from the nested character scope at `CharacterFactory`, not from ordinary domain methods.
- Preserve construction order, object identity, scoped lifetimes, runtime script behavior, and character gameplay behavior.
- Make focused component tests pass direct substitutes without mocking `ICharacter.ServiceProvider` for normal dependencies.

**Non-Goals:**

- Remove `IServiceProvider` from the entire creature hierarchy.
- Replace all runtime type activation across GameWorld.
- Introduce a generic resolver, dependency bag, second character scope, or new retry/lifecycle mechanism.
- Change public gameplay, persistence, protocol, or database behavior.

## Decisions

1. **Use `ActivatorUtilities` only in `CharacterFactory`.** The factory creates the nested scope and calls `ActivatorUtilities.CreateInstance<Character>` with the scope, session, and client as explicit arguments. The container supplies the remaining typed constructor dependencies from that scope. A manually expanded factory constructor was rejected because it would duplicate the character dependency graph and create a second composition list.

2. **Keep `IServiceScope` as a lifetime input, but never use it for ordinary lookup in `Character`.** `Creature` owns and disposes the scope, so `Character` must still pass it to the base constructor. The scope is not used as a service resolver by `Character` or its components. Removing it from the base path would require the unrelated creature lifetime refactor excluded by issue #408.

3. **Pass component dependencies directly and preserve the existing order.** Each constructor receives only the services it uses. `Character` passes those services when it calls `new` for containers and skills. A shared `CharacterDependencies` object was rejected because it would hide the same dependency graph behind a service bag.

4. **Use narrow activators for runtime-selected scripts.** Familiar scripts, character-NPC scripts, generic character scripts, and widget scripts selected through existing public operations receive separate typed activators. Scoped activators own the type-specific `GetRequiredService(Type)` operation inside the character scope; the stateless widget activator receives the owning character and resolves through that character's scope because `IWidgetBuilder` is an existing singleton. This keeps runtime selection where it belongs without exposing an arbitrary resolver to a domain component. The existing metadata providers continue to map IDs to types.

5. **Inject the dialogue implementation into `Slayer`.** `ISlayerTaskCompletedDialogue` is a fixed skill-specific dependency, not a runtime-selected type. Injecting it directly avoids a normal service lookup on task completion and keeps its existing scoped activation behavior.

6. **Update shared constructors only where character construction needs them.** `Movement` and `CreatureCombat` gain explicit constructor paths for path finding, mediator, combat options, and hit-splat creation. Existing NPC construction is preserved through its current path so this change does not become a general NPC refactor. Character combat uses the explicit path and no longer reaches through a character owner for these services.

7. **Do not add optional constructor parameters.** Every new dependency is required and appears as a required constructor argument. Existing public method optionality is not expanded by this change.

## Risks / Trade-offs

- [Risk] Constructor changes can leave hidden direct callers or tests behind. → Search all production and test call sites after each component group, then run focused tests and a solution build.
- [Risk] A typed activator could accidentally resolve from the application root. → Register activators in the character scope and construct them from the nested provider; add a factory activation test that checks scoped identity.
- [Risk] Initialization order could change while wiring many components. → Keep `Character`'s current order and preserve the comment that render information must exist before appearance hydration.
- [Risk] Existing NPC paths still contain service-provider lookups. → Keep those paths unchanged and scope the final scan to the character composition graph covered by this change.

## Migration Plan

1. Add the OpenSpec requirements and narrow activator contracts.
2. Add scoped activator registrations and change `CharacterFactory` to compose `Character` from the nested scope.
3. Migrate character partials and owned components, including character-created movement/combat and farming patch tasks.
4. Update focused GameWorld and Game.Scripts tests and add coverage for direct dependency construction and nested scope lifetime.
5. Run the focused test projects, solution build, strict OpenSpec validation, and final character-graph scan. Rollback is a source revert; no persisted data migration is needed.

## Open Questions

None. The issue defines the scope, the composition boundary, and the required runtime activation behavior.
