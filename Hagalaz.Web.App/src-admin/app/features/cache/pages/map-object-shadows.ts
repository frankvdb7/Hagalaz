import { underwaterPlaneIndex, mapAppearanceKey, type MapPlacement, type MapRenderRegion, type ObjectRenderAppearance } from "../services/map-render.models";

/** The client's filtered floor-shadow grid, read by world position. */
export interface ObjectShadowField {
    /** Identifies the field's contents, so unchanged shadows need no terrain rebuild. */
    readonly signature: string;
    value(plane: number, worldX: number, worldY: number): number;
}

export interface ShadowSource {
    placement: MapPlacement;
    regionId: number;
    /** Where a draft has moved the placement; absent when it has not moved. */
    moved?: Pick<MapPlacement, "x" | "y" | "plane" | "rotation">;
}

export const noObjectShadows: ObjectShadowField = { signature: "", value: () => 0 };

const wallStrength = 50;
const cell = (x: number, y: number): number => x * 65536 + y;

type Grid = Map<number, number>;

/** Each placement writes the maximum, never a sum (Class_xa.LA). */
function write(grid: Grid, x: number, y: number, strength: number): void {
    const key = cell(x, y);
    if ((grid.get(key) ?? 0) < strength) grid.set(key, strength);
}

/** The floor corners along a wall's edge or at a corner piece (Map.addToClip). */
const wallCorners: Record<number, readonly (readonly (readonly [number, number])[])[]> = {
    0: [[[0, 0], [0, 1]], [[0, 1], [1, 1]], [[1, 0], [1, 1]], [[0, 0], [1, 0]]],
    1: [[[0, 1]], [[1, 1]], [[1, 0]], [[0, 0]]],
    3: [[[0, 1]], [[1, 1]], [[1, 0]], [[0, 0]]],
};

function writePlacement(grid: Grid, x: number, y: number, shape: number, rotation: number, appearance: ObjectRenderAppearance, strength: number): void {
    const corners = wallCorners[shape]?.[rotation];
    if (corners) {
        for (const [dx, dy] of corners) write(grid, x + dx, y + dy, wallStrength);
        return;
    }
    if (shape === 10 || shape === 11) writeFootprint(grid, x, y, rotatedSize(appearance, rotation), strength);
}

/** The object's footprint, with width and length swapped for quarter turns. */
function rotatedSize({ sizeX, sizeY }: ObjectRenderAppearance, rotation: number): readonly [number, number] {
    return rotation & 1 ? [sizeY, sizeX] : [sizeX, sizeY];
}

function writeFootprint(grid: Grid, x: number, y: number, [sizeX, sizeY]: readonly [number, number], strength: number): void {
    for (let dx = 0; dx <= sizeX; dx++) for (let dy = 0; dy <= sizeY; dy++) write(grid, x + dx, y + dy, strength);
}

/** Class_xa_Sub1.SA: deliberately asymmetric, and its integer weights can exceed the source. */
function filterGrid(grid: Grid): Grid {
    const at = (x: number, y: number): number => grid.get(cell(x, y)) ?? 0;
    const filtered: Grid = new Map();
    for (const key of grid.keys()) {
        const x = Math.floor(key / 65536), y = key - x * 65536;
        for (const [fx, fy] of [[x, y], [x + 1, y], [x - 1, y], [x, y + 1], [x, y - 1]]) {
            const target = cell(fx, fy);
            if (filtered.has(target)) continue;
            filtered.set(target, (at(fx - 1, fy) >> 2) + (at(fx + 1, fy) >> 3) + (at(fx, fy - 1) >> 2) + (at(fx, fy + 1) >> 3) + (at(fx, fy) >> 1));
        }
    }
    return filtered;
}

export function objectShadowField(regions: readonly MapRenderRegion[], appearances: ReadonlyMap<string, ObjectRenderAppearance>, sources: readonly ShadowSource[]): ObjectShadowField {
    const origin = new Map(regions.map(region => [region.id, region]));
    const grids = new Map<number, Grid>();
    for (const source of sources) {
        const caster = shadowCaster(origin, appearances, source);
        if (!caster) continue;
        const { region, position, appearance, strength } = caster;
        const grid: Grid = grids.get(position.plane) ?? new Map();
        grids.set(position.plane, grid);
        writePlacement(grid, region.regionX * 64 + position.x, region.regionY * 64 + position.y, source.placement.shape, position.rotation, appearance, strength);
    }
    if (!grids.size) return noObjectShadows;
    return shadowField(grids);
}

/** The source's region, current position and appearance when it casts a shadow above the underwater plane. */
function shadowCaster(origin: ReadonlyMap<number, MapRenderRegion>, appearances: ReadonlyMap<string, ObjectRenderAppearance>, { placement, regionId, moved }: ShadowSource) {
    const region = origin.get(regionId);
    const position = moved ?? placement;
    if (!region || position.plane >= underwaterPlaneIndex) return null;
    // Falls back like the object builder until an edited rotation's appearance arrives.
    const appearance = appearances.get(mapAppearanceKey({ ...placement, rotation: position.rotation })) ?? appearances.get(mapAppearanceKey(placement));
    const strength = appearance?.shadowStrength ?? 0;
    return appearance && strength > 0 ? { region, position, appearance, strength } : null;
}

function shadowField(grids: ReadonlyMap<number, Grid>): ObjectShadowField {
    const filtered = new Map([...grids].map(([plane, grid]) => [plane, filterGrid(grid)]));
    const signature = [...filtered].sort(([left], [right]) => left - right)
        .map(([plane, grid]) => `${plane}:${[...grid].filter(([, value]) => value > 0).sort(([left], [right]) => left - right).flat().join()}`).join("|");
    return {
        signature,
        value(plane, worldX, worldY) {
            const grid = filtered.get(plane);
            if (!grid) return 0;
            const x = Math.floor(worldX), y = Math.floor(worldY), fx = worldX - x, fy = worldY - y;
            const corner = (dx: number, dy: number): number => grid.get(cell(x + dx, y + dy)) ?? 0;
            const south = corner(0, 0) + (corner(1, 0) - corner(0, 0)) * fx;
            const north = corner(0, 1) + (corner(1, 1) - corner(0, 1)) * fx;
            return south + (north - south) * fy;
        },
    };
}

/**
 * The client scales floor lightness by (74 - shadow) / 128. These floors exclude that base brightness, so only the
 * change relative to unshadowed ground applies, through the palette's 0.7 gamma and on to linear output.
 */
export function shadowedFloorColor(color: readonly number[], shadow: number): number[] {
    const factor = (Math.max(0, 74 - shadow) / 74) ** (0.7 * 2.2);
    return color.map(channel => channel * factor);
}
