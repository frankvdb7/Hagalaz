## ADDED Requirements

### Requirement: Projectile traversal consumes the LOS collision layer
The system SHALL evaluate tile-to-tile projectile LOS from `ObjectBlock` and the middle directional `Blocked*` flags emitted by the existing collision writers. It SHALL NOT treat the high `ObjectAllowRange` or `WallAllowRange*` routing flags as LOS blockers.

#### Scenario: Full LOS object blocks regardless of routing state
- **WHEN** a standard object emits `ObjectBlock`, with or without `ObjectAllowRange`
- **THEN** projectile LOS through that tile SHALL be unsuccessful

#### Scenario: High routing object alone does not block LOS
- **WHEN** a standard object emits `ObjectAllowRange` without `ObjectBlock`
- **THEN** projectile LOS through that tile SHALL remain successful

#### Scenario: Gateway wall blocks through its middle layer
- **WHEN** a solid gateway wall emits the matching cardinal `Blocked*` flag but omits `WallAllowRange*`
- **THEN** projectile LOS across that wall from either side SHALL be unsuccessful

#### Scenario: Movement-only state does not block LOS
- **WHEN** a ray encounters only `FloorBlock`, `FloorDecorationBlock`, or a high routing wall flag
- **THEN** that state SHALL not by itself make the projectile LOS unsuccessful

#### Scenario: Diagonal wall blocks the entering side
- **WHEN** an exact 45-degree ray enters a tile with the middle diagonal `Blocked*` flag emitted for the side it enters from
- **THEN** projectile LOS SHALL be unsuccessful

### Requirement: Projectile traversal follows the fixed-point ray
The system SHALL trace the supplied source and target tiles with 16.16 fixed-point slope arithmetic, inspecting each X and Y tile boundary the ray crosses rather than walking by the sign of the remaining delta. Exact 45-degree rays SHALL also inspect the matching diagonal wall boundary.

#### Scenario: Asymmetric ray records its crossed tiles
- **WHEN** a clear ray travels from `(x, y)` to `(x + 5, y + 2)`
- **THEN** its trace SHALL follow the fixed-point boundary crossings rather than the diagonal staircase `(x + 1, y + 1)`, `(x + 2, y + 2)`, and so on

#### Scenario: Crossed X or Y boundary blocks the ray
- **WHEN** a matching cardinal middle LOS flag is present on any tile boundary crossed by an asymmetric ray
- **THEN** the projectile LOS SHALL be unsuccessful

#### Scenario: Southwest path records its destination
- **WHEN** a clear ray travels one tile southwest
- **THEN** the successful path SHALL contain only the southwest destination tile, not collision-probe tiles

#### Scenario: Different planes have no line of sight
- **WHEN** the source and target are on different planes
- **THEN** the projectile LOS SHALL be unsuccessful

### Requirement: Ranged combat consumes projectile line-of-sight
The system SHALL use the existing `IProjectilePathFinder` result for `CreatureCombat` targets within an attack range greater than one, without maintaining a second collision evaluator.

#### Scenario: Ranged target check delegates to projectile pathfinding
- **WHEN** a combat target is within a ranged attack distance
- **THEN** the target reach decision SHALL consume the configured projectile pathfinder result
