import type { StrokePreviewGeometry } from "../../../../../shared/model-viewer/model-scene";
import type { MapRenderRegion } from "../services/map-render.models";
import { CollisionFlag, type CollisionMap } from "./map-collision";
import { mapSceneOrigin } from "./map-point-lights";
import { floorTileTriangles } from "./map-scene-geometry";
import { blockedPlane, tileFlagOptions } from "./map-tile-flags";

/** Tiles and edges that placed objects block. */
export const objectCollisionRgb = 0xff9500;
export const projectileObjectRgb = 0xff63d8;
export const projectileWallRgb = 0x65dfff;
export const walkableRgb = 0x38bd83;
/** A blocked ground tile under a bridge, which the game drops. */
export const ineffectiveBlockRgb = 0x6a2a2a;
const overlayOpacity = 0.55;

/** A rectangle inside one tile, in tile fractions from its south-west corner. */
type TilePart = readonly [x0: number, y0: number, x1: number, y1: number];
const wholeTile: TilePart = [0, 0, 1, 1];
const walkableTile: TilePart = [0.06, 0.06, 0.94, 0.94];
const walkableFlagInset: TilePart = [0.18, 0.18, 0.3, 0.3];
const secondFlagInset: TilePart = [0.3, 0.3, 0.7, 0.7];
const edge = 0.14, cornerSize = 0.25;
const west: TilePart = [0, 0, edge, 1], north: TilePart = [0, 1 - edge, 1, 1], east: TilePart = [1 - edge, 0, 1, 1], south: TilePart = [0, 0, 1, edge];
const wallParts: readonly TilePart[] = [
    [0, 1 - cornerSize, cornerSize, 1], north,
    [1 - cornerSize, 1 - cornerSize, 1, 1], east,
    [1 - cornerSize, 0, 1, cornerSize], south,
    [0, 0, cornerSize, cornerSize], west,
];
const surfacePlanes = 4;
const blockedRgb = tileFlagOptions[0].rgb;
const movementObjectMask = CollisionFlag.ObjectTile | CollisionFlag.FloorDecorationBlock;
const movementTileMask = CollisionFlag.FloorBlock | movementObjectMask;
const tileVertices: readonly (readonly [number, number])[] = [[0, 0], [1, 0], [0, 1], [0, 1], [1, 0], [1, 1]];

interface OverlayContext {
    regions: ReadonlyMap<number, MapRenderRegion>;
    origin: { x: number; y: number };
    output: StrokePreviewGeometry;
}

/** Markers for stored terrain flags and collision on the planes the server checks. */
export function buildWalkabilityOverlay(regions: readonly MapRenderRegion[], visiblePlanes: readonly boolean[], collision: CollisionMap): StrokePreviewGeometry | null {
    if (!regions.length) return null;
    const context: OverlayContext = {
        regions: new Map(regions.map(region => [(region.regionX << 8) | region.regionY, region])),
        origin: mapSceneOrigin(regions),
        output: { positions: [], uvs: [], rgb: [], textureId: null, opacity: overlayOpacity },
    };
    for (const region of regions) drawRegion(context, region, visiblePlanes, collision.get(region.id));
    return context.output.positions.length ? context.output : null;
}

function drawRegion(
    context: OverlayContext, region: MapRenderRegion, visiblePlanes: readonly boolean[],
    collision: readonly Int32Array[] | undefined,
): void {
    for (let plane = 0; plane < surfacePlanes; plane++) {
        if (!visiblePlanes[plane] || !region.planes[plane]) continue;
        addFlagMarkers(context, region, plane);
        const tiles = collision?.[plane];
        if (!tiles) continue;
        addWalkableMarkers(context, region, plane, tiles);
        addCollisionMarkers(context, region, plane, tiles);
    }
}

function addFlagMarkers(context: OverlayContext, region: MapRenderRegion, plane: number): void {
    const { flags } = region.planes[plane];
    for (let tile = 0; tile < flags.length; tile++) {
        if (!flags[tile]) continue;
        const [first, second] = tileFlagOptions.filter(option => flags[tile] & option.bit);
        if (!first) continue;
        const wx = region.regionX * 64 + (tile >> 6), wy = region.regionY * 64 + (tile & 63);
        const dropped = first.name === "blocked" && blockedPlane(region.planes, plane, tile) === null;
        addPart(context, plane, wx, wy, wholeTile, dropped ? ineffectiveBlockRgb : first.rgb);
        if (second) addPart(context, plane, wx, wy, secondFlagInset, second.rgb);
    }
}

function addWalkableMarkers(context: OverlayContext, region: MapRenderRegion, plane: number, tiles: Int32Array): void {
    const { flags, underlays, overlays } = region.planes[plane];
    for (let tile = 0; tile < tiles.length; tile++) {
        if ((tiles[tile] & movementTileMask) || !(underlays[tile] || overlays[tile])) continue;
        const wx = region.regionX * 64 + (tile >> 6), wy = region.regionY * 64 + (tile & 63);
        const part = tileFlagOptions.some(option => flags[tile] & option.bit) ? walkableFlagInset : walkableTile;
        addPart(context, plane, wx, wy, part, walkableRgb);
    }
}

