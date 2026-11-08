import { modelFaceWinding, prepareCacheModel, type CacheModelGeometry } from "../../../../../shared/model-viewer/cache-model-geometry";
import type { ModelGeometry, ModelTexture } from "../../../../../shared/model-viewer/model-scene";
import { mapAppearanceKey, underwaterPlaneIndex, type MapAtmosphere, type MapLandscapeSave, type MapPlacement, type MapRenderRegion, type ObjectRenderAppearance } from "../services/map-render.models";
import { terrainSampler, linearFloorColor, isVisibleFloor, type VisibleFloor, type FloorSample } from "./terrain-blending";
import { mixWaterFog, waterFogFactor, waterFogLookup, type WaterFog } from "./underwater-fog";
import { overlayPriority, overlaySample, overlaySpreads, spreadSample, type SpreadLookup } from "./overlay-spreading";
import type { TerrainBlend } from "../../../../../shared/model-viewer/terrain-material";
import type { TerrainEdits } from "./map-terrain-edit";

// Revision-742 ordinary floor topology, Class274's floor vertex and triangle tables.
const floorX = [0, 256, 512, 512, 512, 256, 0, 0, 128, 256, 128, 384, 256];
const floorY = [0, 0, 0, 256, 512, 512, 512, 256, 256, 384, 128, 128, 256];
const a = [
    [0, 2, 4, 6],
    [6, 0, 2, 4],
    [6, 0, 2],
    [2, 6, 0],
    [0, 2, 6],
    [6, 0, 2],
    [5, 6, 0, 1, 2, 4],
    [7, 2, 4, 4],
    [2, 4, 4, 7],
    [6, 6, 4, 0, 2, 2],
    [0, 2, 2, 6, 6, 4],
    [0, 2, 2, 4, 6, 6],
    [0, 2, 4, 6],
];
const b = [
    [2, 4, 6, 0],
    [0, 2, 4, 6],
    [0, 2, 4],
    [4, 0, 2],
    [2, 4, 0],
    [0, 2, 4],
    [6, 0, 1, 2, 4, 5],
    [0, 4, 7, 6],
    [4, 7, 6, 0],
    [0, 8, 6, 2, 9, 4],
    [2, 9, 4, 0, 8, 6],
    [2, 11, 4, 6, 10, 0],
    [2, 4, 6, 0],
];
const c = [
    [12, 12, 12, 12],
    [12, 12, 12, 12],
    [5, 5, 5],
    [5, 5, 5],
    [5, 5, 5],
    [5, 5, 5],
    [12, 12, 12, 12, 12, 12],
    [1, 1, 1, 7],
    [1, 1, 7, 1],
    [8, 9, 9, 8, 8, 9],
    [8, 8, 9, 8, 9, 9],
    [10, 10, 11, 11, 11, 10],
    [12, 12, 12, 12],
];
const overlayCount = [4, 2, 1, 1, 2, 2, 3, 1, 3, 3, 3, 2, 0];

export function mapHeight(regions: readonly MapRenderRegion[], plane: number, worldX: number, worldY: number): number | null {
    for (const region of regions) {
        const x = worldX - region.regionX * 64,
            y = worldY - region.regionY * 64;
        const surface = region.planes[plane];
        if (!surface || x < 0 || y < 0 || x > 64 || y > 64) continue;
        const tx = Math.min(Math.floor(x), 63),
            ty = Math.min(Math.floor(y), 63);
        const heights = [
            surface.cornerHeights[tx * 65 + ty],
            surface.cornerHeights[(tx + 1) * 65 + ty],
            surface.cornerHeights[tx * 65 + ty + 1],
            surface.cornerHeights[(tx + 1) * 65 + ty + 1],
        ];
        if (heights.some((height) => height === null || height === undefined)) continue;
        const [h00, h10, h01, h11] = heights as number[];
        const fx = (x - tx) * 512,
            fy = (y - ty) * 512;
        const low = Math.floor(((512 - fx) * h00 + fx * h10) / 512);
        const high = Math.floor(((512 - fx) * h01 + fx * h11) / 512);
        return Math.floor(((512 - fy) * low + fy * high) / 512);
    }
    return null;
}

/** Editor visibility choices; the default draws everything. */
export interface MapSceneFilter {
    terrain: boolean;
    waterSurface: boolean;
    hiddenShapes: ReadonlySet<number>;
    hiddenPlacements: ReadonlySet<MapPlacement>;
}
const showEverything: MapSceneFilter = { terrain: true, waterSurface: true, hiddenShapes: new Set(), hiddenPlacements: new Set() };

export interface MapObjectRange {
    regionId: number;
    placement: MapPlacement;
    startVertex: number;
    endVertex: number;
}

export interface MapObjectTransform {
    x: number;
    y: number;
    plane: number;
    rotation: number;
    scale: number;
}

