## 1. Remove the legacy entity contract

- [x] 1.1 Remove `IEntity.Region` and the concrete `Region` properties from `Creature`, `GameObject`, and `GroundItem`.
- [x] 1.2 Inject `IMapRegionService` into `GroundItem` through `GroundItemBuilder` and preserve `IGroundItem.Despawn()` and clone behavior.

## 2. Migrate production callers

- [x] 2.1 Replace character-bound `.Region` operations in commands, skills, combat, inventory, thieving, cannon, and mining with `IMapRegionService` lookups.
- [x] 2.2 Inject `IMapRegionService` into standalone object scripts that operate on their owner and migrate their region operations.

## 3. Update regression coverage

- [x] 3.1 Update test doubles and command tests to configure `IMapRegionService` instead of `IEntity.Region`.
- [x] 3.2 Add or update ground-item tests covering service-based `Despawn()` removal.

## 4. Validate the migration

- [x] 4.1 Verify no C# production or test caller references the removed entity/concrete `Region` property, then run both focused test projects and the solution build.
