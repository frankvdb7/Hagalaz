import { underwaterPlaneIndex, type MapFloor, type MapRenderPlane, type MapRenderRegion, type MapTerrainSave, type MapTerrainTileChange } from "../services/map-render.models";
import type { StrokePreviewGeometry } from "../../../../../shared/model-viewer/model-scene";
import { mapHeight } from "./map-scene-geometry";

/** Stored values: height counts steps of 32 above the plane below; floor 0 means none. */
export interface TerrainTileEdit { height?: number; underlay?: number; overlay?: number; overlayShape?: number; overlayRotation?: number; }
/** Region ID, then tile key, to the tile's edited values. */
export type TerrainEdits = ReadonlyMap<number, ReadonlyMap<number, TerrainTileEdit>>;
export type TerrainBrushTool = "raise" | "lower" | "flatten" | "underlay" | "overlay" | "clear-overlay";
export interface TerrainBrush {
    tool: TerrainBrushTool;
    plane: number;
    size: number;
    strength: number;
    underlay: number | null;
    overlay: number | null;
    overlayShape: number;
    overlayRotation: number;
}
/** A viewport position in scene units, 512 to a tile. */
export interface BrushPoint { x: number; z: number; }
export const floorKey = (overlay: boolean, id: number): string => `${overlay}:${id}`;

const heightTools: readonly TerrainBrushTool[] = ["raise", "lower", "flatten"];
const tileKey = (plane: number, x: number, y: number): number => (plane << 12) | (x << 6) | y;
const splitKey = (key: number) => ({ plane: key >> 12, x: (key >> 6) & 63, y: key & 63 });
const usesCorners = (brush: TerrainBrush): boolean => heightTools.includes(brush.tool);

/** Regions as they would look with the edits stored. A region the edits do not reach keeps its identity. */
export function applyTerrainEdits(regions: readonly MapRenderRegion[], edits: TerrainEdits | undefined, floors: ReadonlyMap<string, MapFloor>): readonly MapRenderRegion[] {
    if (!edits?.size) return regions;
    return regions.map(region => previewRegion(region, edits, floors));
}

interface CornerOwner { edits: ReadonlyMap<number, TerrainTileEdit>; offsetX: number; offsetY: number; }

// Row and column 64 of a region's corners belong to the regions east and north of it.
function cornerOwners(region: MapRenderRegion, edits: TerrainEdits): CornerOwner[] {
    const east = region.regionX < 255, north = region.regionY < 255;
    const candidates: [number, number, number, boolean][] = [
        [region.id, 0, 0, true], [region.id + 256, 64, 0, east], [region.id + 1, 0, 64, north], [region.id + 257, 64, 64, east && north],
    ];
    return candidates.flatMap(([id, offsetX, offsetY, exists]) => {
        const owned = exists ? edits.get(id) : undefined;
        return owned ? [{ edits: owned, offsetX, offsetY }] : [];
    });
}

function previewRegion(region: MapRenderRegion, edits: TerrainEdits, catalog: ReadonlyMap<string, MapFloor>): MapRenderRegion {
    const owners = cornerOwners(region, edits);
    if (!owners.length) return region;
    const editsHeights = owners.some(owner => [...owner.edits.values()].some(edit => edit.height !== undefined));
    const planes = region.planes.map(plane => copyPlane(plane, editsHeights));
    for (const owner of owners) applyOwnerHeights(region, planes, owner);
    const own = edits.get(region.id);
    if (!own) return { ...region, planes };
    applyFloors(planes, own);
    return { ...region, planes, floors: withPaintedFloors(region.floors, own, catalog) };
}

// A floor edit keeps the height arrays themselves, which tells the scene that objects need no refit.
function copyPlane(plane: MapRenderPlane, editsHeights: boolean): MapRenderPlane {
    return { ...plane, cornerHeights: editsHeights ? [...plane.cornerHeights] : plane.cornerHeights, underlays: [...plane.underlays], overlays: [...plane.overlays], overlayShapes: [...plane.overlayShapes], overlayRotations: [...plane.overlayRotations] };
}

