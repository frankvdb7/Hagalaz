import { prepareCacheModel } from "../../../../../shared/model-viewer/cache-model-geometry";
import type { ModelGeometry, ModelTexture } from "../../../../../shared/model-viewer/model-scene";
import { mapAppearanceKey, type MapPlacement, type MapRenderRegion, type ObjectRenderAppearance } from "../services/map-render.models";
import { terrainSampler, linearFloorColor, isVisibleFloor, type VisibleFloor, type FloorSample } from "./terrain-blending";
import type { TerrainBlend } from "../../../../../shared/model-viewer/terrain-material";

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

type SceneGeometry = {
    firstTexel: number[];
    positions: number[];
    indices: number[];
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
    blends: Map<string, TerrainBlend>;
    preparation: MapScenePreparation;
};
type ScenePoint = { x: number; y: number; z: number };

/** Selection-scoped reuse; no global model/terrain cache. */
export class MapScenePreparation {
    private ground?: { regions: readonly MapRenderRegion[]; planes: string; modes: string[]; geometry: SceneGeometry };
    private sampling?: { regions: readonly MapRenderRegion[]; sample: ReturnType<typeof terrainSampler> };
    private readonly models = new WeakMap<ObjectRenderAppearance, { dependencies: { id: number; prepared: boolean }[]; modes: string[]; parts: ModelGeometry[] }>();
    private combined?: { terrain: SceneGeometry; appearances: ReadonlyMap<string, ObjectRenderAppearance>; parts: ModelGeometry[][]; result: { geometry: SceneGeometry; warnings: string[] } };

    terrain(regions: readonly MapRenderRegion[], textures: ReadonlyMap<number, ModelTexture>, planes: readonly boolean[], build: () => SceneGeometry): SceneGeometry {
        const modes = regions.flatMap(region => region.floors.map(floor => {
            const texture = textures.get(floor.textureId);
            return `${texture?.alphaMode ?? 0}:${texture?.effectId ?? 0}`;
        }));
        const key = planes.join();
        if (this.ground?.regions === regions && this.ground.planes === key && this.ground.modes.every((mode, index) => modes[index] === mode)) return this.ground.geometry;
        const geometry = build();
        this.ground = { regions, planes: key, modes, geometry };
        return geometry;
    }

    sampleTerrain(regions: readonly MapRenderRegion[]): ReturnType<typeof terrainSampler> {
        if (this.sampling?.regions !== regions) this.sampling = { regions, sample: terrainSampler(regions) };
        return this.sampling.sample;
    }

    appearance(appearance: ObjectRenderAppearance, textures: ReadonlyMap<number, ModelTexture>): ModelGeometry[] {
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
        if (previous && sameValues(previous.modes, modes)) return previous.parts;
        const parts = appearance.parts.map(part => prepareCacheModel({ ...part, particles: [] }, textures));
        this.models.set(appearance, { dependencies, modes, parts });
        return parts;
    }

    landscape(terrain: SceneGeometry, appearances: ReadonlyMap<string, ObjectRenderAppearance>, textures: ReadonlyMap<number, ModelTexture>, build: () => { geometry: SceneGeometry; warnings: string[] }): { geometry: SceneGeometry; warnings: string[] } {
        const parts = [...appearances.values()].map(appearance => this.appearance(appearance, textures));
        const previous = this.combined;
        if (previous?.terrain === terrain && previous.appearances === appearances && sameValues(previous.parts, parts)) return previous.result;
        const result = build();
        this.combined = { terrain, appearances, parts, result };
        return result;
    }
}

function sameValues<T>(left: readonly T[], right: readonly T[]): boolean {
    return left.length === right.length && left.every((value, index) => value === right[index]);
}

function copyTerrain(source: SceneGeometry): SceneGeometry {
    return { firstTexel: source.firstTexel.slice(), positions: source.positions.slice(), indices: source.indices.slice(), colors: source.colors.slice(), opacities: source.opacities.slice(), uvs: source.uvs.slice(), textureIds: source.textureIds.slice(), facePriorities: source.facePriorities.slice(), terrainWeights: source.terrainWeights.slice(), terrainBlends: source.terrainBlends, preparedLighting: true };
}

