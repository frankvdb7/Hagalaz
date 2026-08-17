## Context

`Creature` currently owns a `Dictionary<Type, IState>`, calls every state's lifecycle methods, ticks every state, expires entries with `TicksLeft <= 0`, and applies one global longest-duration replacement rule. `IState` therefore couples unrelated passive, timed, callback, and persistence concerns. Character hydration/dehydration also resolves a registered implementation type through `IServiceProvider` and reflects a metadata attribute from every runtime state.

The game loop is synchronous and serialized per creature. The existing typed creature API, DI scanning, startup service, and character hydration/dehydration pipeline are established boundaries that must remain usable.

## Goals / Non-Goals

**Goals:**

- Keep `IState` as a small marker and model timing, removal callbacks, and persistence as opt-in capabilities. Defer custom ticking and add callbacks until a production state needs them.
- Give each creature one state collection as the owner of storage, add/remove transitions, duplicate policy, ticking, and expiry.
- Preserve typed state queries and existing synchronous game-loop behavior without per-tick reflection or new allocations beyond the existing pooled snapshot approach.
- Make persistent identity/activation a narrow registry boundary and make unknown or runtime-only persisted records harmless.
- Prove the design with focused lifecycle and representative gameplay regressions.

**Non-Goals:**

- A mass rename or folder reorganization of the state catalog.
- A new event bus, ECS/component framework, workflow engine, worker, queue, database schema, or package.
- Moving every equipment, prayer, agility, minigame, or progression concept into a new domain model in this change.

## Decisions

1. **Capability interfaces over a larger base class.** `IState` is empty. `ITimedState` owns `TicksLeft`, `IStateLifecycle` owns the removal callback currently used by production states, and `IPersistentState` opts into character persistence. `State` is only an optional empty convenience base implementation of `IState`; a separate `TimedState : ITimedState` convenience base supplies the duration property without inheriting from `State`. `IKeepLongestDurationState` is the only reapplication opt-in; all other duplicate applications keep the active instance. Resting uses one concrete `RestingState` with an optional removal callback; no derived orb-specific resting state is used because collection identity is exact-type.

2. **A per-creature collection owns transitions.** The concrete internal `CreatureStateCollection` is constructed by and owned by one `Creature`; it is not registered globally or exposed through a public abstraction. `Creature` forwards its existing public methods to the collection. This keeps state ownership local, avoids a second global source of truth, and gives add/remove/tick processing one place to enforce removal callbacks.

3. **Reapplication stays minimal and compositional.** The collection defaults every state to keep-existing. `IKeepLongestDurationState : ITimedState` is the only opt-in capability and causes a longer reapplication to replace the active timed instance; passive states and timed states without that capability are not compared or replaced. A general policy enum, replace mode, and strategy registry were rejected because no production mechanic currently needs them.

4. **Timing is processed by capability checks.** The collection snapshots active states with the existing `ArrayPool` technique, decrements `ITimedState` only when implemented, and removes expired timed states by object identity so a state removed during processing cannot remove a newer instance. Passive states are not touched by game-tick processing. Custom ticking is deferred until a production state requires it.

5. **Removal callbacks are optional and immediate.** `IStateLifecycle.OnRemoved` is invoked only after an active state is explicitly removed or expires. A rejected duplicate invokes no removal callback; a longer keep-longest reapplication removes the old instance once. Add callbacks are not part of the current contract because no production state uses them.

6. **The registry separates identity from scoped activation.** Singleton `StateProvider` owns the stable ID-to-type mapping and reverse identity lookup. Scoped `StateService` obtains the registered type from that mapping and resolves the state through its own character `IServiceProvider`, preserving constructor injection for scoped dependencies. Gameplay and persistence callers still use `TryCreateState(id, out state)` and `TryGetStateId(state, out id)` without receiving raw implementation types. Duplicate IDs fail startup with an `InvalidOperationException`; unknown IDs return `false` and are skipped by hydration.

7. **Persistence is opt-in and identity remains metadata-backed.** Only `IPersistentState` instances are dehydrated, and the metadata factory discovers only those types. The audited durable set is `DefaultSkulledState`, `HasGodWarsHoleRopeState`, `HasSaradominFirstRockRopeState`, `HasSaradominLastRockRopeState`, and `LodestoneActivatedState`; equipment, prayer, combat/session, activity, and NPC-derived states remain runtime-only. Every persistent state registration must expose an implementation type with `StateMetaDataAttribute`; startup fails with a diagnostic when the stable identity is missing. Timed persistent states retain `TicksLeft`; non-timed persistent states use the existing DTO's zero duration. Equipment remains the authoritative source.

## Risks / Trade-offs

- [Risk] Existing persisted rows for runtime-only states will no longer be restored. → This is intentional compatibility behavior for stale runtime records; hydration skips them without failing login, while explicitly persistent states retain their IDs.
- [Risk] States that currently rely on an implicit duration but were not migrated to `TimedState` would stop expiring. → Compile-time failures from remaining `TicksLeft` assignments and focused searches for duration initializers identify the migration set; representative timed mechanics are covered by tests.
- [Risk] Changing lifecycle timing could expose callers that depended on duplicate callbacks. → The old callbacks were not safe to replay; tests assert exactly-once semantics and the state collection handles transitions centrally.
- [Risk] A registration failure can prevent startup when a duplicate ID exists. → This is the intended fail-fast behavior for persistent identity configuration and provides the duplicate ID in the exception/log diagnostic.

## Migration Plan

1. Add capability contracts and the creature-owned collection while keeping typed `Creature` forwarding methods.
2. Migrate current timed state classes and remove `int.MaxValue` from the representative until-removed call sites.
3. Update registry activation and character persistence to use capability checks and safe `Try*` operations.
4. Run focused state tests, then build the affected projects and run the broader GameWorld test project.
5. Rollback is a source-level revert; no database migration is required because the existing state DTO and state table shape remain unchanged.

## Open Questions

- The remaining state catalog should be classified and migrated in follow-up vertical slices; this change deliberately does not infer ownership for every one-off state from its filename.