function applyOwnerHeights(region: MapRenderRegion, planes: MapRenderPlane[], owner: CornerOwner): void {
    const corners = new Set<number>();
    for (const [key, edit] of owner.edits) {
        const { x, y } = splitKey(key);
        if (edit.height !== undefined && (!owner.offsetX || !x) && (!owner.offsetY || !y)) corners.add((x + owner.offsetX) * 65 + y + owner.offsetY);
    }
    for (const corner of corners) recomputeCorner(region, planes, corner, owner.edits);
}

// Heights are stored relative to the plane below, so a change carries every plane above it.
function recomputeCorner(region: MapRenderRegion, planes: MapRenderPlane[], corner: number, edits: ReadonlyMap<number, TerrainTileEdit>): void {
    const x = Math.floor(corner / 65) % 64, y = (corner % 65) % 64;
    let newBelow = 0, oldBelow = 0;
    for (let level = 0; level < Math.min(planes.length, underwaterPlaneIndex); level++) {
        const old = region.planes[level].cornerHeights[corner];
        if (old === null) return;
        const stored = edits.get(tileKey(level, x, y))?.height;
        const next = stored === undefined ? newBelow + old - oldBelow : newBelow - stored * 32;
        planes[level].cornerHeights[corner] = next;
        if (!level) shiftUnderwater(planes, corner, next - old);
        newBelow = next;
        oldBelow = old;
    }
}

function shiftUnderwater(planes: MapRenderPlane[], corner: number, delta: number): void {
    const heights = planes[underwaterPlaneIndex]?.cornerHeights;
    const height = heights?.[corner];
    if (heights && height !== null && height !== undefined) heights[corner] = height + delta;
}

function applyFloors(planes: MapRenderPlane[], edits: ReadonlyMap<number, TerrainTileEdit>): void {
    for (const [key, edit] of edits) {
        const { plane: level, x, y } = splitKey(key);
        const plane = planes[level], tile = x * 64 + y;
        if (edit.underlay !== undefined) plane.underlays[tile] = edit.underlay;
        if (edit.overlay === undefined) continue;
        plane.overlays[tile] = edit.overlay;
        plane.overlayShapes[tile] = edit.overlay ? edit.overlayShape ?? 0 : 0;
        plane.overlayRotations[tile] = edit.overlay ? edit.overlayRotation ?? 0 : 0;
    }
}

function paintedFloorKeys(edits: Iterable<TerrainTileEdit>): Set<string> {
    const keys = new Set<string>();
    for (const edit of edits) {
        if (edit.underlay) keys.add(floorKey(false, edit.underlay));
        if (edit.overlay) keys.add(floorKey(true, edit.overlay));
    }
    return keys;
}

function withPaintedFloors(known: readonly MapFloor[], edits: ReadonlyMap<number, TerrainTileEdit>, catalog: ReadonlyMap<string, MapFloor>): MapFloor[] {
    const present = new Set(known.map(floor => floorKey(floor.overlay, floor.id)));
    return [...known, ...[...paintedFloorKeys(edits.values())].flatMap(key => present.has(key) ? [] : catalog.get(key) ?? [])];
}

/** Texture IDs of painted floors, which the scene loader cannot find in the stored terrain. */
export function paintedFloorTextures(edits: TerrainEdits | undefined, catalog: ReadonlyMap<string, MapFloor>): number[] {
    const textures = new Set<number>();
    for (const region of edits?.values() ?? []) {
        for (const key of paintedFloorKeys(region.values())) {
            const texture = catalog.get(key)?.textureId ?? -1;
            if (texture >= 0) textures.add(texture);
        }
    }
    return [...textures].sort((left, right) => left - right);
}

/** Only changes that differ from the stored region; a region with none is omitted. */
export function mapTerrainSaves(regions: readonly MapRenderRegion[], edits: TerrainEdits | undefined): MapTerrainSave[] {
    return regions.flatMap(region => {
        const tiles = [...edits?.get(region.id) ?? []].flatMap(([key, edit]) => tileChange(region, key, edit));
        return tiles.length ? [{ kind: "terrain" as const, regionId: region.id, tiles }] : [];
    });
}