export interface MapSceneDraft {
    transforms: ReadonlyMap<MapPlacement, MapObjectTransform | null>;
    additions: readonly Pick<MapObjectRange, "regionId" | "placement">[];
    terrain?: TerrainEdits;
}

/** Each region whose surface landscape the draft would change, with its complete resulting list. Scale has no stored form and underwater objects are a separate archive. */
export function mapLandscapeSaves(regions: readonly MapRenderRegion[], draft: MapSceneDraft | undefined): MapLandscapeSave[] {
    if (!draft) return [];
    return regions.flatMap(region => {
        const added = draft.additions.filter(addition => addition.regionId === region.id).map(addition => addition.placement);
        const surface = region.objects.filter(isSurfacePlacement);
        const objects = [...surface, ...added.filter(isSurfacePlacement)].flatMap(placement => {
            const transform = draft.transforms.get(placement);
            if (transform === null) return [];
            const { id, shape } = placement;
            const { rotation, x, y, plane } = transform ?? placement;
            return [{ id, shape, rotation, x, y, plane }];
        });
        const unchanged = objects.length === surface.length && objects.every((object, index) => samePlacement(object, surface[index]));
        return unchanged ? [] : [{ kind: "landscape" as const, regionId: region.id, objects }];
    });
}

/** What the draft changes that saving cannot store, phrased for the save confirmation. */
export function unstoredEditNotes(draft: MapSceneDraft | undefined): string[] {
    if (!draft) return [];
    const transforms = [...draft.transforms];
    const scaled = transforms.some(([, transform]) => transform !== null && transform.scale !== 1);
    const underwater = [...transforms.map(([placement]) => placement), ...draft.additions.map(addition => addition.placement)].some(placement => !isSurfacePlacement(placement));
    return [...scaled ? ["Scale changes are not stored."] : [], ...underwater ? ["Underwater object changes are not stored."] : []];
}

function isSurfacePlacement(placement: MapPlacement): boolean {
    return placement.plane !== underwaterPlaneIndex;
}

function samePlacement(left: MapPlacement, right: MapPlacement): boolean {
    return left.id === right.id && left.shape === right.shape && left.rotation === right.rotation && left.x === right.x && left.y === right.y && left.plane === right.plane;
}

export interface MapSceneObjects extends ModelGeometry {
    placements: readonly MapObjectRange[];
}

/** Terrain and objects build and draw separately; placement ranges are relative to the objects. */
export interface MapSceneGeometry {
    terrain: ModelGeometry | null;
    objects: MapSceneObjects | null;
    placements: readonly MapObjectRange[];
}

type SceneObjects = { geometry: MapSceneObjects | null; warnings: string[] };
type CornerHeights = readonly (number | null)[];

type SceneGeometry = Omit<MapSceneObjects, "uvs"> & {
    placements: MapObjectRange[];
    firstTexelVertices: number[];
    positions: number[];
    colors: number[];
    opacities: number[];
    uvs: number[];
    textureIds: number[];
    facePriorities: number[];
    preparedLighting: boolean;
    terrainBlends: TerrainBlend[];
    terrainWeights: number[];
};
type SceneContext = {
    regions: readonly MapRenderRegion[];
    appearances: ReadonlyMap<string, ObjectRenderAppearance>;
    textures: ReadonlyMap<number, ModelTexture>;
    visiblePlanes: readonly boolean[];
    geometry: SceneGeometry;
    warnings: Set<string>;
    originX: number;
    originY: number;
    sampleTerrain: ReturnType<typeof terrainSampler>;
    spreads: SpreadLookup;
    filter: MapSceneFilter;
    waterFog: (worldX: number, worldY: number) => WaterFog | undefined;
    blends: Map<string, TerrainBlend>;
    preparation: MapScenePreparation;
    atmosphere?: MapAtmosphere;
    lightTint?: readonly number[];
};
type ScenePoint = { x: number; y: number; z: number };

interface PreparedMapPart {
    positions: Float64Array;
    positionIndices?: Uint16Array | Uint32Array;
    indices: Uint16Array | Uint32Array;
    colors?: Float64Array;
    opacities?: Float64Array;
    uvs?: Float64Array;
    textureIds?: Int32Array;
    firstTexel?: Uint8Array;
    facePriorities?: readonly number[];
    maxHeight: number;
}

