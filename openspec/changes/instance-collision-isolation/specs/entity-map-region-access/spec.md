# Spec Delta

## ADDED Requirements

### Requirement: Collision is read from the source's world

Collision reads for movement, interaction and line of sight SHALL use the world of the location they start from, never the global world by default.

#### Scenario: Instance and global collision disagree

- **GIVEN** the same coordinates are blocked in the global world and open in an encounter instance
- **WHEN** a creature in the instance is checked, routed or shot at across those coordinates
- **THEN** the instance's collision decides, and a creature in the global world still sees the global collision

#### Scenario: Route crosses a region boundary

- **WHEN** a path from an instance crosses a 64x64 region boundary
- **THEN** every tile is read from the instance's region for that tile

#### Scenario: Region is missing

- **WHEN** the world or region of a tile is missing, not ready, removed or out of range
- **THEN** the tile is blocked and no region is created or resumed

#### Scenario: Different worlds

- **WHEN** a path or projectile check joins entities, or an entity and an object, in different worlds
- **THEN** it fails instead of matching their overlapping coordinates
