## Why

`MapRegion` writes incorrect solid directional bits for diagonal/corner walls, so the server collision map disagrees with the game client. Its unfinished-wall rotation-0 range flags also disagree with the corresponding unflag path, leaving collision state behind after removal.

## What Changes

- Correct the `Blocked*` directions for diagonal/corner wall rotations 1 and 3, and make the corresponding removal operations exact inverses.
- Correct unfinished-wall rotation 0's origin `WallAllowRange*` pair to match its geometry and existing inverse.
- Add focused unit coverage for all `MapRegion` collision writers: floor decorations, standard objects, wall shapes, flag/removal inversion, and layer dispatch.

### In Scope

- Wall, diagonal/corner wall, and unfinished-wall collision flags for rotations 0 through 3.
- Standard-object footprints and floor-decoration collision behavior.
- The existing `IMapRegionService` collision-writing seam in unit tests.

### Non-goals

- Change projectile or line-of-sight traversal semantics tracked by issue #364.
- Rename or reinterpret `CollisionFlag` values.
- Add collision storage, ownership, retry, persistence, or public API mechanisms.

### Acceptance Criteria

- Solid `WallCornerDiagonal` and `WallCorner` rotations use `NW/SE`, `NE/SW`, `SE/NW`, and `SW/NE` direction pairs for low, solid, and range layers.
- Flagging and unflagging every supported collision writer are exact inverses for the flags written by that object and preserve unrelated seeded collision bits.
- Unfinished-wall rotation 0 range flags match its west/north geometry and are removed exactly.
- The focused GameWorld unit tests, solution build, strict OpenSpec validation, and diff check pass.

### Stop Conditions

Pause and record follow-up work if the corrections require collision-enum changes, projectile traversal changes, a new production abstraction, or alterations outside `MapRegion` collision writing.

## Capabilities

### New Capabilities

- `map-collision-writer`: Server collision writers generate and remove geometric low, solid, and range layers consistently.

### Modified Capabilities

- None.

## Impact

- `Hagalaz.Services.GameWorld/Model/Maps/Regions/CollisionMethods.cs`
- `Hagalaz.Services.GameWorld.Tests/CollisionMethodsTests.cs`
- No public APIs, dependencies, configuration, migrations, or distributed runtime topology changes.