function packMapPart(source: Pick<CacheModelGeometry, "positions" | "indices" | "faceColors">, prepared: ModelGeometry): PreparedMapPart {
    let positions: Float64Array;
    let positionIndices: PreparedMapPart["positionIndices"];
    if (source.faceColors && source.positions.length < prepared.positions.length) {
        const vertexCount = source.positions.length / 3;
        const remap = new Int32Array(vertexCount).fill(-1);
        const mapping = vertexCount <= 65536 ? new Uint16Array(prepared.indices.length) : new Uint32Array(prepared.indices.length);
        const compact: number[] = [];
        let expandedVertex = 0;
        for (let face = 0; face < source.indices.length; face += 3) {
            for (const corner of modelFaceWinding) {
                const original = source.indices[face + corner];
                if (remap[original] === -1) {
                    remap[original] = compact.length / 3;
                    const offset = expandedVertex * 3;
                    compact.push(prepared.positions[offset], prepared.positions[offset + 1], prepared.positions[offset + 2]);
                }
                mapping[expandedVertex++] = remap[original];
            }
        }
        positions = Float64Array.from(compact);
        positionIndices = mapping;
    } else positions = Float64Array.from(prepared.positions);
    let maxHeight = 1;
    for (let vertex = 1; vertex < positions.length; vertex += 3) maxHeight = Math.max(maxHeight, positions[vertex]);
    const indices = prepared.positions.length / 3 <= 65536 ? Uint16Array.from(prepared.indices) : Uint32Array.from(prepared.indices);
    return {
        positions, positionIndices, indices, maxHeight,
        colors: prepared.colors && Float64Array.from(prepared.colors),
        opacities: prepared.opacities && Float64Array.from(prepared.opacities),
        uvs: prepared.uvs && Float64Array.from(prepared.uvs),
        textureIds: prepared.textureIds && Int32Array.from(prepared.textureIds),
        firstTexel: prepared.firstTexel && Uint8Array.from(prepared.firstTexel),
        facePriorities: prepared.facePriorities,
    };
}

/** Selection-scoped reuse; no global model/terrain cache. */
export class MapScenePreparation {
    private ground?: { regions: readonly MapRenderRegion[]; planes: string; modes: string[]; atmosphere?: MapAtmosphere; geometry: SceneGeometry };
    private sampling?: { regions: readonly MapRenderRegion[]; sample: ReturnType<typeof terrainSampler> };
    private readonly models = new WeakMap<ObjectRenderAppearance, { dependencies: { id: number; prepared: boolean }[]; modes: string[]; atmosphere?: MapAtmosphere; parts: PreparedMapPart[] }>();
    private objects?: { inputs: readonly unknown[]; heights: readonly CornerHeights[]; result: SceneObjects };

    terrain(regions: readonly MapRenderRegion[], textures: ReadonlyMap<number, ModelTexture>, planes: readonly boolean[], filter: MapSceneFilter, atmosphere: MapAtmosphere | undefined, build: () => SceneGeometry): SceneGeometry {
        const modes = regions.flatMap(region => region.floors.map(floor => {
            const texture = textures.get(floor.textureId);
            return `${texture?.alphaMode ?? 0}:${texture?.effectId ?? 0}`;
        }));
        const key = `${planes.join()}:${filter.terrain}:${filter.waterSurface}`;
        if (this.ground?.regions === regions && this.ground.planes === key && this.ground.atmosphere === atmosphere && sameValues(this.ground.modes, modes)) return this.ground.geometry;
        const geometry = build();
        this.ground = { regions, planes: key, modes, atmosphere, geometry };
        return geometry;
    }

    sampleTerrain(regions: readonly MapRenderRegion[]): ReturnType<typeof terrainSampler> {
        if (this.sampling?.regions !== regions) this.sampling = { regions, sample: terrainSampler(regions) };
        return this.sampling.sample;
    }

    appearance(appearance: ObjectRenderAppearance, textures: ReadonlyMap<number, ModelTexture>, atmosphere?: MapAtmosphere): PreparedMapPart[] {
        const previous = this.models.get(appearance);
        const dependencies = previous?.dependencies ?? appearance.parts.flatMap(part => part.faceColors ? [...new Set(part.faceTextureIds ?? [])]
            .filter(id => id >= 0)
            .map(id => ({ id, prepared: part.ambient !== undefined || part.faceShadingTypes !== undefined })) : []);
        const modes = dependencies.map(({ id, prepared }) => {
            const material = textures.get(id);
            // Prepared lighting already applies neutral defaults before an image arrives.
            // Legacy unprepared colours still use white while material metadata is absent.
            return `${!prepared && !material ? "missing" : "present"}:${material?.colorBlend ?? 0}:${material?.brightness ?? 0}:${prepared && material?.effectId === 6}`;
        });
        if (previous && previous.atmosphere === atmosphere && sameValues(previous.modes, modes)) return previous.parts;
        const parts = appearance.parts.map(part => packMapPart(part, prepareCacheModel({ ...part, particles: [] }, textures, atmosphere)));
        this.models.set(appearance, { dependencies, modes, atmosphere, parts });
        return parts;
    }

