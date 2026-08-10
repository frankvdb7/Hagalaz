## Context

Entities currently expose a deprecated `Region` property. The property hides lookup of the singleton `IMapRegionService` inside `Creature`, `GameObject`, and `GroundItem`, while most newer code already resolves the service directly. Removing the property affects the abstractions project, GameWorld models, scripts, commands, and tests.

## Goals / Non-Goals

**Goals:**

- Make `IMapRegionService` the only region lookup mechanism exposed to callers.
- Preserve the existing region lookup, add/remove, update, and ground-item despawn behavior.
- Keep dependency ownership explicit at existing DI and character service boundaries.

**Non-Goals:**

- Changing `IMapRegionService` methods or map-region lifecycle ownership.
- Introducing a general entity-to-region adapter, helper framework, or replacement property.
- Migrating unrelated service-locator usage or unrelated `Region` properties.

## Decisions

- **Remove the contract and concrete convenience properties.** `IEntity.Region`, `Creature.Region`, `GameObject.Region`, and `GroundItem.Region` are deleted. This is a deliberate breaking change; retaining a compatibility wrapper would preserve the implicit lookup this change is intended to eliminate.
- **Reuse the existing map service API.** Callers resolve the region with `GetOrCreateMapRegion(location.RegionId, location.Dimension, false)`, preserving the legacy property's behavior. No new service method is added.
- **Use existing character service boundaries for character-bound operations.** Commands, skills, combat, and inventory code resolve `IMapRegionService` from `ICharacter.ServiceProvider`, matching the existing command and script patterns.
- **Use constructor injection for standalone object scripts.** Scripts that operate on `Owner` without a character context receive `IMapRegionService` through their existing DI constructors. Adding a shared service-locator-backed property to `GameObjectScript` was rejected because it would create another implicit access mechanism.
- **Inject the service into `GroundItem`.** `GroundItemBuilder` supplies the existing service to the model, and `Clone` carries the same dependency forward. `IGroundItem.Despawn()` remains the public operation and removes the item from the region selected by its location.

## Risks / Trade-offs

- [Risk] Constructor signatures for concrete scripts and `GroundItem` change. → All affected instances are created through existing DI/builders or are updated in focused test helpers; solution compilation verifies remaining callers.
- [Risk] A missed `.Region` caller could break a less frequently built project. → Search the full C# source/test tree and run both affected test projects plus the solution build.
- [Risk] Region lookup semantics could accidentally change during replacement. → Keep the exact existing `GetOrCreateMapRegion` arguments and add a direct despawn regression test.

## Migration Plan

This is an in-repository source migration with no persisted data or deployment sequencing. Apply the contract removal and caller updates together, run focused tests and the solution build, then archive the OpenSpec change after validation. Rollback is a source revert if required.
