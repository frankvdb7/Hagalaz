## ADDED Requirements

### Requirement: Entity region access uses the map-region service

Consumers needing an entity's map region MUST resolve it through `IMapRegionService` using the entity location. Entity contracts MUST NOT expose a region property as an alternate lookup path.

#### Scenario: Region is resolved from an entity location

- **WHEN** a consumer needs the region for an entity at a location
- **THEN** it resolves the region through `IMapRegionService` using that location's region ID and dimension

#### Scenario: Entity contract is consumed

- **WHEN** code consumes an `IEntity`, `ICreature`, `IGameObject`, or `IGroundItem`
- **THEN** no region property is required or available on the entity contract

### Requirement: Existing region operation semantics are preserved

Region add, remove, update, collision, and lookup operations MUST target the same region selected by the legacy entity property for the entity's current location.

#### Scenario: Entity region operation is migrated

- **WHEN** a former entity-region operation is performed
- **THEN** the operation targets the region returned for the entity's current region ID and dimension with the existing create/resume behavior

### Requirement: Ground-item despawn removes the located item

`IGroundItem.Despawn()` MUST remove the ground item from the map region identified by its location and MUST retain its existing success result and removal behavior.

#### Scenario: Ground item despawns

- **WHEN** `Despawn()` is called for a ground item
- **THEN** the item is removed from the region identified by its location and the operation returns success
