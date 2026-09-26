## ADDED Requirements

### Requirement: World entities receive resolvable identity

When a `GroundItem` or `GameObject` is added to a map region and becomes
observable, the map-region ownership boundary MUST register that exact entity
with `IEntityStore` before publication completes.

#### Scenario: Runtime ground item registration

- **WHEN** a runtime ground item is added to a map region
- **THEN** it has a non-default `EntityHandle`
- **AND** the handle resolves to that exact item through `IEntityStore`

#### Scenario: Loaded resource registration

- **WHEN** `MapRegionLoader` applies a loaded ground item or game object
- **THEN** the exact resource has a non-default handle
- **AND** the handle resolves through the same `IEntityStore`

### Requirement: Permanent removal invalidates identity

When a ground item or game object permanently leaves map-region ownership, its
exact entry MUST be removed from `IEntityStore`.

#### Scenario: Stale handle after removal

- **WHEN** an entity is permanently removed
- **THEN** its previous handle does not resolve
- **AND** a later entity reusing the slot cannot be resolved by that stale
  handle

#### Scenario: Region destruction

- **WHEN** a region is permanently destroyed
- **THEN** its active ground items and game objects no longer resolve

### Requirement: Disabled static identity is retained until destruction

Disabling a static game object MUST retain its region ownership and handle so
re-enabling the same object preserves identity. Permanent region destruction
MUST unregister disabled static objects.

#### Scenario: Disable and re-enable

- **WHEN** a static game object is disabled and the same instance is re-added
- **THEN** its handle is unchanged
- **AND** the handle resolves to that instance while it is active again

#### Scenario: Disabled object during region destruction

- **WHEN** a static game object is disabled and its region is destroyed
- **THEN** its handle no longer resolves
