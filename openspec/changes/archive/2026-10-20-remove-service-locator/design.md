## Context

The remaining global locator consumers are concentrated in `AreaScript` and `SpellBookTab`. GameWorld already constructs area and widget scripts through the DI container, and `IProjectileBuilder` and `IMagicService` are already registered or injected through those existing paths. The locator project contains no reusable behavior beyond the obsolete global lookup wrapper.

## Goals / Non-Goals

**Goals:**

- Make the required area and spellbook dependencies explicit at their existing construction boundaries.
- Preserve area respawn coordinates, spellbook event registration, and ancient-spell behavior.
- Remove the global provider setup, implementation, stale references, and now-unused project dependencies.
- Keep the migration narrow and verifiable through focused tests, build validation, and repository search.

**Non-Goals:**

- Do not migrate unrelated `IServiceProvider` usage already used at composition or character-owned activation boundaries.
- Do not add a generic resolver, script context, factory framework, retry path, or new service lifetime.
- Do not call `LoadScriptedCombatSpells` from a constructor, `OnOpen`, refresh method, or any new lifecycle hook.

## Decisions

- **Inject options into the `AreaScript` base constructor.** `IOptions<WorldOptions>` is a real required dependency of area initialization, so the base class owns the option-backed respawn calculation. All DI-created concrete area scripts pass the dependency to the base constructor. Changing `IAreaScript.Initialize(IArea)` was rejected because it would widen the public contract and move construction dependencies into every caller.

- **Inject `IProjectileBuilder` into `SpellBookTab`.** The five ancient-spell helper methods become instance methods using the injected builder. The existing `_magicService` field is used directly where the class already has that dependency. Injecting `IServiceProvider` for these paths was rejected because it would preserve the service-locator pattern under a different name.

- **Delete the uncalled `LoadScriptedCombatSpells` method.** Repository search shows no caller, so removing this private dead path is safer than making it part of `SpellBookTab` initialization. This prevents duplicate dictionary population, recursive lifecycle calls, and any new per-open initialization loop.

- **Keep `SpellBookTab` lifecycle unchanged.** The static constructor continues to call only `LoadTeleports`; `OnOpen` remains the handler-registration entry point; refresh methods do not call `OnOpen`; ancient-spell helpers return their existing fixed four-element arrays.

- **Remove the locator project and preserve direct DI package ownership.** Delete the locator project and its references from `Hagalaz.Game.Common` and `Hagalaz.Game`, add the existing Microsoft DI abstractions package directly to `Hagalaz.Game.Common` for its `GetRequiredService` extension calls, and regenerate affected lock files. No new third-party dependency is introduced.

## Risks / Trade-offs

- [Risk] Constructor signatures for area scripts and `SpellBookTab` change. → All current registrations use DI; focused activation/build checks and the solution build catch missed construction paths.
- [Risk] Static spell data could be initialized repeatedly if lifecycle code is changed accidentally. → Do not add a call to `LoadScriptedCombatSpells`; preserve the existing static teleport initialization and `OnOpen` boundary.
- [Risk] Removing the project could expose an implicit DI package dependency. → Add the already-used Microsoft DI abstractions package directly to `Hagalaz.Game.Common` and verify restore/build.

## Migration Plan

Apply the source and project-reference changes together, update the OpenSpec task checklist as each task completes, restore lock files, run focused tests and the solution build, validate the change strictly, and confirm the final repository search is clean. Rollback is a source revert; no persisted data or deployment migration is required.