    /** Floors do not affect objects, so only their inputs and the ground heights invalidate them. */
    landscape(regions: readonly MapRenderRegion[], planes: readonly boolean[], appearances: ReadonlyMap<string, ObjectRenderAppearance>, textures: ReadonlyMap<number, ModelTexture>, atmosphere: MapAtmosphere | undefined, draft: MapSceneDraft | undefined, filter: MapSceneFilter, build: () => SceneObjects, refit: (previous: SceneObjects, changed: ReadonlySet<number>) => SceneObjects | null): SceneObjects {
        const parts = [...appearances.values()].flatMap(appearance => this.appearance(appearance, textures, atmosphere));
        const inputs = objectInputs(regions, planes, appearances, draft, filter, parts);
        const heights = regions.flatMap(region => region.planes.map(plane => plane.cornerHeights));
        const previous = this.objects && sameValues(this.objects.inputs, inputs) ? this.objects : undefined;
        if (previous && sameValues(previous.heights, heights)) return previous.result;
        const result = (previous && refit(previous.result, changedCorners(regions, previous.heights))) ?? build();
        this.objects = { inputs, heights, result };
        return result;
    }
}

// An empty edit list means the same as none, and a terrain stroke creates one.
function objectInputs(regions: readonly MapRenderRegion[], planes: readonly boolean[], appearances: ReadonlyMap<string, ObjectRenderAppearance>, draft: MapSceneDraft | undefined, filter: MapSceneFilter, parts: readonly PreparedMapPart[]): readonly unknown[] {
    return [planes.join(), appearances, draft?.transforms.size ? draft.transforms : undefined, draft?.additions.length ? draft.additions : undefined, filter, ...regions.map(region => region.objects), ...parts];
}

const worldCorner = (x: number, y: number): number => x * 65536 + y;

/** World corners whose height differs on any plane. */
function changedCorners(regions: readonly MapRenderRegion[], previous: readonly CornerHeights[]): Set<number> {
    const changed = new Set<number>();
    let plane = 0;
    for (const region of regions) {
        for (const { cornerHeights } of region.planes) {
            const before = previous[plane++];
            if (before === cornerHeights) continue;
            for (let corner = 0; corner < cornerHeights.length; corner++)
                if (before[corner] !== cornerHeights[corner]) changed.add(worldCorner(region.regionX * 64 + Math.floor(corner / 65), region.regionY * 64 + corner % 65));
        }
    }
    return changed;
}

function sameValues<T>(left: readonly T[], right: readonly T[]): boolean {
    return left.length === right.length && left.every((value, index) => value === right[index]);
}

function emptySceneGeometry(): SceneGeometry {
    return { placements: [], firstTexelVertices: [], positions: [], indices: [], colors: [], opacities: [], uvs: [], textureIds: [], facePriorities: [], preparedLighting: true, terrainBlends: [], terrainWeights: [] };
}

function buildObjects(context: SceneContext, draft: MapSceneDraft | undefined): SceneObjects {
    context.geometry = emptySceneGeometry();
    addLandscape(context, draft);
    const { firstTexelVertices, colors, opacities, uvs, ...attributes } = context.geometry;
    const firstTexel = firstTexelVertices.length ? new Uint8Array(opacities.length) : undefined;
    if (firstTexel) for (const vertex of firstTexelVertices) firstTexel[vertex] = 1;
    const vertexRgba = new Float32Array(opacities.length * 4);
    for (let vertex = 0; vertex < opacities.length; vertex++) {
        const rgb = vertex * 3, rgba = vertex * 4;
        vertexRgba[rgba] = colors[rgb];
        vertexRgba[rgba + 1] = colors[rgb + 1];
        vertexRgba[rgba + 2] = colors[rgb + 2];
        vertexRgba[rgba + 3] = opacities[vertex];
    }
    return { geometry: { ...attributes, indices: sequentialIndices(opacities.length), firstTexel, vertexRgba, uvs: Float32Array.from(uvs) }, warnings: [...context.warnings] };
}

/** Moves only the objects on changed ground. Null means an object's vertices changed, so a full build is needed. */
function refitObjects(context: SceneContext, draft: MapSceneDraft | undefined, previous: SceneObjects, changed: ReadonlySet<number>): SceneObjects | null {
    const geometry = previous.geometry;
    if (!geometry) return null;
    const positions = geometry.positions.slice();
    const ranges: [number, number][] = [];
    const prepared = new Map<string, PreparedMapPart[]>();
    for (const range of geometry.placements) {
        const region = context.regions.find(candidate => candidate.id === range.regionId);
        if (!region || !touchesChangedGround(context, region, range.placement, draft, changed)) continue;
        context.geometry = emptySceneGeometry();
        addVisiblePlacement(context, region, range.placement, prepared, draft);
        const fresh = context.geometry.positions;
        if (fresh.length !== (range.endVertex - range.startVertex) * 3) return null;
        for (let value = 0; value < fresh.length; value++) positions[range.startVertex * 3 + value] = fresh[value];
        ranges.push([range.startVertex, range.endVertex]);
    }
    if (!ranges.length) return previous;
    // Only the newest patch is needed, so older geometry can be released.
    geometry.patchOf = undefined;
    return { geometry: { ...geometry, positions, patchOf: { base: geometry, ranges } }, warnings: previous.warnings };
}

