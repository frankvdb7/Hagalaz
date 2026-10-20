## Why

`IEntity.Region` is a deprecated convenience property that makes entity models resolve map-region infrastructure implicitly. Removing it completes the migration to the existing `IMapRegionService`, gives region lookup one owner, and prevents new callers from depending on the legacy contract.

## What Changes

- **BREAKING**: Remove `IEntity.Region` and the corresponding concrete properties from creatures, game objects, and ground items.
- Migrate every production and test caller to `IMapRegionService`, preserving the current `GetOrCreateMapRegion(location.RegionId, location.Dimension, false)` semantics.
- Inject `IMapRegionService` into `GroundItem` through `GroundItemBuilder` so `IGroundItem.Despawn()` retains its behavior without restoring an entity region property.
- Supply the service to standalone game-object scripts through their existing dependency-injection constructors; character-bound code uses the existing character service provider.
- Do not change `IMapRegionService`, map-region lifecycle ownership, or unrelated properties named `Region`.

## Capabilities

### New Capabilities

- `entity-map-region-access`: Defines the authoritative service-based map-region access contract for entities and their callers.

### Modified Capabilities

- None.

## Impact

- Affected contracts: `IEntity` and inherited entity interfaces.
- Affected implementations: `Creature`, `GameObject`, `GroundItem`, and region-using scripts/services.
- Affected tests: entity test doubles, command tests, and ground-item lifecycle tests.
- No new packages, workers, queues, persistence, or service APIs.

## Acceptance Criteria

- The solution contains no `IEntity.Region` member or concrete entity `.Region` compatibility property.
- All former region operations resolve through `IMapRegionService` and preserve existing lookup semantics.
- `IGroundItem.Despawn()` removes the item from the map region determined by its location.
- Focused tests and the solution build pass.

## Non-Goals and Stop Conditions

- Do not redesign `IMapRegionService` or map-region lifecycle behavior.
- Do not migrate unrelated service-locator usage or unrelated `Region` properties.
- Stop and record follow-up work if the change requires a second region owner, a new persistence mechanism, or behavior changes beyond the legacy property removal.
