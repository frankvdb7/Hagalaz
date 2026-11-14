import type { MapPlacement, MapRenderRegion, ObjectCollision } from "../services/map-render.models";
import { blockedPlane, collisionPlane } from "./map-tile-flags";

/** Individual bits in the server's CollisionFlag enum. */
export const CollisionFlag = {
    Walkable: 0x0,
    WallNorthWest: 0x1,
    WallNorth: 0x2,
    WallNorthEast: 0x4,
    WallEast: 0x8,
    WallSouthEast: 0x10,
    WallSouth: 0x20,
    WallSouthWest: 0x40,
    WallWest: 0x80,
    ObjectTile: 0x100,
    BlockedNorthWest: 0x200,
    BlockedNorth: 0x400,
    BlockedNorthEast: 0x800,
    BlockedEast: 0x1000,
    BlockedSouthEast: 0x2000,
    BlockedSouth: 0x4000,
    BlockedSouthWest: 0x8000,
    BlockedWest: 0x10000,
    ObjectBlock: 0x20000,
    FloorDecorationBlock: 0x40000,
    Unknown1: 0x80000,
    Unknown2: 0x100000,
    FloorBlock: 0x200000,
    WallAllowRangeNorthWest: 0x400000,
    WallAllowRangeNorth: 0x800000,
    WallAllowRangeNorthEast: 0x1000000,
    WallAllowRangeEast: 0x2000000,
    WallAllowRangeSouthEast: 0x4000000,
    WallAllowRangeSouth: 0x8000000,
    WallAllowRangeSouthWest: 0x10000000,
    WallAllowRangeWest: 0x20000000,
    ObjectAllowRange: 0x40000000,
} as const;

export function collisionBitNames(value: number): string[] {
    return Object.entries(CollisionFlag).filter(([, bit]) => (value & bit) !== 0).map(([name]) => name);
}

export type CollisionMap = ReadonlyMap<number, readonly Int32Array[]>;
type CollisionRegion = Pick<MapRenderRegion, "id" | "regionX" | "regionY" | "objects"> & {
    planes: readonly { flags: readonly number[] }[];
};

const directions = [
    [-1, 1], [0, 1], [1, 1], [1, 0], [1, -1], [0, -1], [-1, -1], [-1, 0],
] as const;
const straight = [[7], [1], [3], [5]];
const diagonal = [[0], [2], [4], [6]];
const corner = [[7, 1], [1, 3], [3, 5], [5, 7]];
const wallDirections = [straight, diagonal, corner, diagonal];

export function buildCollisionMap(
    regions: readonly CollisionRegion[],
    placements: ReadonlyMap<number, readonly MapPlacement[]>,
    definitions: ReadonlyMap<number, ObjectCollision | null>,
): CollisionMap {
    const entries = regions.map(region => ({ region, planes: Array.from({ length: 4 }, () => new Int32Array(4096)) }));
    const values = new Map(entries.map(({ region, planes }) => [region.id, planes]));
    const write = (x: number, y: number, plane: number, bits: number) => {
        const tiles = values.get(((x >> 6) << 8) | (y >> 6))?.[plane];
        if (tiles) tiles[((x & 63) << 6) | (y & 63)] |= bits;
    };

    for (const { region, planes } of entries) {
        writeTerrain(region, planes);
        for (const placement of placements.get(region.id) ?? region.objects) {
            const definition = definitions.get(placement.id);
            if (definition) writePlacement(region, placement, definition, write);
        }
    }
    return values;
}

type CollisionWriter = (x: number, y: number, plane: number, bits: number) => void;

function writeTerrain(region: CollisionRegion, planes: readonly Int32Array[]): void {
    for (const [plane, { flags }] of region.planes.slice(0, 4).entries()) {
        for (let tile = 0; tile < flags.length; tile++) {
            const target = blockedPlane(region.planes, plane, tile);
            if (target !== null) planes[target][tile] |= CollisionFlag.FloorBlock;
        }
    }
}

function writePlacement(region: CollisionRegion, placement: MapPlacement, definition: ObjectCollision, write: CollisionWriter): void {
    if (placement.plane >= 4) return;
    const plane = collisionPlane(region.planes, placement.plane, placement.x * 64 + placement.y);
    if (plane === null) return;
    const x = region.regionX * 64 + placement.x;
    const y = region.regionY * 64 + placement.y;
    writeShape(placement, definition, (dx, dy, bits) => write(x + dx, y + dy, plane, bits));
}

/** Ground decorations block only with clip type 1; walls and footprints block when clipped or when they are gateways. */
function writeShape({ shape, rotation }: MapPlacement, definition: ObjectCollision, writeTile: PlacementWriter): void {
    if (shape === 22) {
        if (definition.clipType === 1) writeTile(0, 0, CollisionFlag.FloorDecorationBlock);
        return;
    }
    if (definition.clipType === 0 && !definition.gateway) return;
    if (shape >= 0 && shape <= 3) writeWalls(wallDirections[shape][rotation], definition, writeTile);
    else if (shape >= 9 && shape <= 21) writeFootprint(rotation, definition, writeTile);
}

type PlacementWriter = (dx: number, dy: number, bits: number) => void;

function writeWalls(walls: readonly number[], { solid, gateway }: ObjectCollision, write: PlacementWriter): void {
    const layers = CollisionFlag.WallNorthWest | (solid ? CollisionFlag.BlockedNorthWest : 0) | (gateway ? 0 : CollisionFlag.WallAllowRangeNorthWest);
    for (const direction of walls) {
        const [dx, dy] = directions[direction];
        write(0, 0, layers << direction);
        write(dx, dy, layers << ((direction + 4) & 7));
    }
}

function writeFootprint(rotation: number, { sizeX, sizeY, solid, gateway }: ObjectCollision, write: PlacementWriter): void {
    const bits = CollisionFlag.ObjectTile | (solid ? CollisionFlag.ObjectBlock : 0) | (gateway ? 0 : CollisionFlag.ObjectAllowRange);
    const [width, height] = rotation & 1 ? [sizeY, sizeX] : [sizeX, sizeY];
    for (let dx = 0; dx < width; dx++) {
        for (let dy = 0; dy < height; dy++) write(dx, dy, bits);
    }
}