function touchesChangedGround(context: SceneContext, region: MapRenderRegion, source: MapPlacement, draft: MapSceneDraft | undefined, changed: ReadonlySet<number>): boolean {
    const placement = draft?.transforms.get(source) ?? source;
    const appearance = context.appearances.get(mapAppearanceKey(source));
    const size = Math.max(appearance?.sizeX ?? 1, appearance?.sizeY ?? 1);
    // Contoured models can overhang their footprint a little.
    const reach = 2;
    const left = region.regionX * 64 + placement.x - reach, bottom = region.regionY * 64 + placement.y - reach;
    for (let x = left; x <= left + size + reach * 2; x++) for (let y = bottom; y <= bottom + size + reach * 2; y++) if (changed.has(worldCorner(x, y))) return true;
    return false;
}

export function buildMapScene(
    regions: readonly MapRenderRegion[],
    appearances: ReadonlyMap<string, ObjectRenderAppearance>,
    textures: ReadonlyMap<number, ModelTexture>,
    visiblePlanes: readonly boolean[],
    landscape: boolean,
    preparation = new MapScenePreparation(),
    atmosphereRegionId: number | null = regions[0]?.id ?? null,
    draft?: MapSceneDraft,
    filter: MapSceneFilter = showEverything,
): { geometry: MapSceneGeometry | null; warnings: string[] } {
    if (!regions.length) return { geometry: null, warnings: [] };
    const atmosphere = regions.find(region => region.id === atmosphereRegionId)?.atmosphere ?? undefined;
    const lightTint = atmosphere && [16, 8, 0].map(shift => {
        const value = ((atmosphere.lightRgb >> shift) & 255) / 255;
        const linear = value <= 0.04045 ? value / 12.92 : ((value + 0.055) / 1.055) ** 2.4;
        return linear * (atmosphere.intensity / 1.1523438);
    });
    const geometry = emptySceneGeometry();
    let sampler: ReturnType<typeof terrainSampler> | undefined;
    let spreads: SpreadLookup | undefined;
    let waterFog: ReturnType<typeof waterFogLookup> | undefined;
    const context: SceneContext = {
        regions,
        appearances,
        textures,
        visiblePlanes,
        geometry,
        warnings: new Set(),
        originX: Math.min(...regions.map((region) => region.regionX)) * 64,
        originY: Math.min(...regions.map((region) => region.regionY)) * 64,
        sampleTerrain: (...args) => { sampler ??= preparation.sampleTerrain(regions); return sampler(...args); },
        filter,
        spreads: (...args) => { spreads ??= overlaySpreads(regions); return spreads(...args); },
        waterFog: (...args) => { waterFog ??= waterFogLookup(regions); return waterFog(...args); },
        blends: new Map(),
        preparation,
        atmosphere,
        lightTint,
    };
    const terrain = preparation.terrain(regions, textures, visiblePlanes, filter, atmosphere, () => {
        if (filter.terrain) addTerrain(context);
        context.geometry.indices = sequentialIndices(context.geometry.opacities.length);
        return context.geometry;
    });
    const objects = landscape
        ? preparation.landscape(regions, visiblePlanes, appearances, textures, atmosphere, draft, filter, () => buildObjects(context, draft), (previous, changed) => refitObjects(context, draft, previous, changed))
        : { geometry: null, warnings: [...context.warnings] };
    const drawnTerrain = terrain.positions.length ? terrain : null;
    const drawnObjects = objects.geometry?.positions.length ? objects.geometry : null;
    if (!drawnTerrain && !drawnObjects) return { geometry: null, warnings: objects.warnings };
    return { geometry: { terrain: drawnTerrain, objects: drawnObjects, placements: drawnObjects?.placements ?? [] }, warnings: objects.warnings };
}

function sequentialIndices(count: number): Uint16Array | Uint32Array {
    // The maximum 16-bit index is reserved for primitive restart.
    const indices = count > 65535 ? new Uint32Array(count) : new Uint16Array(count);
    for (let index = 0; index < count; index++) indices[index] = index;
    return indices;
}