function tileChange(region: MapRenderRegion, key: number, edit: TerrainTileEdit): MapTerrainTileChange[] {
    const { plane, x, y } = splitKey(key);
    const stored = region.planes[plane], tile = x * 64 + y;
    const changed = { ...heightChange(region, plane, x, y, edit), ...underlayChange(stored, tile, edit), ...overlayChange(stored, tile, edit) };
    return Object.keys(changed).length ? [{ plane, x, y, ...changed }] : [];
}

function heightChange(region: MapRenderRegion, plane: number, x: number, y: number, edit: TerrainTileEdit): Partial<MapTerrainTileChange> {
    return edit.height === undefined || edit.height === relativeHeight(region, plane, x, y) ? {} : { height: edit.height };
}

function underlayChange(stored: MapRenderPlane, tile: number, edit: TerrainTileEdit): Partial<MapTerrainTileChange> {
    return edit.underlay === undefined || edit.underlay === stored.underlays[tile] ? {} : { underlay: edit.underlay };
}

function overlayChange(stored: MapRenderPlane, tile: number, edit: TerrainTileEdit): Partial<MapTerrainTileChange> {
    if (edit.overlay === undefined || !overlayDiffers(stored, tile, edit)) return {};
    return edit.overlay ? { overlay: edit.overlay, overlayShape: edit.overlayShape ?? 0, overlayRotation: edit.overlayRotation ?? 0 } : { overlay: 0 };
}

function overlayDiffers(stored: MapRenderPlane, tile: number, edit: TerrainTileEdit): boolean {
    if (edit.overlay !== stored.overlays[tile]) return true;
    return !!edit.overlay && ((edit.overlayShape ?? 0) !== stored.overlayShapes[tile] || (edit.overlayRotation ?? 0) !== stored.overlayRotations[tile]);
}

function relativeHeight(region: MapRenderRegion, plane: number, x: number, y: number): number | null {
    const corner = x * 65 + y;
    const height = region.planes[plane]?.cornerHeights[corner] ?? null;
    const below = plane ? region.planes[plane - 1].cornerHeights[corner] : 0;
    return height === null || below === null ? null : (below - height) / 32;
}

/** The format has no value 1, and a plane cannot sit below the one under it. */
function storableHeight(value: number, direction: number): number {
    const height = Math.min(255, Math.max(0, Math.round(value)));
    if (height !== 1) return height;
    return direction > 0 ? 2 : 0;
}

/** Applies one stroke once per tile, reading heights as they were when it started. */
export function applyBrushStroke(regions: readonly MapRenderRegion[], edits: TerrainEdits | undefined, brush: TerrainBrush, points: readonly BrushPoint[]): TerrainEdits | undefined {
    const origin = worldOrigin(regions);
    if (!points.length || !origin) return edits;
    const world = points.map(point => ({ x: origin.x + point.x / 512, y: origin.y + point.z / 512 }));
    const flatten = brush.tool === "flatten" ? flattenTarget(regions, brush, world[0]) : null;
    if (brush.tool === "flatten" && flatten === null) return edits;
    const next = new Map(edits);
    let changed = false;
    for (const [wx, wy] of strokeTiles(world, brush)) {
        const region = regions.find(candidate => candidate.regionX === wx >> 6 && candidate.regionY === wy >> 6 && candidate.terrainStatus === "available");
        const patch = region && brushPatch(region, brush, wx & 63, wy & 63, flatten);
        if (!region || !patch) continue;
        const tiles = new Map(next.get(region.id));
        const key = tileKey(brush.plane, wx & 63, wy & 63);
        tiles.set(key, { ...tiles.get(key), ...patch });
        next.set(region.id, tiles);
        changed = true;
    }
    return changed ? next : edits;
}

function worldOrigin(regions: readonly MapRenderRegion[]): { x: number; y: number } | null {
    if (!regions.length) return null;
    return { x: Math.min(...regions.map(region => region.regionX)) * 64, y: Math.min(...regions.map(region => region.regionY)) * 64 };
}

