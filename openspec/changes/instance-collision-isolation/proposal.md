# Proposal

## Why

Collision reads ignore the world a creature stands in. `MapRegionService.GetClippingFlag` and `IsAccessible` always read the global world, so a creature inside an encounter instance (for example a TzHaar cave) is routed, blocked and shot at using the global tiles that share its coordinates. The `Dimension` model is planned for removal, so the fix must not spread a dimension argument through the pathfinders.

## What Changes

Collision is read from the world of the traversal's source location.

In scope:

- One world-aware collision read on `IMapRegionService`; the coordinate-only read stays and means the global world.
- `IsAccessible(ILocation)` reads the location's own world.
- `PathFinderBase` takes the world from the source location at each public entry point (`Find`, location-based `CheckStep`) and restores it on exit. Helper signatures are unchanged.
- `IPathFinder.CheckStep(IVector3, int, int, int)`, used by `Movement.Tick` and melee reach, so a step is checked in the creature's world.
- Entity-to-entity and entity-to-object paths between different worlds fail instead of matching overlapping coordinates.

Non-goals:

- Removing `Dimension`, a new world registry, or changing region residency and teardown.
- Path points: they stay coordinates, and `Movement.AddToQueue(IVector2)` already places them in the moving creature's world.

## Impact

A missing, not ready, removed or out-of-range world still reads as blocked, and no read creates or resumes a region. Global-world behaviour is unchanged.