export function buildMapScene(
    regions: readonly MapRenderRegion[],
    appearances: ReadonlyMap<string, ObjectRenderAppearance>,
    textures: ReadonlyMap<number, ModelTexture>,
    visiblePlanes: readonly boolean[],
    landscape: boolean,
    preparation = new MapScenePreparation()
): { geometry: ModelGeometry | null; warnings: string[] } {
    if (!regions.length) return { geometry: null, warnings: [] };
    const geometry: SceneGeometry = {
        firstTexel: [],
        positions: [],
        indices: [],
        colors: [],
        opacities: [],
        uvs: [],
        textureIds: [],
        facePriorities: [],
        preparedLighting: true,
        terrainBlends: [],
        terrainWeights: [],
    };
    let sampler: ReturnType<typeof terrainSampler> | undefined;
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
        blends: new Map(),
        preparation,
    };
    const terrain = preparation.terrain(regions, textures, visiblePlanes, () => { addTerrain(context); return context.geometry; });
    const result = landscape ? preparation.landscape(terrain, appearances, textures, () => {
        context.geometry = copyTerrain(terrain);
        addLandscape(context);
        const weights = context.geometry.terrainWeights;
        const terrainWeightCount = weights.length;
        weights.length = context.geometry.positions.length / 3 * 8;
        weights.fill(0, terrainWeightCount);
        return { geometry: context.geometry, warnings: [...context.warnings] };
    }) : { geometry: terrain, warnings: [...context.warnings] };
    return { geometry: result.geometry.positions.length ? result.geometry : null, warnings: result.warnings };
}

function addVertex(context: SceneContext, point: ScenePoint, color: readonly number[], opacity: number, uv: readonly number[], weights: readonly number[] | null = [], firstTexel = 0): void {
    const output = context.geometry;
    output.positions.push((point.x - context.originX) * 512, point.y, (point.z - context.originY) * 512);
    output.colors.push(...color);
    output.opacities.push(opacity);
    output.uvs.push(...uv);
    output.firstTexel.push(firstTexel);
    output.indices.push(output.indices.length);
    // Landscape's null weights are padded in one contiguous fill after assembly.
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
    if (!floor) return;
    const corners = [a[shape][triangle], c[shape][triangle], b[shape][triangle]].map((point) => {
        const [rx, ry] = rotateFloor(floorX[point], floorY[point], rotation);
        const wx = region.regionX * 64 + x + rx / 512,
            wy = region.regionY * 64 + y + ry / 512;
        return { wx, wy, height: mapHeight([region], level, wx, wy) };
    });
    const available = corners.filter((corner): corner is { wx: number; wy: number; height: number } => corner.height !== null);
    if (available.length !== 3) return;
    if (!overlay && addBlendedFloor(context, level, available)) return;
    const texture = floor.textureId;
    // Floor textures modulate the client palette tint, not raw definition RGB or white.
    context.geometry.textureIds.push(texture);
    context.geometry.facePriorities.push(0);
    for (const corner of available) {
        const samples = overlay ? [] : context.sampleTerrain(level, corner.wx, corner.wy);
        const tint = samples.length ? blendedFloorColor(samples) : linearFloorColor(floor.rgb);
        addVertex(context, { x: corner.wx, y: -corner.height, z: corner.wy }, tint, 1, [
            (corner.wx * 512) / floor.textureScale,
            (-corner.wy * 512) / floor.textureScale,
        ]);
    }
}

function addBlendedFloor(context: SceneContext, level: number, corners: readonly { wx: number; wy: number; height: number }[]): boolean {
    const samples = corners.map(corner => context.sampleTerrain(level, corner.wx, corner.wy));
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
        const color = blendedFloorColor(samples[index]);
        addVertex(context, { x: corner.wx, y: -corner.height, z: corner.wy }, color, 1, [corner.wx * 512, -corner.wy * 512], weights);
    });
    return true;
}

