## Context

`Creature` currently owns a `Dictionary<Type, IState>`, calls every state's lifecycle methods, ticks every state, expires entries with `TicksLeft <= 0`, and applies one global longest-duration replacement rule. `IState` therefore couples unrelated passive, timed, callback, and persistence concerns. Character hydration/dehydration also resolves a registered implementation type through `IServiceProvider` and reflects a metadata attribute from every runtime state.

The game loop is synchronous and serialized per creature. The existing typed creature API, DI scanning, startup service, and character hydration/dehydration pipeline are established boundaries that must remain usable.

## Goals / Non-Goals

**Goals:**

- Keep `IState` as a small marker and model timing, custom ticking, lifecycle callbacks, and persistence as opt-in capabilities.
- Give each creature one state collection as the owner of storage, add/remove transitions, duplicate policy, ticking, and expiry.
- Preserve typed state queries and existing synchronous game-loop behavior without per-tick reflection or new allocations beyond the existing pooled snapshot approach.
- Make persistent identity/activation a narrow registry boundary and make unknown or runtime-only persisted records harmless.
- Prove the design with focused lifecycle and representative gameplay regressions.

**Non-Goals:**

- A mass rename or folder reorganization of the state catalog.
- A new event bus, ECS/component framework, workflow engine, worker, queue, database schema, or package.
- Moving every equipment, prayer, agility, minigame, or progression concept into a new domain model in this change.

## Decisions

1. **Capability interfaces over a larger base class.** `IState` is empty. `ITimedState` owns `TicksLeft`, `ITickableState` owns custom `Tick`, `IStateLifecycle` owns add/remove callbacks, and `IPersistentState` opts into character persistence. A non-timed state can remain the existing `State` base class and is therefore until-removed by construction. A separate `TimedState` convenience base supplies duration only; reapplication policy remains an independent capability so a timed state can explicitly choose replace or keep-existing behavior.

2. **A per-creature collection owns transitions.** `CreatureStateCollection` is constructed by and owned by one `Creature`; it is not registered globally. `Creature` forwards its existing public methods to the collection. This keeps state ownership local, avoids a second global source of truth, and gives add/remove/tick processing one place to enforce exactly-once lifecycle callbacks.

3. **Policy is state-declared, not inherited from lifetime.** `IStateReapplicationPolicy` exposes `KeepExisting`, `Replace`, and `KeepLongestDuration`. The collection uses an explicit policy when a state implements the capability; otherwise timed states default to `KeepLongestDuration` and non-timed states default to `KeepExisting`. The collection does not compare `TicksLeft` for passive states. A generic strategy registry was rejected because there is only one current container and no demonstrated external policy provider.

4. **Timing is processed by capability checks.** The collection snapshots active states with the existing `ArrayPool` technique. It invokes `ITickableState` only when implemented, decrements `ITimedState` only when implemented, and removes expired timed states by object identity so a state replaced during its tick cannot remove its replacement. Passive states are not touched by game-tick processing.

5. **Lifecycle callbacks are optional and immediate.** Add and remove callbacks are invoked through `IStateLifecycle` only. A real add invokes `OnAdded` once; a rejected duplicate invokes neither callback; replacement invokes one removal for the old instance and one add for the new instance; explicit removal and expiry both invoke one removal. The old `OnRegistered` replay is removed so hydration/equipment states cannot receive duplicate add notifications.

6. **The registry exposes operations, not implementation types.** `IStateProvider` exposes `TryCreateState(id, out state)` and `TryGetStateId(state, out id)`. `StateProvider` may use DI and implementation types internally at the composition boundary, but callers cannot request a raw `Type` or resolve arbitrary state implementations. Duplicate IDs fail startup with an `InvalidOperationException`; unknown IDs return `false` and are skipped by hydration.

7. **Persistence is opt-in and identity remains metadata-backed.** Only `IPersistentState` instances are dehydrated, and the metadata factory discovers only those types. The audited durable set is `DefaultSkulledState`, `HasGodWarsHoleRopeState`, `HasSaradominFirstRockRopeState`, `HasSaradominLastRockRopeState`, and `LodestoneActivatedState`; equipment, prayer, combat/session, activity, and NPC-derived states remain runtime-only. Missing identity causes a persistent instance to be omitted rather than crashing generic save. Timed persistent states retain `TicksLeft`; until-removed persistent states use the existing DTO's zero duration. Equipment remains the authoritative source.

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
