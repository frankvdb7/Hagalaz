import type { MapFloor, MapRenderRegion } from "../services/map-render.models";
import { isVisibleFloor, linearFloorColor, type FloorSample, type VisibleFloor } from "./terrain-blending";

export type SpreadPoint = { sample: FloorSample; priority: number } | undefined;
export type SpreadLookup = (plane: number, x: number, y: number) => readonly SpreadPoint[];

interface SpreadingTile {
    sample: FloorSample;
    priority: number;
    shape: number;
    rotation: number;
}

// Class274's perimeter coverage by overlay shape, points 0-7 clockwise from the tile origin.
const coveredPoints = ["11111111", "11100011", "10000111", "00111100", "11111100", "11100111", "11000111", "11000001", "01111111", "10001111", "11111000", "11100000", "00000000"];
// Neighbour [dx, dy] claiming one corner, [our point, its point before rotation].
const diagonals = [
    [-1, -1, 0, 4],
    [1, -1, 2, 6],
    [-1, 1, 6, 2],
    [1, 1, 4, 0],
] as const;
// Neighbour [dx, dy] claiming one shared edge, [our start, our step, its start, its step].
const edges = [
    [0, -1, 2, -1, 4, 1],
    [0, 1, 4, 1, 2, -1],
    [-1, 0, 6, 1, 4, -1],
    [1, 0, 4, -1, 6, 1],
] as const;
const noSpread: readonly SpreadPoint[] = [];

export function overlaySample(floor: VisibleFloor): FloorSample {
    return { key: `overlay:${floor.id}`, layer: { textureId: floor.textureId, textureScale: floor.textureScale, color: linearFloorColor(floor.rgb) }, weight: 1 };
}

/** The client orders overlays by authored priority, then definition ID. Tile values are ID + 1. */
export function overlayPriority(floor: MapFloor): number {
    return ((floor.overlayProperties?.priority ?? 8) << 8) | (floor.id - 1);
}

function covers(tile: SpreadingTile, point: number): boolean {
    return coveredPoints[tile.shape]?.[(point + tile.rotation * 2) & 7] === "1";
}

function claim(points: SpreadPoint[], point: number, tile: SpreadingTile): void {
    if ((points[point]?.priority ?? -1) <= tile.priority) points[point] = { sample: tile.sample, priority: tile.priority };
}

/** Class274.method2698: perimeter points taken over by surrounding overlays that spread. */
export function overlaySpreads(regions: readonly MapRenderRegion[]): SpreadLookup {
    const tiles = new Map<string, SpreadingTile>();
    for (const region of regions) addSpreadingTiles(region, tiles);
    if (!tiles.size) return () => noSpread;
    const cache = new Map<string, readonly SpreadPoint[]>();
    return (plane, x, y) => {
        const key = `${plane}:${x}:${y}`;
        let points = cache.get(key);
        if (!points) {
            points = resolvePoints(tiles, plane, x, y);
            cache.set(key, points);
        }
        return points;
    };
}

function addSpreadingTiles(region: MapRenderRegion, tiles: Map<string, SpreadingTile>): void {
    const floors = new Map(region.floors.filter(floor => floor.overlay && floor.overlayProperties?.spreadsToNeighbours && isVisibleFloor(floor)).map(floor => [floor.id, floor as VisibleFloor]));
    if (!floors.size) return;
    region.planes.forEach((plane, level) => {
        for (let tile = 0; tile < 4096; tile++) {
            const floor = floors.get(plane.overlays[tile]);
            if (!floor) continue;
            tiles.set(`${level}:${region.regionX * 64 + Math.floor(tile / 64)}:${region.regionY * 64 + (tile % 64)}`, {
                sample: overlaySample(floor),
                priority: overlayPriority(floor),
                shape: plane.overlayShapes[tile],
                rotation: plane.overlayRotations[tile],
            });
        }
    });
}

type TileAt = (dx: number, dy: number) => SpreadingTile | undefined;

function resolvePoints(tiles: ReadonlyMap<string, SpreadingTile>, plane: number, x: number, y: number): readonly SpreadPoint[] {
    const at: TileAt = (dx, dy) => tiles.get(`${plane}:${x + dx}:${y + dy}`);
    const points: SpreadPoint[] = [];
    // Diagonals are written first and unconditionally; edges and the tile itself then compare priority.
    claimDiagonals(points, at);
    for (const edge of edges) claimEdge(points, at, edge);
    const own = at(0, 0);
    if (own) for (let point = 0; point < 8; point++) if (covers(own, point)) claim(points, point, own);
    return points;
}

function claimDiagonals(points: SpreadPoint[], at: TileAt): void {
    for (const [dx, dy, point, source] of diagonals) {
        const tile = at(dx, dy);
        if (tile && covers(tile, source)) points[point] = { sample: tile.sample, priority: tile.priority };
    }
}

function claimEdge(points: SpreadPoint[], at: TileAt, [dx, dy, start, step, sourceStart, sourceStep]: (typeof edges)[number]): void {
    const tile = at(dx, dy);
    if (!tile) return;
    for (let index = 0; index < 3; index++) {
        if (covers(tile, sourceStart + sourceStep * index)) claim(points, (start + step * index) & 7, tile);
    }
}

/** The spreading floor at a local shape point, once it outranks `minimumPriority`. */
export function spreadSample(points: readonly SpreadPoint[], shapePoint: number, rotation: number, minimumPriority: number): FloorSample | undefined {
    if (shapePoint >= 8) return undefined;
    const point = points[(shapePoint - rotation * 2) & 7];
    return point && point.priority > minimumPriority ? point.sample : undefined;
}
