# Spec Delta

## Purpose

Defines how an 8x8 part of a dynamic region maps to a source part and how the server materializes objects and collision for it, so server state matches the map the client draws.

## ADDED Requirements

### Requirement: Dynamic creation maps parts from the source region

When a dynamic region is created from a source region, each destination local part (x, y, plane) SHALL map to the source region's part at the same local index, identified by absolute source part coordinates. Local part indices `0..7` and absolute part coordinates SHALL NOT be interchangeable.

#### Scenario: Source region with different region X and Y

- **GIVEN** a source region whose region X differs from its region Y and lies away from the world origin
- **WHEN** a dynamic region is created from it
- **THEN** destination local part (x, y, z) maps to source part (sourceRegionX * 8 + x, sourceRegionY * 8 + y, z) for every x, y in `0..7` and z in `0..3`

#### Scenario: Rotating or deleting by local index

- **GIVEN** a dynamic destination region whose region X differs from its region Y
- **WHEN** local part (x, y, z) is rotated or deleted
- **THEN** exactly the part at absolute (destRegionX * 8 + x, destRegionY * 8 + y, z) changes and no other part is created or changed

### Requirement: Part presence is explicit

A dynamic part SHALL be either present, with a source part X, Y, plane and rotation, or absent. Absence SHALL NOT be inferred from the descriptor value.

#### Scenario: Deleted part is absent

- **WHEN** a dynamic part is deleted
- **THEN** it is absent, it holds no objects, and loading it performs no template lookup

#### Scenario: Descriptor zero is present

- **WHEN** a part is mapped to source part (0, 0), plane 0, rotation 0
- **THEN** it is present and materializes that source part

### Requirement: Parts materialize from the source template

A present part's objects and floor blocks SHALL come from the source region's map template: cache object placements, configured object spawns and blocked floor tiles. Live source-region state and runtime entities SHALL NOT be copied, and the result SHALL NOT depend on whether the source region was loaded before.

#### Scenario: Source region never loaded

- **GIVEN** two identical dynamic regions, one created while the source region was resident and one while it was not
- **THEN** both have the same objects and collision

#### Scenario: Runtime state in the source is ignored

- **GIVEN** a resident source region with a removed static object, a runtime-spawned object, a ground item and an NPC in the copied part
- **WHEN** the part is materialized
- **THEN** the removed static object is present, and the runtime object, ground item and NPC are not copied

### Requirement: Materialization reads the mapped source area

A part SHALL read the 8x8 source tiles at the source part's local origin ((sourcePartX & 7) * 8, (sourcePartY & 7) * 8) on the source plane, and write to the destination part's tiles on the destination plane.

#### Scenario: Copy to a different local part

- **WHEN** source part (sx, sy) is mapped to a destination local part with a different local index
- **THEN** the part keeps source part (sx, sy) exactly and receives the objects and floor blocks of source part (sx, sy)

#### Scenario: Copy from another plane

- **WHEN** a destination part on plane 0 maps a source part on plane 2
- **THEN** it receives the plane-2 source content on plane 0

### Requirement: Bridge planes follow the client

A dynamic part SHALL select static cache objects and blocked tiles by their raw source plane, and SHALL place them one plane lower where the destination tile is a bridge according to the chunk drawn on destination plane 1. Blocked tiles below plane 0 are skipped; objects below plane 0 stay on their draw plane without collision unless an object with collision holds that slot. A self-mapped region SHALL equal its standard decoding.

#### Scenario: Copying a bridge chunk across planes

- **GIVEN** a cache source chunk with plane-1 bridge tiles and objects on raw plane 1
- **WHEN** destination planes 0 and 1 copy source plane 1 and plane 2 copies source plane 0, with rotation 0, 1, 2 and 3
- **THEN** the objects and floor blocks equal the client's dynamic chunk decoding of the same mapping

#### Scenario: Plane-0 object under a bridge

- **GIVEN** a static object on raw plane 0 whose destination tile is a bridge
- **WHEN** the part is materialized
- **THEN** the object exists on plane 0 without collision
- **AND** an object with collision in the same slot takes precedence over it