function addVertex(context: SceneContext, point: ScenePoint, color: readonly number[], opacity: number, uv: readonly number[], weights: readonly number[] | null = [], firstTexel = 0): void {
    const output = context.geometry;
    output.positions.push((point.x - context.originX) * 512, point.y, (point.z - context.originY) * 512);
    const light = context.lightTint;
    if (light) output.colors.push(color[0] * light[0], color[1] * light[1], color[2] * light[2]);
    else output.colors.push(color[0], color[1], color[2]);
    if (firstTexel) output.firstTexelVertices.push(output.opacities.length);
    output.opacities.push(opacity);
    output.uvs.push(uv[0], uv[1]);
    // Only the terrain blend shader reads these attributes.
    if (weights) for (let layer = 0; layer < 8; layer++) output.terrainWeights.push(weights[layer] ?? 0);
}

function addTerrain(context: SceneContext): void {
    for (const region of context.regions) {
        const floors = new Map(region.floors.filter(isVisibleFloor).map((floor) => [`${floor.overlay}:${floor.id}`, floor]));
        region.planes.forEach((_, level) => {
            if (!context.visiblePlanes[level]) return;
            for (let x = 0; x < 64; x++) for (let y = 0; y < 64; y++) addFloorTile(context, region, floors, level, x, y);
        });
    }
}

function addFloorTile(context: SceneContext, region: MapRenderRegion, floors: ReadonlyMap<string, VisibleFloor>, level: number, x: number, y: number): void {
    const plane = region.planes[level],
        tile = x * 64 + y;
    if (!plane.underlays[tile] && !plane.overlays[tile]) return;
    const shape = plane.overlays[tile] ? plane.overlayShapes[tile] : 12;
    const rotation = plane.overlayRotations[tile];
    for (let triangle = 0; triangle < a[shape].length; triangle++) {
        addFloorTriangle(context, region, floors, level, x, y, shape, rotation, tile, triangle);
    }
}

function addFloorTriangle(
    context: SceneContext,
    region: MapRenderRegion,
    floors: ReadonlyMap<string, VisibleFloor>,
    level: number,
    x: number,
    y: number,
    shape: number,
    rotation: number,
    tile: number,
    triangle: number
): void {
    const plane = region.planes[level];
    const overlay = triangle < overlayCount[shape];
    const floor = floors.get(`${overlay}:${overlay ? plane.overlays[tile] : plane.underlays[tile]}`);
    if (!floor || isHiddenWater(context, level, floor)) return;
    const corners = [a[shape][triangle], c[shape][triangle], b[shape][triangle]].map((point) => {
        const [rx, ry] = rotateFloor(floorX[point], floorY[point], rotation);
        const wx = region.regionX * 64 + x + rx / 512,
            wy = region.regionY * 64 + y + ry / 512;
        return { wx, wy, point, height: mapHeight([region], level, wx, wy) };
    });
    const available = corners.filter((corner): corner is FloorCorner => corner.height !== null);
    if (available.length !== 3) return;
    const spreads = context.spreads(level, region.regionX * 64 + x, region.regionY * 64 + y);
    const minimumPriority = overlay ? overlayPriority(floor) : -1;
    const cornerSamples = available.map(corner => {
        const spread = spreadSample(spreads, corner.point, rotation, minimumPriority);
        return spread ? [spread] : overlay ? [overlaySample(floor)] : context.sampleTerrain(level, corner.wx, corner.wy);
    });
    const shade = floorShade(context, level, region.regionX * 64 + x, region.regionY * 64 + y, available);
    if (shade.flat) {
        addPlainFloor(context, -1, floor.textureScale, available, () => shade.flat as readonly number[]);
        return;
    }
    if (addLayeredFloor(context, available, cornerSamples, shade.apply)) return;
    // Floor textures modulate the client palette tint, not raw definition RGB or white.
    addPlainFloor(context, floor.textureId, floor.textureScale, available, index => {
        const samples = cornerSamples[index];
        return shade.apply(samples.length ? blendedFloorColor(samples) : linearFloorColor(floor.rgb), index);
    });
}

// The client's water material effects: water, underwater, lake, HD water and HD water wave.
const waterEffects = [2, 4, 5, 8, 9];

function isHiddenWater(context: SceneContext, level: number, floor: VisibleFloor): boolean {
    return !context.filter.waterSurface && level !== underwaterPlaneIndex && waterEffects.includes(context.textures.get(floor.textureId)?.effectId ?? 0);
}

function addPlainFloor(context: SceneContext, texture: number, textureScale: number, corners: readonly FloorCorner[], tint: (index: number) => readonly number[]): void {
    context.geometry.textureIds.push(texture);
    context.geometry.facePriorities.push(0);
    for (const [index, corner] of corners.entries()) {
        addVertex(context, { x: corner.wx, y: -corner.height, z: corner.wy }, tint(index), 1, [
            (corner.wx * 512) / textureScale,
            (-corner.wy * 512) / textureScale,
        ]);
    }
}

