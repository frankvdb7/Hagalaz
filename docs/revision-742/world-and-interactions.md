# World, maps and entity interaction — revision 742

**Repository assessment:** Established region, object, NPC and packet layers. Code entry points do not imply full behavior coverage.

## Historical scope

Revision-specific map collision, terrain locations, region loading, ground-item spawn visibility, object/NPC interactions and server-authoritative movement.

## Existing Hagalaz entry points

- `Hagalaz.Services.GameWorld/Services/MapRegionService.cs`
- `Hagalaz.Services.GameWorld/Data/MapRegionLoader.cs`
- `Hagalaz.Services.GameWorld/Services/GroundItemService.cs`
- `Hagalaz.Services.GameWorld/Network/Protocol/742/Decoders/GroundItemClickMessageDecoder.cs`

## How to implement

1. Use `IMapRegionService`, current object/NPC services and path-finding contracts. Feature scripts must ask the world for valid reach/line of sight and perform world mutations through registered owners.
2. Keep 742 protocol encoding/decoding in the existing versioned protocol folder; validate map keys, component IDs, coordinate transforms and cache lookups from matching fixtures. See the existing `docs/3d-rendering/` work for appearance specifics.

## Verification criteria

- Blocked geometry prevents melee hits and invalid clicks; stale despawned targets do not execute; ground items respect ownership/visibility and cannot be picked up twice; region reload does not lose mutations.
- Resolve exact 742 interface IDs, timings, data and animations with client/cache fixtures and focused server tests.

## Evidence

- Historical behavior and resource IDs require direct 742 fixture verification.
- Follow [implementation conventions](implementation-conventions.md) before adding state, handlers or persistence.


## Numeric NPC registration inventory

The [NPC script registration CSV](data/catalogs/npc-script-registrations.csv) maps concrete IDs to existing NPC scripts; it is a repository inventory, not NPC spawn data or a 742 client cache dump. See the [NPC and item parity backlog](reference/items-npcs-remaining-work.md).
