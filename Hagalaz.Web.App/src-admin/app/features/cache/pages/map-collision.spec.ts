import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import { describe, expect, it } from "vitest";
import type { MapPlacement, ObjectCollision } from "../services/map-render.models";
import { required } from "../../../../testing/required";
import { buildCollisionMap, CollisionFlag as Flag, collisionBitNames } from "./map-collision";

const id = 257;
const objectId = 7;
const blocking = { sizeX: 1, sizeY: 1, clipType: 1, solid: true, gateway: false };
const placement = (shape: number, rotation = 0, x = 10, y = 10, plane = 0): MapPlacement =>
    ({ id: objectId, shape, rotation, x, y, plane });
const region = (objects: MapPlacement[], regionX = 1) => ({
    id: (regionX << 8) | 1, regionX, regionY: 1, objects,
    planes: Array.from({ length: 4 }, () => ({ flags: new Array<number>(4096).fill(0) })),
});

const fixture = (name: string) => readFileSync(resolve(process.cwd(), "../TestSupport/Fixtures/maps", name));

/** Decode the pinned landscape's source placements before applying any bridge flags. */
function landscapePlacements(): MapPlacement[] {
    const bytes = fixture("landscape-12850.dat");
    let cursor = 0;
    const smart = () => {
        const first = bytes.readUInt8(cursor++);
        return first < 128 ? first : ((first << 8) | bytes.readUInt8(cursor++)) - 32768;
    };
    const objects: MapPlacement[] = [];
    let id = -1;
    while (true) {
        let delta = 0, part: number;
        do {
            part = smart();
            delta += part;
        } while (part === 32767);
        if (!delta) return objects;
        id += delta;
        let tile = 0;
        for (let step = smart(); step; step = smart()) {
            tile += step - 1;
            const attributes = bytes.readUInt8(cursor++);
            objects.push({ id, shape: attributes >> 2, rotation: attributes & 3, x: (tile >> 6) & 63, y: tile & 63, plane: tile >> 12 });
        }
    }
}

it("matches every server collision value in the pinned region 12850", () => {
    const flags = JSON.parse(fixture("flags-12850.json").toString()) as number[][];
    const objects = landscapePlacements();
    const definitions = JSON.parse(fixture("object-collision-12850.json").toString()) as (ObjectCollision & { id: number })[];
    const expected = JSON.parse(fixture("collision-12850.json").toString()) as number[][];
    expect(objects).toHaveLength(5934);
    expect(definitions.map(entry => entry.id)).toEqual([...new Set(objects.map(object => object.id))]);
    const actual = required(buildCollisionMap(
        [{ id: 12850, regionX: 50, regionY: 50, planes: flags.map(plane => ({ flags: plane })), objects }],
        new Map(), new Map(definitions.map(entry => [entry.id, entry])),
    ).get(12850), "region 12850 collision");
    for (const [plane, tiles] of actual.entries()) {
        for (const [tile, value] of tiles.entries()) {
            if (value !== expected[plane][tile]) expect(value, `Plane ${plane} tile ${tile}`).toBe(expected[plane][tile]);
        }
        expect(tiles).toHaveLength(expected[plane].length);
    }
});
const definitions = (definition: ObjectCollision) => new Map([[objectId, definition]]);
const wallCases = [
    [0, 0, 128, [[-1, 0, 8]]], [0, 1, 2, [[0, 1, 32]]], [0, 2, 8, [[1, 0, 128]]], [0, 3, 32, [[0, -1, 2]]],
    [1, 0, 1, [[-1, 1, 16]]], [1, 1, 4, [[1, 1, 64]]], [1, 2, 16, [[1, -1, 1]]], [1, 3, 64, [[-1, -1, 4]]],
    [2, 0, 130, [[-1, 0, 8], [0, 1, 32]]], [2, 1, 10, [[0, 1, 32], [1, 0, 128]]],
    [2, 2, 40, [[1, 0, 128], [0, -1, 2]]], [2, 3, 160, [[0, -1, 2], [-1, 0, 8]]],
    [3, 0, 1, [[-1, 1, 16]]], [3, 1, 4, [[1, 1, 64]]], [3, 2, 16, [[1, -1, 1]]], [3, 3, 64, [[-1, -1, 4]]],
] as const;