function flattenTarget(regions: readonly MapRenderRegion[], brush: TerrainBrush, point: { x: number; y: number }): number | null {
    const wx = Math.round(point.x), wy = Math.round(point.y);
    const region = regions.find(candidate => candidate.regionX === wx >> 6 && candidate.regionY === wy >> 6);
    return region ? relativeHeight(region, brush.plane, wx & 63, wy & 63) : null;
}

// Drags sample once a frame, so a fast stroke is filled in between samples.
function strokeTiles(world: readonly { x: number; y: number }[], brush: TerrainBrush): [number, number][] {
    const snap = usesCorners(brush) ? Math.round : Math.floor;
    const radius = (brush.size - 1) / 2;
    const tiles = new Map<number, [number, number]>();
    const stamp = (point: { x: number; y: number }): void => {
        for (let dx = -radius; dx <= radius; dx++) for (let dy = -radius; dy <= radius; dy++) {
            const wx = snap(point.x) + dx, wy = snap(point.y) + dy;
            tiles.set(wx * 65536 + wy, [wx, wy]);
        }
    };
    world.forEach((point, index) => {
        const previous = world[index - 1] ?? point;
        const steps = Math.max(1, Math.ceil(Math.hypot(point.x - previous.x, point.y - previous.y) * 2));
        for (let step = 1; step <= steps; step++) stamp({ x: previous.x + (point.x - previous.x) * step / steps, y: previous.y + (point.y - previous.y) * step / steps });
    });
    return [...tiles.values()];
}

function brushPatch(region: MapRenderRegion, brush: TerrainBrush, x: number, y: number, flatten: number | null): TerrainTileEdit | null {
    if (usesCorners(brush)) return heightPatch(region, brush, x, y, flatten);
    if (!region.planes[brush.plane]) return null;
    if (brush.tool === "clear-overlay") return { overlay: 0 };
    if (brush.tool === "underlay") return brush.underlay === null ? null : { underlay: brush.underlay };
    return brush.overlay === null ? null : { overlay: brush.overlay, overlayShape: brush.overlayShape, overlayRotation: brush.overlayRotation };
}

function heightPatch(region: MapRenderRegion, brush: TerrainBrush, x: number, y: number, flatten: number | null): TerrainTileEdit | null {
    const current = relativeHeight(region, brush.plane, x, y);
    if (current === null) return null;
    if (brush.tool === "flatten") return { height: storableHeight(flatten ?? current, 0) };
    const direction = brush.tool === "raise" ? 1 : -1;
    return { height: storableHeight(Math.round(current) + direction * brush.strength, direction) };
}

/** A closed line around the brush at ground height, in scene units. */
export function terrainBrushOutline(regions: readonly MapRenderRegion[], brush: TerrainBrush, point: BrushPoint): number[] | null {
    const origin = worldOrigin(regions);
    if (!origin) return null;
    const corners = usesCorners(brush);
    const snap = corners ? Math.round : Math.floor;
    const radius = (brush.size - 1) / 2;
    const pad = corners ? 0.5 : 0;
    const left = snap(origin.x + point.x / 512) - radius - pad, right = snap(origin.x + point.x / 512) + radius + (corners ? pad : 1);
    const bottom = snap(origin.y + point.z / 512) - radius - pad, top = snap(origin.y + point.z / 512) + radius + (corners ? pad : 1);
    const outline: number[] = [];
    const add = (wx: number, wy: number): void => {
        const height = mapHeight(regions, brush.plane, wx, wy);
        if (height !== null) outline.push((wx - origin.x) * 512, -height, (wy - origin.y) * 512);
    };
    for (let wx = left; wx < right; wx++) add(wx, bottom);
    for (let wy = bottom; wy < top; wy++) add(right, wy);
    for (let wx = right; wx > left; wx--) add(wx, top);
    for (let wy = top; wy > bottom; wy--) add(left, wy);
    return outline.length ? outline : null;
}

/** Height strokes draw over the ground in this colour, so the raised area stands out. */
const heightPreviewRgb = 0xffb800;
/** Approximates the region lighting that the finished ground bakes into its textures. */
const litTextureTint = 0xa0a0a0;
const previewTileVertices: readonly (readonly [number, number])[] = [[0, 0], [1, 0], [0, 1], [0, 1], [1, 0], [1, 1]];

