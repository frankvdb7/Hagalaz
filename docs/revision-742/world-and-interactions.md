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

## World-object placement and collision source audit

The [world-object implementation dossier](reference/deep-world-objects.md) separates physical index-16 definitions, index-5 static placements and `gameobject_spawns` overlays. The [321 source-registered handler IDs](data/catalogs/object-script-registrations.csv) and [object opcode constants](data/catalogs/object-opcode-constants.csv) are not cache/world placement records. Map collision, doors, skill objects and travel destinations still require direct fixture evidence.

## P1 object and region interaction protocol

The [protocol integration dossier](reference/deep-client-interface-cs2-protocol-integration.md#4-packet-framing-reconciliation-and-protocol-state-cautions) documents the serious **opcode 162 enclosed zone update** declared-framing mismatch and embedded encoder metadata ownership risk. Test actual grouped object/ground-item/projectile updates with the unchanged client before claiming zone map parity. Server/Client 742 interaction and component use-on packets also require exact transforms and collision validation.

Object scripts may select a varbit dynamically from `Owner.Definition.VarpBitFileId`; the [source call inventory](data/catalogs/client-varbit-source-calls.csv) separates these dynamic values from hard-coded varbits. Verify against decoded index-22 definitions and object transformation state; do not substitute static script-registration IDs.