#### Scenario: Plane-1 chunk changes

- **WHEN** the chunk drawn on plane 1 of a column is written, rotated or deleted
- **THEN** objects and floor blocks of every plane in that column, and of every column that read its bridge flags, follow the new bridge flags

### Requirement: Objects follow the part rotation

For part rotation r in `0..3`, each template object whose origin lies in the source part SHALL be placed at the rotated position for its footprint, with rotation `(object rotation + r) & 3`, keeping its id, shape and layer.

#### Scenario: Four rotations of a mixed part

- **GIVEN** a source part with a wall, a wall decoration, a floor decoration, a 1x1 object and a 2x3 object
- **WHEN** it is materialized with rotation 0, 1, 2 and 3
- **THEN** each object's position, rotation, footprint size, shape and layer match the client's rotated chunk for that rotation

### Requirement: Collision matches the rotated part

Destination collision SHALL be computed from the rotated floor blocks and the placed objects with the same collision rules as standard regions, so floor, wall, object and range flags and multi-tile footprints match the rotated part.

#### Scenario: Directional flags rotate

- **WHEN** a part containing a floor block, a wall, a solid object, a range-blocking wall and a multi-tile object is materialized with rotation 0, 1, 2 and 3
- **THEN** destination collision equals the collision of a standard region containing the rotated floor block and objects

### Requirement: Rebuilding a part discards old content

Writing, rotating or deleting a part SHALL remove all objects of its previous content, including disabled static objects, and their collision, without recording them as disabled static objects or queuing runtime removal updates. Runtime object add and remove outside part rebuilds SHALL keep their existing behaviour.

#### Scenario: Runtime objects of a replaced chunk are removed

- **GIVEN** a runtime-added object and a runtime-disabled static object in a part, and an object another column placed in it
- **WHEN** the part is deleted or rewritten
- **THEN** the runtime objects are removed with their collision, and the other column's object stays

#### Scenario: A displaced object returns when its slot frees

- **GIVEN** a column whose object was replaced in its slot by an object another column placed there
- **WHEN** that other column is deleted or rewritten
- **THEN** the displaced object and its collision are restored

#### Scenario: Old static removal does not leak

- **GIVEN** a part in which a static object was removed at runtime
- **WHEN** the part is rewritten with another source part
- **THEN** a full update of the part sends no removal for the old object, and the old object is destroyed and unregistered

#### Scenario: Queued updates of old content are dropped

- **GIVEN** an update for an old object is queued or prepared, alongside an unrelated update in the same part
- **WHEN** the part is rewritten before the update is sent
- **THEN** the old object's update is not sent and the unrelated update still is

#### Scenario: Overlapping objects from other parts keep their collision

- **GIVEN** an object rooted in one part whose footprint shares a tile with an object in a neighbouring part, in the same or an adjacent region
- **WHEN** the neighbouring part is deleted or rewritten
- **THEN** the shared tile keeps the surviving object's collision

### Requirement: Zone updates address the destination part

Zone updates for a part's runtime objects, disabled static objects and ground items SHALL be addressed to the destination part's coordinates and plane, never to its source part.

#### Scenario: Cross-part and cross-plane mapping

- **GIVEN** a destination part on plane 0 mapped to a different source part on plane 1
- **WHEN** a full or incremental zone update is sent for it
- **THEN** the update's zone base and plane are the destination part's

### Requirement: Dynamic creation builds content once

Creating a dynamic region SHALL apply every part mapping before building, so each column is built once and object spawn hooks run only for the final objects.

#### Scenario: Object spawn hooks during creation

- **WHEN** a dynamic region is created from a source with one static object
- **THEN** the object's spawn hook runs once and no destroy hook runs

### Requirement: Restoring a standard region

Making a region standard SHALL map every part to itself, present, with rotation 0, and rebuild its objects and collision from the region's own template.

#### Scenario: Dynamic edits are undone

- **GIVEN** a region loaded from its template, made dynamic, with parts written, rotated and deleted
- **WHEN** it is made standard
- **THEN** its part mappings, presence, objects and collision equal a freshly loaded standard region
