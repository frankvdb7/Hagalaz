## ADDED Requirements

### Requirement: Projectile traversal honors emitted projectile collision
The system SHALL determine every `ProjectilePathFinder` step from the existing directional traversal mask for its destination tile. A tile whose matching mask includes a floor, floor-decoration, or high directional wall bit SHALL stop projectile traversal, even when `FloorBlock` is not present. A standard object whose only matching collision bit is `ObjectAllowRange` and whose full collision state also contains `ObjectBlock` SHALL remain traversable.

#### Scenario: Cardinal wall collision blocks line-of-sight
- **WHEN** a cardinal step reaches a tile containing the matching high directional wall bit
- **THEN** the projectile path SHALL be unsuccessful

#### Scenario: Diagonal wall collision blocks line-of-sight
- **WHEN** a diagonal step reaches a tile containing any matching diagonal or component cardinal high directional wall bit
- **THEN** the projectile path SHALL be unsuccessful

#### Scenario: High-object-only standard object blocks line-of-sight
- **WHEN** a standard object has emitted `ObjectAllowRange` without `ObjectBlock`
- **THEN** projectile traversal through that tile SHALL be unsuccessful

#### Scenario: Range-permissive standard object remains traversable
- **WHEN** a standard object has emitted `ObjectBlock | ObjectAllowRange` and no other matching blocker
- **THEN** projectile traversal through that tile SHALL remain successful

#### Scenario: Floor collision overrides a range-permissive object combination
- **WHEN** a tile contains `FloorBlock` or `FloorDecorationBlock` together with `ObjectBlock | ObjectAllowRange`
- **THEN** projectile traversal through that tile SHALL be unsuccessful

### Requirement: Projectile direction is spatially consistent
The system SHALL append the coordinate it validated for each projectile step.

#### Scenario: Southwest traversal reaches its validated tile
- **WHEN** a clear projectile path moves southwest by one tile
- **THEN** its next waypoint SHALL be `(x - 1, y - 1)`

### Requirement: Ranged combat consumes projectile line-of-sight
The system SHALL use the existing `IProjectilePathFinder` result for `CreatureCombat` targets within an attack range greater than one, without maintaining a second collision evaluator.

#### Scenario: Ranged target check delegates to projectile pathfinding
- **WHEN** a combat target is within a ranged attack distance
- **THEN** the target reach decision SHALL consume the configured projectile pathfinder result