/**
 * The brushed tiles as they will look, drawn while a stroke is in progress. It does not blend floors at
 * their edges; the finished scene does that on release.
 */
export function terrainStrokePreview(regions: readonly MapRenderRegion[], brush: TerrainBrush, points: readonly BrushPoint[], floors: ReadonlyMap<string, MapFloor>): StrokePreviewGeometry | null {
    const origin = worldOrigin(regions);
    const stroke = applyBrushStroke(regions, undefined, brush, points);
    if (!origin || !stroke) return null;
    const after = applyTerrainEdits(regions, stroke, floors);
    const texture = paintedFloor(brush, floors);
    const preview: StrokePreviewGeometry = { positions: [], rgb: [], uvs: [], textureId: texture && texture.textureId >= 0 ? texture.textureId : null, opacity: usesCorners(brush) ? 0.45 : 1 };
    for (const [wx, wy] of previewTiles(regions, stroke, brush)) addPreviewTile(preview, after, brush, origin, wx, wy, texture);
    return preview.positions.length ? preview : null;
}

function paintedFloor(brush: TerrainBrush, floors: ReadonlyMap<string, MapFloor>): MapFloor | undefined {
    if (brush.tool === "underlay") return floors.get(floorKey(false, brush.underlay ?? 0));
    return brush.tool === "overlay" ? floors.get(floorKey(true, brush.overlay ?? 0)) : undefined;
}

// A height edit moves a corner, which every tile sharing that corner must show.
function previewTiles(regions: readonly MapRenderRegion[], stroke: TerrainEdits, brush: TerrainBrush): [number, number][] {
    const tiles = new Map<number, [number, number]>();
    const reach = usesCorners(brush) ? [-1, 0] : [0];
    for (const region of regions) {
        for (const key of stroke.get(region.id)?.keys() ?? []) {
            const { x, y } = splitKey(key);
            for (const dx of reach) for (const dy of reach) {
                const wx = region.regionX * 64 + x + dx, wy = region.regionY * 64 + y + dy;
                tiles.set(wx * 65536 + wy, [wx, wy]);
            }
        }
    }
    return [...tiles.values()];
}

function addPreviewTile(preview: StrokePreviewGeometry, regions: readonly MapRenderRegion[], brush: TerrainBrush, origin: { x: number; y: number }, wx: number, wy: number, floor: MapFloor | undefined): void {
    const heights = previewTileVertices.map(([dx, dy]) => mapHeight(regions, brush.plane, wx + dx, wy + dy));
    if (heights.includes(null)) return;
    const scale = (floor?.textureScale ?? 512) / 512;
    previewTileVertices.forEach(([dx, dy], corner) => {
        preview.positions.push((wx + dx - origin.x) * 512, -(heights[corner] ?? 0), (wy + dy - origin.y) * 512);
        preview.uvs.push((wx + dx) / scale, -(wy + dy) / scale);
    });
    preview.rgb.push(previewTileRgb(regions, brush, wx, wy, floor));
}

function previewTileRgb(regions: readonly MapRenderRegion[], brush: TerrainBrush, wx: number, wy: number, floor: MapFloor | undefined): number {
    if (usesCorners(brush)) return heightPreviewRgb;
    if (floor) return floor.textureId >= 0 ? litTextureTint : floor.rgb ?? 0;
    const region = regions.find(candidate => candidate.regionX === wx >> 6 && candidate.regionY === wy >> 6);
    const underlay = region?.planes[brush.plane]?.underlays[(wx & 63) * 64 + (wy & 63)];
    return region?.floors.find(candidate => !candidate.overlay && candidate.id === underlay)?.rgb ?? 0x808080;
}

/** What a terrain save leaves as it was, phrased for the save confirmation. */
export function terrainEditNotes(edits: TerrainEdits | undefined): string[] {
    for (const region of edits?.values() ?? []) for (const edit of region.values()) if (edit.height !== undefined) return ["Walkability and other tile flags are not changed."];
    return [];
}
