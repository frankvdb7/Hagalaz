## Purpose

Ensure static map-cache geometry and its server-side collision representation use the same local coordinates and map planes.

## ADDED Requirements

### Requirement: Static cache objects retain region coordinates

The system MUST place each static cache object at its decoded local X and Y within the complete 64x64 region, without reducing the coordinates to an 8x8 chunk.

#### Scenario: Object is in a later 8x8 chunk

- **WHEN** a static cache object is decoded at local coordinates outside the first 8x8 chunk
- **THEN** the object and its collision flags use those complete local region coordinates

#### Scenario: Object is on a non-zero plane

- **WHEN** a static cache object is decoded on plane 1, 2, or 3
- **THEN** the object and its collision flags are loaded on that effective plane

### Requirement: Terrain collision remains complete

The system MUST apply impassable terrain callbacks for every decoded plane in a complete region.

#### Scenario: Impassable terrain exists on multiple planes

- **WHEN** terrain flags identify impassable tiles on more than one plane
- **THEN** the collision callback is invoked for each effective flagged tile