function addCollisionMarkers(context: OverlayContext, region: MapRenderRegion, plane: number, tiles: Int32Array): void {
    for (let tile = 0; tile < tiles.length; tile++) {
        const bits = tiles[tile];
        if (!bits) continue;
        const wx = region.regionX * 64 + (tile >> 6), wy = region.regionY * 64 + (tile & 63);
        if ((bits & CollisionFlag.FloorBlock) && blockedPlane(region.planes, plane, tile) !== plane)
            addPart(context, plane, wx, wy, wholeTile, blockedRgb);
        if (bits & movementObjectMask) addPart(context, plane, wx, wy, wholeTile, bits & CollisionFlag.ObjectBlock ? projectileObjectRgb : objectCollisionRgb);
        addWallMarkers(context, plane, wx, wy, bits);
    }
}

function addWallMarkers(context: OverlayContext, plane: number, wx: number, wy: number, bits: number): void {
    for (const [direction, part] of wallParts.entries()) {
        if (bits & (1 << direction)) {
            const rgb = bits & (CollisionFlag.BlockedNorthWest << direction) ? projectileWallRgb : objectCollisionRgb;
            addPart(context, plane, wx, wy, part, rgb);
        }
    }
}

function addPart(context: OverlayContext, plane: number, wx: number, wy: number, [x0, y0, x1, y1]: TilePart, rgb: number): void {
    const corners = tileCorners(context, plane, wx, wy);
    if (!corners) return;
    const [h00, h10, h01, h11] = corners;
    if (h00 + h11 !== h10 + h01) {
        addConformingPart(context, plane, wx, wy, [x0, y0, x1, y1], rgb);
        return;
    }
    const { output, origin } = context;
    for (const [cx, cy] of tileVertices) {
        const fx = cx ? x1 : x0, fy = cy ? y1 : y0;
        const height = (h00 * (1 - fx) + h10 * fx) * (1 - fy) + (h01 * (1 - fx) + h11 * fx) * fy;
        output.positions.push((wx + fx - origin.x) * 512, -height, (wy + fy - origin.y) * 512);
        output.uvs.push(0, 0);
    }
    output.rgb.push(rgb);
}

type FloorPoint = [x: number, y: number, height: number];

/** Clip to an axis-aligned edge while keeping heights on the original triangle. */
function clipPart(points: readonly FloorPoint[], axis: 0 | 1, edge: number, side: number): FloorPoint[] {
    const clipped: FloorPoint[] = [];
    for (let i = 0; i < points.length; i++) {
        const start = points[i], end = points[(i + 1) % points.length];
        const a = (start[axis] - edge) * side, b = (end[axis] - edge) * side;
        if (a >= 0) clipped.push(start);
        if ((a < 0 && b > 0) || (a > 0 && b < 0)) {
            const t = a / (a - b);
            clipped.push([
                start[0] + (end[0] - start[0]) * t,
                start[1] + (end[1] - start[1]) * t,
                start[2] + (end[2] - start[2]) * t,
            ]);
        }
    }
    return clipped;
}

function addConformingPart(context: OverlayContext, plane: number, wx: number, wy: number, [x0, y0, x1, y1]: TilePart, rgb: number): void {
    const region = context.regions.get(((wx >> 6) << 8) | (wy >> 6));
    if (!region) return;
    const points: FloorPoint[] = [];
    for (const triangle of floorTileTriangles(region, plane, wx & 63, wy & 63)) {
        const left = clipPart(triangle, 0, x0, 1);
        const right = clipPart(left, 0, x1, -1);
        const bottom = clipPart(right, 1, y0, 1);
        const polygon = clipPart(bottom, 1, y1, -1);
        for (let i = 1; i + 1 < polygon.length; i++) points.push(polygon[0], polygon[i], polygon[i + 1]);
    }
    if (!points.length) return;
    // The tile mesh stores one colour per six vertices. Pad an odd triangle with a degenerate one.
    if (points.length % 6) points.push(points[0], points[0], points[0]);
    const { output, origin } = context;
    for (const [fx, fy, height] of points) {
        output.positions.push((wx + fx - origin.x) * 512, -height, (wy + fy - origin.y) * 512);
        output.uvs.push(0, 0);
    }
    for (let i = 0; i < points.length; i += 6) output.rgb.push(rgb);
}

/** A tile's corner heights, south-west, south-east, north-west, north-east; null where the ground is missing or not loaded. */
function tileCorners(context: OverlayContext, plane: number, wx: number, wy: number): number[] | null {
    const heights = context.regions.get(((wx >> 6) << 8) | (wy >> 6))?.planes[plane]?.cornerHeights;
    const corner = (wx & 63) * 65 + (wy & 63);
    const corners = [corner, corner + 65, corner + 1, corner + 66].map(index => heights?.[index] ?? null);
    return corners.includes(null) ? null : corners as number[];
}