interface FloorShade {
    /** The fog hides the floor entirely, texture included. */
    flat?: readonly number[];
    apply: (color: readonly number[], index: number) => readonly number[];
}
const noShade: FloorShade = { apply: color => color };

function floorShade(context: SceneContext, level: number, worldX: number, worldY: number, corners: readonly FloorCorner[]): FloorShade {
    const fog = level === underwaterPlaneIndex ? context.waterFog(worldX, worldY) : undefined;
    if (!fog) return noShade;
    const factors = corners.map(corner => waterFogFactor(fog, corner.height - (mapHeight(context.regions, 0, corner.wx, corner.wy) ?? corner.height)));
    return { flat: factors.every(factor => factor >= 1) ? fog.color : undefined, apply: (color, index) => mixWaterFog(color, fog, factors[index]) };
}

type FloorCorner = { wx: number; wy: number; point: number; height: number };

function addLayeredFloor(context: SceneContext, corners: readonly FloorCorner[], samples: readonly (readonly FloorSample[])[], shade: FloorShade["apply"]): boolean {
    if (samples.some(sample => !sample.length)) return false;
    const layers = new Map(samples.flat().map(sample => [sample.key, sample.layer]));
    if (layers.size < 2) return false;
    // Special/transparent floors keep their existing material/effect path.
    if ([...layers.values()].some(layer => {
        const material = context.textures.get(layer.textureId);
        return material && (material.alphaMode !== 0 || (material.effectId ?? 0) !== 0);
    })) return false;
    const ordered = [...layers].sort(([left], [right]) => left.localeCompare(right));
    const keys = ordered.map(([key]) => key);
    const key = keys.join("|");
    let blend = context.blends.get(key);
    if (!blend) {
        blend = { id: -2 - context.blends.size, layers: ordered.map(([, layer]) => layer) };
        context.blends.set(key, blend);
        context.geometry.terrainBlends.push(blend);
    }
    context.geometry.textureIds.push(blend.id);
    context.geometry.facePriorities.push(0);
    corners.forEach((corner, index) => {
        const weights = keys.map(key => samples[index].find(sample => sample.key === key)?.weight ?? 0);
        const color = shade(blendedFloorColor(samples[index]), index);
        addVertex(context, { x: corner.wx, y: -corner.height, z: corner.wy }, color, 1, [corner.wx * 512, -corner.wy * 512], weights);
    });
    return true;
}

function blendedFloorColor(samples: readonly FloorSample[]): number[] {
    return [0, 1, 2].map(channel => samples.reduce((sum, sample) => sum + sample.layer.color[channel] * sample.weight, 0));
}

function addLandscape(context: SceneContext, draft?: MapSceneDraft): void {
    const prepared = new Map<string, PreparedMapPart[]>();
    for (const region of context.regions)
        for (const placement of region.objects) {
            addVisiblePlacement(context, region, placement, prepared, draft);
        }
    for (const { regionId, placement } of draft?.additions ?? []) {
        const region = context.regions.find(candidate => candidate.id === regionId);
        if (region) addVisiblePlacement(context, region, placement, prepared, draft);
    }
}

function addVisiblePlacement(context: SceneContext, region: MapRenderRegion, placement: MapPlacement, prepared: Map<string, PreparedMapPart[]>, draft?: MapSceneDraft): void {
    if (context.filter.hiddenPlacements.has(placement) || context.filter.hiddenShapes.has(placement.shape)) return;
    addPlacement(context, region, placement, prepared, draft?.transforms.get(placement));
}

function addPlacement(context: SceneContext, region: MapRenderRegion, source: MapPlacement, prepared: Map<string, PreparedMapPart[]>, transform?: MapObjectTransform | null): void {
    if (transform === null) return;
    // Rotation selects the server-rotated appearance, so footprint and decor offsets follow it.
    let placement = transform ? { ...source, x: transform.x, y: transform.y, plane: transform.plane, rotation: transform.rotation } : source;
    if (!context.visiblePlanes[placement.plane]) return;
    let key = mapAppearanceKey(placement),
        appearance = context.appearances.get(key);
    // Keep an edited object drawn (and selectable) at its source rotation until the rotated appearance arrives.
    if (!appearance && placement.rotation !== source.rotation) {
        placement = { ...placement, rotation: source.rotation };
        key = mapAppearanceKey(placement);
        appearance = context.appearances.get(key);
    }
    if (!appearance) return;
    if (appearance.contourMode > 2) {
        context.warnings.add(`Object ${placement.id}: unsupported contour mode ${appearance.contourMode}.`);
        return;
    }
    const parts = prepared.get(key) ?? context.preparation.appearance(appearance, context.textures, context.atmosphere);
    prepared.set(key, parts);
    const center = placementCenter(region, placement, appearance);
    const base = mapHeight(context.regions, placement.plane, center[0], center[1]);
    if (base === null) {
        context.warnings.add(`Region ${region.id}: some objects require unavailable terrain heights.`);
        return;
    }
    const startVertex = context.geometry.positions.length / 3;
    for (const [partIndex, part] of parts.entries()) {
        const projected = projectPart(context, placement, appearance, part, center, base, transform);
        if (!projected) {
            context.warnings.add(`Region ${region.id}: some contoured objects require unavailable boundary data.`);
            continue;
        }
        const offset = decorationOffset(region, placement, context.appearances, partIndex);
        appendPart(context, part, projected, offset);
    }
    const endVertex = context.geometry.positions.length / 3;
    if (endVertex > startVertex) context.geometry.placements.push({ regionId: region.id, placement: source, startVertex, endVertex });
}