function blendedFloorColor(samples: readonly FloorSample[]): number[] {
    return [0, 1, 2].map(channel => samples.reduce((sum, sample) => sum + sample.layer.color[channel] * sample.weight, 0));
}

function addLandscape(context: SceneContext): void {
    const prepared = new Map<string, ModelGeometry[]>();
    for (const region of context.regions)
        for (const placement of region.objects) {
            addPlacement(context, region, placement, prepared);
        }
}

function addPlacement(context: SceneContext, region: MapRenderRegion, placement: MapPlacement, prepared: Map<string, ModelGeometry[]>): void {
    if (!context.visiblePlanes[placement.plane]) return;
    const key = mapAppearanceKey(placement),
        appearance = context.appearances.get(key);
    if (!appearance) return;
    if (appearance.contourMode > 2) {
        context.warnings.add(`Object ${placement.id}: unsupported contour mode ${appearance.contourMode}.`);
        return;
    }
    const parts = prepared.get(key) ?? context.preparation.appearance(appearance, context.textures);
    prepared.set(key, parts);
    const center = placementCenter(region, placement, appearance);
    const base = mapHeight(context.regions, placement.plane, center[0], center[1]);
    if (base === null) {
        context.warnings.add(`Region ${region.id}: some objects require unavailable terrain heights.`);
        return;
    }
    for (const [partIndex, part] of parts.entries()) {
        const projected = projectPart(context, placement, appearance, part, center, base);
        if (!projected) {
            context.warnings.add(`Region ${region.id}: some contoured objects require unavailable boundary data.`);
            continue;
        }
        const offset = decorationOffset(region, placement, context.appearances, partIndex);
        appendPart(context, part, projected, offset);
    }
}

function projectPart(
    context: SceneContext,
    placement: MapPlacement,
    appearance: ObjectRenderAppearance,
    part: ModelGeometry,
    center: readonly number[],
    base: number
): ScenePoint[] | null {
    const projected: ScenePoint[] = [];
    const maxHeight = part.positions.reduce((height, value, index) => (index % 3 === 1 ? Math.max(height, value) : height), 1);
    for (let index = 0; index < part.positions.length; index += 3) {
        const x = center[0] + part.positions[index] / 512,
            y = center[1] + part.positions[index + 2] / 512;
        const up = part.positions[index + 1];
        const height = appearance.contourMode ? mapHeight(context.regions, placement.plane, x, y) : base;
        if (height === null) return null;
        let delta = base - height;
        if (appearance.contourMode === 2) {
            const fraction = Math.trunc((up * 65536) / maxHeight);
            delta = fraction < appearance.contourParameter ? Math.trunc((delta * (appearance.contourParameter - fraction)) / appearance.contourParameter) : 0;
        }
        projected.push({ x, y: up - base + delta, z: y });
    }
    return projected;
}

function appendPart(context: SceneContext, part: ModelGeometry, projected: readonly ScenePoint[], offset: readonly number[]): void {
    appendFaceData(context, part);
    const vertex: ScenePoint = { x: 0, y: 0, z: 0 };
    const uv = [0, 0], color = [1, 1, 1];
    const offsetX = offset[0] / 512, offsetZ = offset[1] / 512;
    for (const source of part.indices) {
        const point = projected[source];
        vertex.x = point.x + offsetX;
        vertex.y = point.y;
        vertex.z = point.z + offsetZ;
        uv[0] = part.uvs?.[source * 2] ?? 0;
        uv[1] = part.uvs?.[source * 2 + 1] ?? 0;
        for (let channel = 0; channel < 3; channel++) color[channel] = part.colors?.[source * 3 + channel] ?? 1;
        addVertex(context, vertex, color, part.opacities?.[source] ?? 1, uv, null, part.firstTexel?.[source] ?? 0);
    }
}

function appendFaceData(context: SceneContext, part: ModelGeometry): void {
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