describe("buildCollisionMap", () => {
    it.each(wallCases)("writes all three wall layers for shape %i rotation %i on both sides", (shape, rotation, origin, neighbours) => {
        const tiles = required(buildCollisionMap([region([placement(shape, rotation)])], new Map(), definitions(blocking)).get(id), "collision planes")[0];
        const layers = (bits: number) => bits | (bits << 9) | (bits << 22);
        expect(tiles[650]).toBe(layers(origin));
        for (const [dx, dy, bits] of neighbours) expect(tiles[650 + dx * 64 + dy]).toBe(layers(bits));
        expect(tiles.filter(Boolean)).toHaveLength(1 + neighbours.length);
    });

    it("uses the rotated footprint and clip-type-one floor decorations", () => {
        const source = region([placement(10, 1), placement(22, 0, 11, 10)]);
        const tiles = required(buildCollisionMap([source], new Map(), definitions({ ...blocking, sizeX: 2 })).get(id), "collision planes")[0];
        expect(tiles[650]).toBe(Flag.ObjectTile | Flag.ObjectBlock | Flag.ObjectAllowRange);
        expect(tiles[651]).toBe(tiles[650]);
        expect(tiles[714]).toBe(Flag.FloorDecorationBlock);
        expect(tiles.filter(Boolean)).toHaveLength(3);
    });

    it("includes gateway movement bits and excludes solid and allow-range bits when they do not apply", () => {
        const source = region([placement(10), placement(0, 1, 12, 12), placement(22, 0, 14, 14)]);
        const withoutCollision = buildCollisionMap([source], new Map(), definitions({ ...blocking, clipType: 0 }));
        expect(withoutCollision.get(id)?.[0].filter(Boolean)).toHaveLength(0);
        const gateway = buildCollisionMap([source], new Map(), definitions({ ...blocking, clipType: 0, solid: false, gateway: true }));
        expect(gateway.get(id)?.[0][650]).toBe(Flag.ObjectTile);
        expect(gateway.get(id)?.[0][780]).toBe(Flag.WallNorth);
        expect(gateway.get(id)?.[0][781]).toBe(Flag.WallSouth);
        expect(collisionBitNames(required(gateway.get(id), "gateway collision planes")[0][650])).toEqual(["ObjectTile"]);
        expect(gateway.get(id)?.[0][910]).toBe(0);
    });

    it("moves terrain and edited placements down at bridge columns and drops plane zero", () => {
        const source = region([placement(10, 0, 10, 10, 0)]);
        source.planes[0].flags[650] = 1;
        source.planes[1].flags[650] = 3;
        const edits = new Map([[id, [placement(10, 0, 10, 10, 1)]]]);
        const planes = required(buildCollisionMap([source], edits, definitions(blocking)).get(id), "collision planes");
        expect(planes[0][650]).toBe(Flag.FloorBlock | Flag.ObjectTile | Flag.ObjectBlock | Flag.ObjectAllowRange);
        expect(planes[1][650]).toBe(0);
        expect(buildCollisionMap([source], new Map(), definitions(blocking)).get(id)?.[0][650]).toBe(Flag.FloorBlock);
    });

    it("writes a wall into a loaded western neighbour and skips an unloaded neighbour", () => {
        const source = region([placement(0, 0, 0, 10)]);
        const west = region([], 0);
        const loaded = buildCollisionMap([source, west], new Map(), definitions(blocking));
        expect(loaded.get(west.id)?.[0][4042]).toBe(Flag.WallEast | Flag.BlockedEast | Flag.WallAllowRangeEast);
        const unloaded = buildCollisionMap([source], new Map(), definitions(blocking));
        expect(unloaded.size).toBe(1);
        expect(unloaded.get(id)?.[0][10]).toBe(Flag.WallWest | Flag.BlockedWest | Flag.WallAllowRangeWest);
    });
});
