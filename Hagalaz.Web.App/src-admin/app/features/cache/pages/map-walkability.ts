import type { StrokePreviewGeometry } from "../../../../../shared/model-viewer/model-scene";
import { mapAppearanceKey, type MapPlacement, type MapRenderRegion, type ObjectRenderAppearance } from "../services/map-render.models";
import { mapSceneOrigin } from "./map-point-lights";
import { blockedPlane, collisionPlane, tileFlagOptions } from "./map-tile-flags";

/** Tiles and edges that placed objects block. */
export const objectCollisionRgb = 0xff9500;
/** A blocked ground tile under a bridge, which the game drops. */
export const ineffectiveBlockRgb = 0x6a2a2a;
const overlayOpacity = 0.55;

export interface WalkabilityObjects {
    appearances: ReadonlyMap<string, ObjectRenderAppearance>;
    /** A region's objects as edited; a region not listed uses its stored objects. */
    placements?: ReadonlyMap<number, readonly MapPlacement[]>;
}

/** A rectangle inside one tile, in tile fractions from its south-west corner. */
type TilePart = readonly [x0: number, y0: number, x1: number, y1: number];
const wholeTile: TilePart = [0, 0, 1, 1];
const secondFlagInset: TilePart = [0.3, 0.3, 0.7, 0.7];
const edge = 0.14, cornerSize = 0.25;
const west: TilePart = [0, 0, edge, 1], north: TilePart = [0, 1 - edge, 1, 1], east: TilePart = [1 - edge, 0, 1, 1], south: TilePart = [0, 0, 1, edge];
// Indexed by rotation, following the game server's wall collision: north is +y.
const straightWalls: readonly (readonly TilePart[])[] = [[west], [north], [east], [south]];
const cornerWalls: readonly (readonly TilePart[])[] = [
    [[0, 1 - cornerSize, cornerSize, 1]], [[1 - cornerSize, 1 - cornerSize, 1, 1]], [[1 - cornerSize, 0, 1, cornerSize]], [[0, 0, cornerSize, cornerSize]],
];
const angledWalls: readonly (readonly TilePart[])[] = [[west, north], [north, east], [east, south], [south, west]];
const wallParts: readonly (readonly (readonly TilePart[])[])[] = [straightWalls, cornerWalls, angledWalls, cornerWalls];
const lastWallShape = 3, firstStandardShape = 9, lastStandardShape = 21, groundDecorationShape = 22, surfacePlanes = 4;
const tileVertices: readonly (readonly [number, number])[] = [[0, 0], [1, 0], [0, 1], [0, 1], [1, 0], [1, 1]];

interface OverlayContext {
    regions: ReadonlyMap<number, MapRenderRegion>;
    origin: { x: number; y: number };
    output: StrokePreviewGeometry;
}

/**
 * Markers for every flagged tile, and for object collision when object data is given, drawn on the plane
 * each is stored on. Blocking follows the game: see `collisionPlane`.
 */
export function buildWalkabilityOverlay(regions: readonly MapRenderRegion[], visiblePlanes: readonly boolean[], objects?: WalkabilityObjects): StrokePreviewGeometry | null {
    if (!regions.length) return null;
    const context: OverlayContext = {
        regions: new Map(regions.map(region => [(region.regionX << 8) | region.regionY, region])),
        origin: mapSceneOrigin(regions),
        output: { positions: [], uvs: [], rgb: [], textureId: null, opacity: overlayOpacity },
    };
    for (const region of regions) {
        for (let plane = 0; plane < surfacePlanes; plane++) if (visiblePlanes[plane] && region.planes[plane]) addFlagMarkers(context, region, plane);
        if (objects) addObjectMarkers(context, region, visiblePlanes, objects);
    }
    return context.output.positions.length ? context.output : null;
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

function addObjectMarkers(context: OverlayContext, region: MapRenderRegion, visiblePlanes: readonly boolean[], objects: WalkabilityObjects): void {
    for (const placement of objects.placements?.get(region.id) ?? region.objects) {
        const appearance = objects.appearances.get(mapAppearanceKey(placement));
        if (!appearance || placement.plane >= surfacePlanes || !visiblePlanes[placement.plane]) continue;
        // On a bridge column the game moves the object one plane down, and drops it below the ground plane.
        if (collisionPlane(region.planes, placement.plane, placement.x * 64 + placement.y) === null) continue;
        const wx = region.regionX * 64 + placement.x, wy = region.regionY * 64 + placement.y;
        for (const [dx, dy, part] of objectParts(placement, appearance)) addPart(context, placement.plane, wx + dx, wy + dy, part, objectCollisionRgb);
    }
}

/** The tile parts an object blocks, by the game server's rules for each shape. */
function objectParts(placement: MapPlacement, appearance: ObjectRenderAppearance): [dx: number, dy: number, part: TilePart][] {
    const { shape, rotation } = placement;
    if (shape === groundDecorationShape) return appearance.clipType === 1 ? [[0, 0, wholeTile]] : [];
    if (!appearance.clipType && !appearance.gateway) return [];
    if (shape <= lastWallShape) return wallParts[shape][rotation & 3].map(part => [0, 0, part]);
    return shape >= firstStandardShape && shape <= lastStandardShape ? footprint(appearance, rotation) : [];
}

/** Every tile under a standard object, whose sides swap when it is turned a quarter. */
function footprint(appearance: ObjectRenderAppearance, rotation: number): [dx: number, dy: number, part: TilePart][] {
    const turned = (rotation & 1) === 1;
    const sizeX = turned ? appearance.sizeY : appearance.sizeX, sizeY = turned ? appearance.sizeX : appearance.sizeY;
    return Array.from({ length: sizeX * sizeY }, (_, index) => [Math.floor(index / sizeY), index % sizeY, wholeTile]);
}

function addPart(context: OverlayContext, plane: number, wx: number, wy: number, [x0, y0, x1, y1]: TilePart, rgb: number): void {
    const corners = tileCorners(context, plane, wx, wy);
    if (!corners) return;
    const [h00, h10, h01, h11] = corners;
    const { output, origin } = context;
    for (const [cx, cy] of tileVertices) {
        const fx = cx ? x1 : x0, fy = cy ? y1 : y0;
        const height = (h00 * (1 - fx) + h10 * fx) * (1 - fy) + (h01 * (1 - fx) + h11 * fx) * fy;
        output.positions.push((wx + fx - origin.x) * 512, -height, (wy + fy - origin.y) * 512);
        output.uvs.push(0, 0);
    }
    output.rgb.push(rgb);
}

/** A tile's corner heights, south-west, south-east, north-west, north-east; null where the ground is missing or not loaded. */
function tileCorners(context: OverlayContext, plane: number, wx: number, wy: number): number[] | null {
    const heights = context.regions.get(((wx >> 6) << 8) | (wy >> 6))?.planes[plane]?.cornerHeights;
    const corner = (wx & 63) * 65 + (wy & 63);
    const corners = [corner, corner + 65, corner + 1, corner + 66].map(index => heights?.[index] ?? null);
    return corners.includes(null) ? null : corners as number[];
}