function projectPart(
    context: SceneContext,
    placement: MapPlacement,
    appearance: ObjectRenderAppearance,
    part: PreparedMapPart,
    center: readonly number[],
    base: number,
    transform?: MapObjectTransform,
): ScenePoint[] | null {
    const projected: ScenePoint[] = [];
    const scale = transform?.scale ?? 1;
    for (let index = 0; index < part.positions.length; index += 3) {
        const x = center[0] + part.positions[index] * scale / 512,
            y = center[1] + part.positions[index + 2] * scale / 512;
        const up = part.positions[index + 1];
        const height = appearance.contourMode ? mapHeight(context.regions, placement.plane, x, y) : base;
        if (height === null && !transform) return null;
        let delta = base - (height ?? base);
        if (appearance.contourMode === 2) {
            const fraction = Math.trunc((up * 65536) / part.maxHeight);
            delta = fraction < appearance.contourParameter ? Math.trunc((delta * (appearance.contourParameter - fraction)) / appearance.contourParameter) : 0;
        }
        projected.push({ x, y: up * scale - base + delta, z: y });
    }
    return projected;
}

function appendPart(context: SceneContext, part: PreparedMapPart, projected: readonly ScenePoint[], offset: readonly number[]): void {
    appendFaceData(context, part);
    const vertex: ScenePoint = { x: 0, y: 0, z: 0 };
    const uv = [0, 0], color = [1, 1, 1];
    const offsetX = offset[0] / 512, offsetZ = offset[1] / 512;
    for (const source of part.indices) {
        const point = projected[part.positionIndices?.[source] ?? source];
        vertex.x = point.x + offsetX;
        vertex.y = point.y;
        vertex.z = point.z + offsetZ;
        uv[0] = part.uvs?.[source * 2] ?? 0;
        uv[1] = part.uvs?.[source * 2 + 1] ?? 0;
        for (let channel = 0; channel < 3; channel++) color[channel] = part.colors?.[source * 3 + channel] ?? 1;
        addVertex(context, vertex, color, part.opacities?.[source] ?? 1, uv, null, part.firstTexel?.[source] ?? 0);
    }
}

function appendFaceData(context: SceneContext, part: PreparedMapPart): void {
    for (let face = 0; face < part.indices.length / 3; face++) {
        context.geometry.textureIds.push(part.textureIds?.[face] ?? -1);
        context.geometry.facePriorities.push(part.facePriorities?.[face] ?? 0);
    }
}

function rotateFloor(x: number, y: number, rotation: number): [number, number] {
    switch (rotation) {
        case 1:
            return [y, 512 - x];
        case 2:
            return [512 - x, 512 - y];
        case 3:
            return [512 - y, x];
        default:
            return [x, y];
    }
}
function placementCenter(region: MapRenderRegion, placement: MapPlacement, appearance: ObjectRenderAppearance): [number, number] {
    const width = placement.rotation & 1 ? appearance.sizeY : appearance.sizeX;
    const height = placement.rotation & 1 ? appearance.sizeX : appearance.sizeY;
    return [region.regionX * 64 + placement.x + width / 2, region.regionY * 64 + placement.y + height / 2];
}

function decorationOffset(
    region: MapRenderRegion,
    placement: MapPlacement,
    appearances: ReadonlyMap<string, ObjectRenderAppearance>,
    part: number
): [number, number] {
    if (![5, 6, 8].includes(placement.shape) || part > 0) return [0, 0];
    const wall = region.objects.find((object) => object.x === placement.x && object.y === placement.y && object.plane === placement.plane && object.shape <= 3);
    const displacement = wall ? (appearances.get(mapAppearanceKey(wall))?.decorDisplacement ?? 64) : 64;
    const amount = placement.shape === 5 ? displacement + 1 : Math.trunc(displacement / 2) + 1;
    const x = placement.shape === 5 ? [1, 0, -1, 0] : [1, -1, -1, 1];
    const y = placement.shape === 5 ? [0, -1, 0, 1] : [-1, -1, 1, 1];
    return [x[placement.rotation] * amount, y[placement.rotation] * amount];
}
