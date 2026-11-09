import { describe, expect, it, vi } from "vitest";
import { buildMapScene as buildLayers, mapHeight, mapLandscapeSaves, MapScenePreparation, unstoredEditNotes, type MapSceneGeometry, type MapSceneObjects } from "./map-scene-geometry";
import { applyTerrainEdits } from "./map-terrain-edit";
import type { ModelGeometry, ModelTexture } from "../../../../../shared/model-viewer/model-scene";
import { mapAppearanceKey, type MapRenderRegion, type ObjectRenderAppearance } from "../services/map-render.models";
import { required } from "../../../../testing/required";
import type { MapObjectClip } from "../services/map-render.models";

it("poses a rotated placement with cache timing while preserving its base vertices and identity", () => {
    const source = region();
    const placement = { id: 1, shape: 10, rotation: 1, x: 1, y: 1, plane: 0 };
    source.objects = [placement, { ...placement, x: 2 }];
    const asset = appearance();
    asset.rotation = 1;
    asset.contourMode = 0;
    Object.assign(asset.parts[0], {
        objectAnimation: { rotation: 1, offset: [0, 0, 0], positionsBeforeExtraRotation: null },
        vertexSkins: [0, 0, 0], faceSkins: [1], animationIds: [42],
    });
    const clip: MapObjectClip = {
        sequence: { id: 42, frameIds: [10, 11], frameDurations: [2, 3], loop: 2, isTweened: false },
        frames: new Map([
            [10, { frame: { skeletonId: 1, transforms: [] }, skeleton: { types: [], groups: [], enabled: [], masks: [] } }],
            [11, {
                frame: { skeletonId: 1, transforms: [
                    { slot: 0, x: 32, y: 0, z: 0, pivotSlot: -1, tweenFlags: 0 },
                    { slot: 1, x: 10, y: 0, z: 0, pivotSlot: -1, tweenFlags: 0 },
                    { slot: 2, x: 2, y: 4, z: 5, pivotSlot: -1, tweenFlags: 0 },
                    { slot: 3, x: 4096, y: 0, z: 0, pivotSlot: -1, tweenFlags: 0 },
                ] },
                skeleton: { types: [1, 5, 7, 9], groups: [[0], [1], [1], [1]], enabled: [], masks: [] },
            }],
        ]),
    };
    const appearances = new Map([[mapAppearanceKey(placement), asset]]);
    const draw = (clips: ReadonlyMap<number, MapObjectClip>) => buildLayers(
        [source], appearances, new Map(), [true], true, new MapScenePreparation(), null, undefined, undefined, false, clips,
    );
    expect(required(draw(new Map()).geometry?.objects, "map objects").animations).toBeUndefined();
    const reads = vi.spyOn(clip.frames, "get");
    const animated = draw(new Map([[42, clip]]));
    expect(animated.warnings).toContain("Animation 42: unsupported transform types 9.");
    const objects = required(animated.geometry?.objects, "map objects");
    expect(objects.placements[0].placement).toBe(placement);
    expect(objects.mapPlanes).toEqual([0, 0, 0, 0, 0, 0]);
    const animations = required(objects.animations, "animated ranges");
    expect(animations.map(range => range.startVertex)).toEqual([0, 3]);
    const first = animations[0];
    const initial = required(first.sample(0).pose, "first frame");
    expect(Array.from(initial.positions)).toEqual(objects.positions.slice(0, 9));
    const initialRgb = Array.from(initial.rgba.slice(0, 3));
    expect(first.sample(39, 0)).toEqual({ delay: 1 });
    const moved = required(first.sample(40, 39).pose, "second frame");
    expect(moved.positions[0]).toBe(objects.positions[0]);
    expect(moved.positions[2]).toBe(objects.positions[2] - 32);
    expect(moved.rgba[3]).toBeCloseTo(175 / 255);
    expect(Array.from(moved.rgba.slice(0, 3))).not.toEqual(initialRgb);
    expect(objects.vertexRgba?.[3]).toBe(1);
    expect(first.alphaFaces).toEqual([true]);
    const nextLoop = required(first.sample(100, 99).pose, "first frame of the next loop");
    expect(Array.from(nextLoop.positions)).toEqual(objects.positions.slice(0, 9));
    for (let elapsed = 100; elapsed < 2000; elapsed += 100) {
        const other = required(animations[1].sample(elapsed).pose, "second placement");
        expect(other.positions[0]).toBe(objects.positions[9]);
        first.sample(elapsed + 40);
    }
    expect(reads).toHaveBeenCalledTimes(2);
    expect(animations[1].sample(2000).pose?.positions).toBe(nextLoop.positions);
    expect(mapLandscapeSaves([source], undefined)).toEqual([]);
});

describe("rebuilding after terrain edits", () => {
    const tileKey = (x: number, y: number) => (x << 6) | y;
    const near = { id: 1, shape: 10, rotation: 0, x: 1, y: 1, plane: 0 };
    const far = { ...near, x: 40, y: 40 };
    function scene() {
        const source = region();
        source.objects = [near, far];
        const appearances = new Map([[mapAppearanceKey(near), appearance()]]);
        const preparation = new MapScenePreparation();
        const draw = (regions: readonly MapRenderRegion[], draft?: Parameters<typeof buildLayers>[7]) => required(buildLayers(regions, appearances, new Map(), [true], true, preparation, source.id, draft).geometry, "the scene");
        return { source, draw, first: draw([source]) };
    }
    const edit = (id: number, tile: number, change: object) => new Map([[id, new Map([[tile, change]])]]);

    it("keeps object geometry across a floor stroke", () => {
        const { source, draw, first } = scene();
        const painted = draw(applyTerrainEdits([source], edit(source.id, tileKey(5, 5), { underlay: 1, overlay: 0 }), new Map()));
        expect(painted.objects).toBe(first.objects);
        expect(painted.terrain).not.toBe(first.terrain);
    });

    it("treats an empty edit list like no edits", () => {
        const { source, draw, first } = scene();
        expect(draw([source], { transforms: new Map(), additions: [] }).objects).toBe(first.objects);
    });

    it("re-fits only the objects on changed ground", () => {
        const { source, draw, first } = scene();
        const objects = required(first.objects, "the objects");
        const [nearRange, farRange] = first.placements;
        const raised = required(draw(applyTerrainEdits([source], edit(source.id, tileKey(2, 2), { height: 60 }), new Map())).objects, "the raised objects");
        expect(raised.patchOf?.base).toBe(objects);
        expect(raised.patchOf?.ranges).toEqual([[nearRange.startVertex, nearRange.endVertex]]);
        expect(raised.positions.slice(nearRange.startVertex * 3, nearRange.endVertex * 3)).not.toEqual(objects.positions.slice(nearRange.startVertex * 3, nearRange.endVertex * 3));
        expect(raised.positions.slice(farRange.startVertex * 3, farRange.endVertex * 3)).toEqual(objects.positions.slice(farRange.startVertex * 3, farRange.endVertex * 3));
    });

    it("keeps only the newest patch", () => {
        const { source, draw, first } = scene();
        const once = required(draw(applyTerrainEdits([source], edit(source.id, tileKey(2, 2), { height: 60 }), new Map())).objects, "the raised objects");
        const twice = required(draw(applyTerrainEdits([source], edit(source.id, tileKey(2, 2), { height: 90 }), new Map())).objects, "the raised objects");
        expect(twice.patchOf?.base).toBe(once);
        expect(once.patchOf).toBeUndefined();
        expect(first.objects).not.toBe(twice);
    });

    it("leaves objects alone when the heights are equal", () => {
        const { source, draw, first } = scene();
        const copy = { ...source, planes: source.planes.map(plane => ({ ...plane, cornerHeights: [...plane.cornerHeights] })) };
        expect(draw([copy]).objects).toBe(first.objects);
    });

    it("rebuilds when a change removes an object's vertices", () => {
        const { source, draw, first } = scene();
        const heights = [...source.planes[0].cornerHeights];
        heights[65 + 1] = null;
        const changed = { ...source, planes: [{ ...source.planes[0], cornerHeights: heights }, ...source.planes.slice(1)] };
        const objects = required(draw([changed]).objects, "the objects");
        expect(objects.patchOf).toBeUndefined();
        expect(objects).not.toBe(first.objects);
    });
});

describe("mapLandscapeSaves", () => {
    const placement = (id: number) => ({ id, shape: 10, rotation: 0, x: id, y: id, plane: 0 });
    const withObjects = (id: number, ...objects: ReturnType<typeof placement>[]) => ({ ...region(id), objects });

    it("has nothing to save without a draft", () => {
        expect(mapLandscapeSaves([withObjects(1, placement(1))], undefined)).toEqual([]);
    });

    it("sends only edited regions with moves, rotations, removals and additions applied", () => {
        const moved = placement(1), removed = placement(2), kept = placement(3), added = placement(4), addedThenMoved = placement(5);
        const saves = mapLandscapeSaves([withObjects(1, moved, removed, kept), withObjects(2, placement(9))], {
            transforms: new Map([
                [moved, { x: 20, y: 21, plane: 2, rotation: 3, scale: 2 }],
                [removed, null],
                [addedThenMoved, { x: 30, y: 31, plane: 1, rotation: 1, scale: 1 }],
            ]),
            additions: [{ regionId: 1, placement: added }, { regionId: 1, placement: addedThenMoved }],
        });
        expect(saves).toEqual([{ kind: "landscape", regionId: 1, objects: [
            { id: 1, shape: 10, rotation: 3, x: 20, y: 21, plane: 2 },
            kept,
            added,
            { id: 5, shape: 10, rotation: 1, x: 30, y: 31, plane: 1 },
        ] }]);
    });

    it("keeps underwater objects out of the surface landscape", () => {
        const surface = placement(1), underwater = { ...placement(2), plane: 4 };
        const moved = { x: 9, y: 9, plane: 0, rotation: 0, scale: 1 };
        const regions = [withObjects(1, surface, underwater)];
        expect(mapLandscapeSaves(regions, { transforms: new Map([[underwater, moved]]), additions: [] })).toEqual([]);
        expect(mapLandscapeSaves(regions, { transforms: new Map([[surface, moved]]), additions: [] })).toEqual([{ kind: "landscape", regionId: 1, objects: [{ id: 1, shape: 10, rotation: 0, x: 9, y: 9, plane: 0 }] }]);
    });

    it("notes the edits saving cannot store", () => {
        const surface = placement(1), underwater = { ...placement(2), plane: 4 };
        const same = { x: 1, y: 1, plane: 0, rotation: 0, scale: 1 };
        expect(unstoredEditNotes(undefined)).toEqual([]);
        expect(unstoredEditNotes({ transforms: new Map([[surface, same], [placement(3), null]]), additions: [] })).toEqual([]);
        expect(unstoredEditNotes({ transforms: new Map([[surface, { ...same, scale: 2 }]]), additions: [] })).toEqual(["Scale changes are not stored."]);
        expect(unstoredEditNotes({ transforms: new Map([[underwater, same]]), additions: [] })).toEqual(["Underwater object changes are not stored."]);
        expect(unstoredEditNotes({ transforms: new Map(), additions: [{ regionId: 1, placement: underwater }] })).toEqual(["Underwater object changes are not stored."]);
    });

    it("skips drafts that leave the stored placements unchanged", () => {
        const scaled = placement(1), added = placement(2);
        expect(mapLandscapeSaves([withObjects(1, scaled)], {
            transforms: new Map([[scaled, { x: 1, y: 1, plane: 0, rotation: 0, scale: 3 }], [added, null]]),
            additions: [{ regionId: 1, placement: added }],
        })).toEqual([]);
    });
});

const merged = new WeakMap<object, WeakMap<object, CombinedScene>>();
type CombinedScene = ReturnType<typeof combine>;

const noLayer = { positions: [], indices: [], colors: [], opacities: [], uvs: [], textureIds: [], facePriorities: [], placements: [] };

function terrainRgba(terrain: ModelGeometry): number[] {
    const colors = terrain.colors ?? [], opacities = terrain.opacities ?? [];
    return opacities.flatMap((opacity, vertex) => [colors[vertex * 3], colors[vertex * 3 + 1], colors[vertex * 3 + 2], opacity]);
}

function sequence(count: number): Uint16Array | Uint32Array {
    const indices = count > 65535 ? new Uint32Array(count) : new Uint16Array(count);
    return indices.map((_, index) => index);
}

function combineObjects(terrain: ModelGeometry, objects: MapSceneObjects, offset: number) {
    return {
        terrainBlends: terrain.terrainBlends ?? [],
        terrainWeights: terrain.terrainWeights ?? [],
        firstTexel: objects.firstTexel && Uint8Array.of(...Array(offset).fill(0), ...objects.firstTexel),
        vertexRgba: Float32Array.of(...terrainRgba(terrain), ...objects.vertexRgba ?? []),
        uvs: Float32Array.of(...terrain.uvs ?? [], ...objects.uvs ?? []),
    };
}

/** The two layers read as one buffer, terrain first, which is how most of these expectations are stated. */
function combine(layers: MapSceneGeometry) {
    const terrain: ModelGeometry = layers.terrain ?? noLayer;
    const objects: ModelGeometry = layers.objects ?? noLayer;
    const offset = terrain.positions.length / 3;
    const joined = {
        positions: [...terrain.positions, ...objects.positions],
        indices: sequence(offset + objects.positions.length / 3),
        textureIds: [...terrain.textureIds ?? [], ...objects.textureIds ?? []],
        facePriorities: [...terrain.facePriorities ?? [], ...objects.facePriorities ?? []],
        placements: layers.placements.map(range => ({ ...range, startVertex: range.startVertex + offset, endVertex: range.endVertex + offset })),
    };
    return layers.objects ? { ...layers.objects, ...joined, ...combineObjects(terrain, layers.objects, offset) } : { ...terrain, ...joined };
}

function combined(layers: MapSceneGeometry): CombinedScene {
    const terrainKey = layers.terrain ?? merged, objectsKey = layers.objects ?? merged;
    const byObjects = merged.get(terrainKey) ?? new WeakMap();
    merged.set(terrainKey, byObjects);
    const existing = byObjects.get(objectsKey);
    if (existing) return existing;
    const created = combine(layers);
    byObjects.set(objectsKey, created);
    return created;
}

function buildMapScene(...args: Parameters<typeof buildLayers>) {
    const scene = buildLayers(...args);
    return { ...scene, geometry: scene.geometry && combined(scene.geometry) };
}

function region(id = 258): MapRenderRegion {
    return {
        id,
        regionX: id >> 8,
        regionY: id & 255,
        terrainStatus: "available",
        landscapeStatus: "available",
        objects: [],
        warnings: [],
        floors: [{ id: 1, overlay: false, rgb: 0xff0000, textureId: -1, secondaryRgb: null, textureScale: 512 }],
        planes: Array.from({ length: 4 }, (_, plane) => ({
            cornerHeights: Array.from({ length: 4225 }, (_, i) => -Math.floor(i / 65) * 32 - (i % 65) * 64 - plane * 960),
            flags: Array(4096).fill(0),
            underlays: [1, ...Array(4095).fill(0)],
            overlays: Array(4096).fill(0),
            overlayShapes: Array(4096).fill(0),
            overlayRotations: Array(4096).fill(0),
        })),
    };
}
function appearance(): ObjectRenderAppearance {
    return {
        id: 1,
        shape: 10,
        rotation: 0,
        sizeX: 1,
        sizeY: 1,
        decorDisplacement: 64,
        contourMode: 1,
        contourParameter: 0,
        warnings: [],
        parts: [
            {
                id: 1,
                positions: [0, 0, 0, 512, 0, 0, 0, -512, 0],
                indices: [0, 1, 2],
                faceColors: [127],
                faceAlpha: [0],
                vertexSkins: null,
                warnings: [],
                animationIds: null,
                coordinateScale: 1,
                faceSkins: null,
                faceTextureIds: null,
                faceTextureVertices: null,
                faceTextureUvs: null,
                scale: null,
                particles: null,
            },
        ],
    };
}

describe("map scene geometry", () => {
    it("applies local transforms and additions without changing the source placements", () => {
        const source = region();
        const original = { id: 1, shape: 10, rotation: 0, plane: 0, x: 0, y: 3 };
        const copy = { ...original, x: 2 };
        source.objects = [original];
        const appearances = new Map([[mapAppearanceKey(original), appearance()], [mapAppearanceKey({ ...original, rotation: 1 }), { ...appearance(), rotation: 1 }]]);
        const preparation = new MapScenePreparation();
        const regions = [source];
        const draw = (draft?: Parameters<typeof buildMapScene>[7]) => required(buildMapScene(regions, appearances, new Map(), [true], true, preparation, source.id, draft).geometry, "scene geometry");
        const initial = draw();
        const edited = draw({
            transforms: new Map([[original, { x: 1, y: 3, plane: 0, rotation: 1, scale: 2 }]]),
            additions: [{ regionId: source.id, placement: copy }],
        });
        expect(edited.placements.map(item => item.placement)).toEqual([original, copy]);
        const start = initial.placements[0].startVertex * 3;
        expect(edited.positions.slice(0, start)).toEqual(initial.positions.slice(0, start));
        expect(edited.positions[start] - initial.positions[start]).toBe(512);
        expect(edited.positions.slice(start, start + 9)).not.toEqual(initial.positions.slice(start, start + 9));
        expect(draw({ transforms: new Map([[original, null]]), additions: [] }).placements).toEqual([]);
        expect(source.objects).toEqual([original]);
        expect(draw().positions).toEqual(initial.positions);
    });
    it("draws rotation edits from the rotated appearance and footprint", () => {
        const source = region();
        const original = { id: 1, shape: 10, rotation: 0, plane: 0, x: 0, y: 0 };
        source.objects = [original];
        const unrotated = { ...appearance(), sizeX: 1, sizeY: 3, contourMode: 0 };
        const rotated = { ...unrotated, rotation: 1, parts: [{ ...unrotated.parts[0], positions: [0, 0, 0, 0, 0, 512, 0, -512, 0] }] };
        const appearances = new Map([[mapAppearanceKey(original), unrotated], [mapAppearanceKey({ ...original, rotation: 1 }), rotated]]);
        const edited = required(buildMapScene([source], appearances, new Map(), [true], true, new MapScenePreparation(), source.id, {
            transforms: new Map([[original, { x: 0, y: 0, plane: 0, rotation: 1, scale: 1 }]]),
            additions: [],
        }).geometry, "edited scene geometry");
        const start = edited.placements[0].startVertex * 3;
        // A rotated 1x3 footprint spans three tiles east and one north; its centre is (1.5, 0.5).
        expect([edited.positions[start], edited.positions[start + 2]]).toEqual([768, 256]);
        // Corners come from the rotated appearance, not a client-side spin of the original mesh.
        const corners = [0, 1, 2].map(corner => [edited.positions[start + corner * 3], edited.positions[start + corner * 3 + 2]]);
        expect(corners).toEqual(expect.arrayContaining([[768, 256], [768, 768]]));
        expect(corners.every(([x]) => x === 768)).toBe(true);
    });
    it("keeps a rotation edit drawn at its source rotation until the rotated appearance loads", () => {
        const source = region();
        const original = { id: 1, shape: 10, rotation: 0, plane: 0, x: 0, y: 0 };
        source.objects = [original];
        const unrotated = { ...appearance(), sizeX: 1, sizeY: 3, contourMode: 0 };
        const pending = required(buildMapScene([source], new Map([[mapAppearanceKey(original), unrotated]]), new Map(), [true], true, new MapScenePreparation(), source.id, {
            transforms: new Map([[original, { x: 2, y: 0, plane: 0, rotation: 1, scale: 1 }]]),
            additions: [],
        }).geometry, "pending scene geometry");
        expect(pending.placements.map(item => item.placement)).toEqual([original]);
        const start = pending.placements[0].startVertex * 3;
        // Source footprint (1x3) at the edited tile: centre (2.5, 1.5).
        expect([pending.positions[start], pending.positions[start + 2]]).toEqual([1280, 768]);
    });
    it("records separate vertex ranges for copies of the same landscape object", () => {
        const source = region();
        const first = { id: 1, shape: 10, rotation: 0, plane: 0, x: 0, y: 0 };
        const second = { ...first, x: 1 };
        source.objects = [first, second];
        const appearances = new Map([[mapAppearanceKey(first), appearance()]]);
        const ground = required(buildMapScene([source], appearances, new Map(), [true], false).geometry, "ground geometry");
        const scene = required(buildMapScene([source], appearances, new Map(), [true], true).geometry, "scene geometry");
        const start = ground.positions.length / 3;
        expect(scene.placements).toEqual([
            { regionId: source.id, placement: first, startVertex: start, endVertex: start + 3 },
            { regionId: source.id, placement: second, startVertex: start + 3, endVertex: start + 6 },
        ]);
        expect(ground.placements).toEqual([]);
    });
    describe("object shadows on the floor", () => {
        const placement = { id: 1, shape: 10, rotation: 0, plane: 0, x: 0, y: 0 };
        const shadowed = (strength: number) => new Map([[mapAppearanceKey(placement), { ...appearance(), shadowStrength: strength }]]);
        const withObject = () => { const source = region(); source.objects = [placement]; return source; };
        const ground = (...args: Parameters<typeof buildLayers>) => required(buildLayers(...args).geometry?.terrain, "terrain");
        const red = (terrain: { colors?: ArrayLike<number> }) => Array.from(required(terrain.colors, "terrain colours")).filter((_, channel) => channel % 3 === 0);

        it("darkens floors at a casting object's corners and leaves others unchanged", () => {
            const source = withObject();
            const plain = red(ground([source], shadowed(0), new Map(), [true], true));
            const dark = red(ground([source], shadowed(30), new Map(), [true], true));
            expect(dark.length).toBe(plain.length);
            dark.forEach((value, vertex) => { expect(value).toBeLessThanOrEqual(plain[vertex]); });
            expect(dark.some((value, vertex) => value < plain[vertex])).toBe(true);
        });

        it("drops the shadow for removed, hidden or switched-off objects", () => {
            const source = withObject(), lit = red(ground([source], shadowed(0), new Map(), [true], true));
            expect(red(ground([source], shadowed(30), new Map(), [true], true, new MapScenePreparation(), source.id, { transforms: new Map([[placement, null]]), additions: [] }))).toEqual(lit);
            expect(red(ground([source], shadowed(30), new Map(), [true], true, new MapScenePreparation(), source.id, undefined, { terrain: true, waterSurface: true, objectShadows: true, hiddenShapes: new Set([10]), hiddenPlacements: new Set() }))).toEqual(lit);
            expect(red(ground([source], shadowed(30), new Map(), [true], false))).toEqual(lit);
            expect(red(ground([source], shadowed(30), new Map(), [true], true, new MapScenePreparation(), source.id, undefined, { terrain: true, waterSurface: true, objectShadows: false, hiddenShapes: new Set(), hiddenPlacements: new Set() }))).toEqual(lit);
        });

        it("reuses the terrain while assets load and applies shadows once they settle", () => {
            const source = withObject(), regions = [source], preparation = new MapScenePreparation();
            const draw = (appearances: ReturnType<typeof shadowed>, loading: boolean) => ground(regions, appearances, new Map(), [true], true, preparation, source.id, undefined, undefined, loading);
            const first = draw(new Map(), true);
            expect(draw(shadowed(30), true)).toBe(first);
            const settled = draw(shadowed(30), false);
            expect(settled).not.toBe(first);
            expect(draw(shadowed(30), true)).toBe(settled);
            expect(draw(shadowed(30), false)).toBe(settled);
        });
    });

    it("uses only the selected anchor atmosphere, including when neighbors load first", () => {
        const anchor = region(), neighbor = region(514);
        neighbor.atmosphere = { lightRgb: 0xffffff, intensity: 0, positiveResponse: 1, negativeResponse: 1, directionX: 0, directionY: -1, directionZ: 0, skyRgb: 0xc8c0a8, fogValue: 0, environmentFaces: null };
        anchor.atmosphere = { ...neighbor.atmosphere, intensity: 1.1523438, lightRgb: 0xff0000 };
        const regions = [neighbor, anchor], preparation = new MapScenePreparation();
        const draw = (loaded: MapRenderRegion[], id: number | null) => required(buildMapScene(loaded, new Map(), new Map(), [true], false, preparation, id).geometry, "terrain geometry");
        const partial = draw([neighbor], anchor.id);
        const partialColors = required(partial.colors, "partial terrain colours");
        expect(partialColors[1]).toBeGreaterThan(0);
        const lit = draw(regions, anchor.id);
        expect(required(lit.colors, "lit terrain colours").slice(0, 3)).toEqual([partialColors[0], 0, 0]);
        expect(draw(regions, anchor.id)).toBe(lit);
        expect(required(draw(regions, null).colors, "terrain colours").slice(0, 3)).toEqual(partialColors.slice(0, 3));
        expect(required(draw(regions, neighbor.id).colors, "neighbour terrain colours").every(channel => channel === 0)).toBe(true);
    });
    it("retains first-texel markers when prepared object faces join terrain", () => {
        const source = region();
        const object = appearance();
        object.parts[0].faceTextureIds = [42];
        object.parts[0].faceTextureUvs = [0,0,0,0,0,0];
        object.parts[0].firstTexelFaces = [0];
        source.objects = [{id:1,shape:10,rotation:0,plane:0,x:0,y:0}];
        const geometry = buildMapScene([source], new Map([[mapAppearanceKey(source.objects[0]),object]]), new Map(), [true], true).geometry;
        expect(geometry).not.toBeNull();
        if (!geometry) throw new Error("Expected terrain and object geometry");
        expect(geometry.firstTexel?.slice(-3)).toEqual(Uint8Array.of(1,1,1));
        expect(geometry.firstTexel?.slice(0,-3).every(value=>value===0)).toBe(true);
        expect(geometry.firstTexel?.length).toBe(geometry.positions.length/3);
        expect(geometry.textureIds?.at(-1)).toBe(42);
    });
    it("reuses terrain across landscape publications and invalidates relevant materials and regions", () => {
        const source = region(), regions = [source], preparation = new MapScenePreparation();
        source.floors[0].textureId = 7;
        const original = buildMapScene(regions, new Map(), new Map(), [true], false, preparation).geometry;
        const unrelated: ModelTexture = { image: {} as ImageBitmap, alphaMode: 0 };
        const next = buildMapScene(regions, new Map([["1:10:0", appearance()]]), new Map([[99, unrelated]]), [true], false, preparation).geometry;
        expect(next).toBe(original);
        const withFloor = buildMapScene(regions, new Map(), new Map([[7, unrelated]]), [true], false, preparation).geometry;
        expect(withFloor).toBe(original);
        expect(withFloor?.textureIds).toEqual([7, 7, 7, 7]);
        expect(original?.textureIds).toEqual([7, 7, 7, 7]);
        const replaced = buildMapScene(regions, new Map(), new Map([[7, { ...unrelated, image: {} as ImageBitmap }]]), [true], false, preparation).geometry;
        expect(replaced).toBe(original);
        const transparent = buildMapScene(regions, new Map(), new Map([[7, { ...unrelated, alphaMode: 2 }]]), [true], false, preparation).geometry;
        expect(transparent).not.toBe(original);
        const special = buildMapScene(regions, new Map(), new Map([[7, { ...unrelated, effectId: 4 }]]), [true], false, preparation).geometry;
        expect(special).not.toBe(transparent);
        const expanded = buildMapScene([...regions, region(514)], new Map(), new Map([[7, unrelated]]), [true], false, preparation).geometry;
        expect(expanded).not.toBe(withFloor);
        expect(expanded?.positions.length).toBeGreaterThan(withFloor?.positions.length ?? 0);
        const upper = buildMapScene(regions, new Map(), new Map(), [false, true], false, preparation).geometry;
        expect(upper?.positions[1]).toBe(960);
    });

    it("reuses spatial tint sampling across material and plane changes until region data changes", () => {
        const regions = [region()], preparation = new MapScenePreparation();
        const sample = preparation.sampleTerrain(regions);
        buildMapScene(regions, new Map(), new Map([[7, { image: {} as ImageBitmap, alphaMode: 2 }]]), [true], false, preparation);
        buildMapScene(regions, new Map(), new Map(), [false, true], false, preparation);
        expect(preparation.sampleTerrain(regions)).toBe(sample);
        const adjacent = [...regions, region(514)];
        expect(preparation.sampleTerrain(adjacent)).not.toBe(sample);
        expect(preparation.sampleTerrain(adjacent)(0, 128.5, 128.5)).toHaveLength(1);
    });

    it("reuses prepared appearances until their own texture inputs change", () => {
        const source = appearance(), preparation = new MapScenePreparation();
        source.parts[0].faceTextureIds = [7];
        source.parts[0].faceTextureUvs = [0, 0, 1, 0, 0, 1];
        const originalPositions = [...source.parts[0].positions];
        const first = preparation.appearance(source, new Map());
        const texture: ModelTexture = { image: {} as ImageBitmap, alphaMode: 0 };
        expect(preparation.appearance(source, new Map([[99, texture]]))).toBe(first);
        const textured = preparation.appearance(source, new Map([[7, texture]]));
        expect(textured).not.toBe(first);
        expect(preparation.appearance(source, new Map([[7, texture]]))).toBe(textured);
        expect(source.parts[0].positions).toEqual(originalPositions);
    });

    it("shares cached positions across face corners without merging their colors or UVs", () => {
        const source = appearance(), preparation = new MapScenePreparation();
        const part = source.parts[0];
        part.indices = [0, 1, 2, 0, 1, 2];
        part.faceColors = [0x1234, 0x4321];
        part.faceTextureIds = [-1, 7];
        part.faceTextureUvs = [0, 0, 0, 0, 0, 0, 0.25, 0.5, 1, 0, 0, 1];
        const [packed] = preparation.appearance(source, new Map());
        expect(packed.positions).toHaveLength(9);
        expect(packed.positionIndices).toEqual(Uint16Array.of(0, 1, 2, 0, 1, 2));
        expect(packed.indices).toEqual(Uint16Array.of(0, 1, 2, 3, 4, 5));
        expect(packed.colors?.slice(0, 9)).not.toEqual(packed.colors?.slice(9));
        expect(packed.uvs?.slice(6)).toEqual(Float64Array.of(0.25, 0.5, 0, 1, 1, 0));
    });

    it("retains combined landscape geometry across ordinary image arrivals and replacements", () => {
        const source = region(), object = appearance(), preparation = new MapScenePreparation();
        const regions = [source];
        source.objects = [{ id: 1, shape: 10, rotation: 0, plane: 0, x: 0, y: 0 }];
        object.parts[0].ambient = 64;
        object.parts[0].faceTextureIds = [7];
        object.parts[0].faceTextureUvs = [0,0,1,0,0,1];
        const appearances = new Map([["1:10:0", object]]);
        const parts = preparation.appearance(object, new Map());
        const original = buildMapScene(regions, appearances, new Map(), [true], true, preparation);
        expect(original.geometry?.textureIds?.at(-1)).toBe(7);
        expect(original.geometry?.positions.length).toBeGreaterThan(36);
        for (const material of [
            { image: {} as ImageBitmap, alphaMode: 0 as const, colorBlend: 0, brightness: 0 },
            { image: {} as ImageBitmap, alphaMode: 2 as const, effectId: 4 },
        ]) {
            const textures = new Map([[7, material]]);
            const next = buildMapScene(regions, appearances, textures, [true], true, preparation);
            expect(next.geometry).toBe(original.geometry);
            expect(next.warnings).toEqual(original.warnings);
            expect(preparation.appearance(object, textures)).toBe(parts);
        }
        const unrelated: ModelTexture = { image: {} as ImageBitmap, alphaMode: 0, colorBlend: 255 };
        expect(buildMapScene(regions, appearances, new Map([[99, unrelated]]), [true], true, preparation).geometry).toBe(original.geometry);
    });

    it("reuses terrain weights without allocating weights for landscape vertices", () => {
        const source = region(), object = appearance();
        source.floors.push({ id: 2, overlay: false, rgb: 0x0000ff, textureId: 20, textureScale: 512, secondaryRgb: null });
        source.planes[0].underlays[1] = 2;
        source.objects = [0,1].map(x => ({ id: 1, shape: 10, rotation: 0, plane: 0, x, y: 0 }));
        object.parts[0].ambient = 64;
        object.parts[0].faceTextureIds = [42];
        object.parts[0].faceTextureUvs = [0,0,1,0,0,1];
        object.parts[0].firstTexelFaces = [0];
        object.parts.push({ ...object.parts[0], id: 2, faceColors: [0x1234], faceTextureIds: [43],
            faceTextureUvs: [0.25,0.25,0.75,0.25,0.25,0.75], firstTexelFaces: [] });
        const input = JSON.stringify(object);
        const regions = [source], preparation = new MapScenePreparation(), textures = new Map();
        const terrain = buildMapScene(regions, new Map(), textures, [true], false, preparation).geometry;
        const combined = buildMapScene(regions, new Map([["1:10:0", object]]), textures, [true], true, preparation).geometry;
        if (!terrain || !combined) throw new Error("Expected combined geometry");
        const terrainWeights = terrain.terrainWeights ?? [], weights = combined.terrainWeights ?? [];
        expect(terrainWeights.some(weight => weight > 0)).toBe(true);
        expect(weights.length).toBe(terrain.positions.length / 3 * 8);
        expect(weights).toBe(terrainWeights);
        expect(combined.indices).toBeInstanceOf(Uint16Array);
        expect(combined.indices.at(-1)).toBe(combined.positions.length / 3 - 1);
        expect(combined.firstTexel?.slice(-12)).toEqual(Uint8Array.of(1,1,1,0,0,0,1,1,1,0,0,0));
        expect(combined.textureIds?.slice(-4)).toEqual([42,43,42,43]);
        expect(combined.uvs?.slice(-6)).toEqual(Float32Array.of(0.25,0.25,0.25,0.75,0.75,0.25));
        expect(combined.vertexRgba?.slice(-24, -12)).not.toEqual(combined.vertexRgba?.slice(-12));
        expect(combined.colors).toBeUndefined();
        expect(combined.opacities).toBeUndefined();
        expect(JSON.stringify(object)).toBe(input);
    });

    it.each([
        { label: "colour blending", settings: { colorBlend: 128 } },
        { label: "brightness", settings: { brightness: 32 } },
        { label: "effect 6 lighting", settings: { effectId: 6 } },
    ])("rebuilds prepared landscape colours when $label changes", ({ settings }) => {
        const source = region(), object = appearance(), preparation = new MapScenePreparation();
        const regions = [source];
        source.objects = [{ id: 1, shape: 10, rotation: 0, plane: 0, x: 0, y: 0 }];
        object.parts[0].ambient = 64;
        object.parts[0].faceTextureIds = [7];
        object.parts[0].faceTextureUvs = [0,0,1,0,0,1];
        const appearances = new Map([["1:10:0", object]]);
        const original = buildMapScene(regions, appearances, new Map(), [true], true, preparation).geometry;
        const textures = new Map<number, ModelTexture>([[7, { image: {} as ImageBitmap, alphaMode: 0, ...settings }]]);
        const changed = buildMapScene(regions, appearances, textures, [true], true, preparation).geometry;
        expect(changed).not.toBe(original);
        expect(changed?.vertexRgba?.slice(-12)).not.toEqual(original?.vertexRgba?.slice(-12));
        expect(changed).toEqual(buildMapScene(regions, appearances, textures, [true], true).geometry);
        const replacement = new Map<number, ModelTexture>([[7, { image: {} as ImageBitmap, alphaMode: 0, ...settings }]]);
        expect(buildMapScene(regions, appearances, replacement, [true], true, preparation).geometry).toBe(changed);
        const restored = buildMapScene(regions, appearances, new Map(), [true], true, preparation).geometry;
        expect(restored?.vertexRgba).toEqual(original?.vertexRgba);
    });

    it("preserves combined warnings and rebuilds for appearance region and visibility changes", () => {
        const source = region(), object = appearance(), preparation = new MapScenePreparation();
        const regions = [source];
        source.objects = [{ id: 1, shape: 10, rotation: 0, plane: 0, x: 0, y: 0 }];
        object.contourMode = 3;
        const appearances = new Map([["1:10:0", object]]);
        const first = buildMapScene(regions, appearances, new Map(), [true], true, preparation);
        expect(first.warnings).toEqual(["Object 1: unsupported contour mode 3."]);
        expect(buildMapScene(regions, appearances, new Map(), [true], true, preparation)).toEqual(first);
        const updated = new Map([["1:10:0", { ...object, contourMode: 0 }]]);
        const changed = buildMapScene(regions, updated, new Map(), [true], true, preparation);
        expect(changed.geometry).not.toBe(first.geometry);
        expect(changed.geometry?.positions.length).toBeGreaterThan(first.geometry?.positions.length ?? 0);
        expect(changed.warnings).toEqual([]);
        expect(buildMapScene(regions, updated, new Map(), [true], false, preparation).geometry).not.toBe(changed.geometry);
        const upper = buildMapScene(regions, updated, new Map(), [false, true], true, preparation);
        expect(upper.geometry).not.toBe(changed.geometry);
        expect(upper.geometry?.positions[1]).toBe(960);
        const adjacent = buildMapScene([...regions, region(514)], updated, new Map(), [true], true, preparation);
        expect(adjacent.geometry).not.toBe(changed.geometry);
        expect(adjacent.geometry?.positions.length).toBeGreaterThan(changed.geometry?.positions.length ?? 0);
    });
    it("emits continuous normalized texture weights at terrain material boundaries", () => {
        const source = region();
        source.floors.push({ id: 2, overlay: false, rgb: 0x0000ff, textureId: 20, textureScale: 1024, secondaryRgb: null });
        source.floors[0].textureId = 10;
        source.planes[0].underlays[1] = 2;
        const geometry = buildMapScene([source], new Map(), new Map(), [true], false).geometry;
        if (!geometry) throw new Error("Expected terrain geometry");
        expect(geometry.terrainBlends).toHaveLength(1);
        expect(geometry.terrainBlends?.[0].layers.map(layer => layer.textureId)).toEqual([10, 20]);
        const weights = geometry.terrainWeights ?? [];
        geometry.textureIds?.forEach((id, face) => {
            if (id >= -1) return;
            for (let corner = 0; corner < 3; corner++) {
                const vertex = geometry.indices[face * 3 + corner] * 8;
                expect(weights.slice(vertex, vertex + 8).reduce((sum, weight) => sum + weight, 0)).toBeCloseTo(1);
            }
        });
        const colors = geometry.colors ?? [];
        expect(colors.some((value, index) => index % 3 === 0 && value > 0 && colors[index + 2] > 0)).toBe(true);
        expect(geometry.textureIds).toContain(-2);
        const effectTexture: ModelTexture = { image: {} as ImageBitmap, alphaMode: 0, effectId: 4 };
        const special = buildMapScene([source], new Map(), new Map([[20, effectTexture]]), [true], false).geometry;
        expect(special?.terrainBlends).toEqual([]);
        expect(special?.textureIds).toContain(20);
        const reflective = buildMapScene([source], new Map(), new Map([[20, { ...effectTexture, effectId: 7 }]]), [true], false).geometry;
        expect(reflective?.terrainBlends?.[0].layers.map(layer => [layer.textureId, layer.reflective])).toEqual([[10, false], [20, true]]);
        expect(reflective?.textureIds).toContain(-2);
    });
    it.each([
        { id: 160, rgb: 0x29380f, textureId: 918, tint: [0.05448027644244237, 0.0822827071298148, 0.017641954488384078] },
        { id: 164, rgb: 0x1c1813, textureId: 929, tint: [0.0295568344378088, 0.02315336617811041, 0.02315336617811041] },
    ])("retains client region 12850 underlay $id colour when its texture finishes loading", ({ id, rgb, textureId, tint }) => {
        const source = region(12850);
        // Real m50_50 definitions, with base palette values from the original Java client.
        source.floors = [{ id, overlay: false, rgb, textureId, secondaryRgb: null, textureScale: 512 }];
        source.planes[0].underlays[0] = id;
        const unloaded = buildMapScene([source], new Map(), new Map(), [true], false).geometry;
        const texture: ModelTexture = { image: {} as ImageBitmap, alphaMode: 0, colorBlend: 0, brightness: 0 };
        const loaded = buildMapScene([source], new Map(), new Map([[textureId, texture]]), [true], false).geometry;
        if (!loaded || !unloaded || !loaded.colors) throw new Error("Expected coloured terrain");
        expect(loaded.textureIds).toEqual(Array(4).fill(textureId));
        expect(loaded.colors).toEqual(unloaded.colors);
        const colors = loaded.colors;
        tint.forEach((value, channel) => { expect(colors[channel]).toBeCloseTo(value, 10); });
        expect(loaded.positions).toEqual(unloaded.positions);
        expect(loaded.uvs).toEqual(unloaded.uvs);
    });
    it("samples authoritative slopes and constructs four planes at legacy scale", () => {
        const source = region();
        expect(mapHeight([source], 0, 64.5, 128.5)).toBe(-48);
        expect(mapHeight([source], 1, 64.5, 128.5)).toBe(-1008);
        const scene = buildMapScene([source], new Map(), new Map(), [true, true, true, true], false);
        expect(scene.geometry?.indices).toHaveLength(48);
        expect(scene.geometry?.positions.slice(0, 9)).toEqual([0, 0, 0, 256, 48, 256, 512, 32, 0]);
        expect(Math.max(...(scene.geometry?.positions.filter((_, i) => i % 3 === 1) ?? []))).toBe(2976);
        expect(scene.geometry?.colors?.slice(0, 3)).toEqual([0.8796223968878317, 0.03189603307301153, 0.01599629336550963]);
    });
    it("positions adjacent regions together and rotates actual overlay shapes", () => {
        const first = region(),
            second = region(514);
        first.floors.push({ id: 1, overlay: true, rgb: 0x0000ff, textureId: -1, secondaryRgb: null, textureScale: 512 });
        first.planes[0].overlays[0] = 1;
        first.planes[0].overlayShapes[0] = 2;
        first.planes[0].overlayRotations[0] = 1;
        const scene = buildMapScene([first, second], new Map(), new Map(), [true], false);
        expect(scene.geometry?.positions.slice(0, 9)).toEqual([512, 96, 512, 512, 64, 256, 0, 64, 512]);
        expect(scene.geometry?.colors?.slice(0, 3)).toEqual([0.01599629336550963, 0.021219010376003555, 0.8796223968878317]);
        expect(Math.max(...(scene.geometry?.positions.filter((_, i) => i % 3 === 0) ?? []))).toBe(64 * 512 + 512);
    });
    it("blends a spreading overlay into a neighbouring underlay's shared corners only", () => {
        const build = (spreadsToNeighbours: boolean) => {
            const source = region();
            source.floors.push({ id: 1, overlay: true, rgb: 0x0000ff, textureId: 7, secondaryRgb: null, textureScale: 512, overlayProperties: { priority: 8, spreadsToNeighbours, waterFogRgb: 0x122b3d, waterFogScale: 512, waterFogBias: 255, waterParameter20: 63, waterParameter21: 0, waterParameter22: 64 } });
            source.planes[0].underlays[64] = 1;
            source.planes[0].overlays[64] = 1;
            return required(buildMapScene([source], new Map(), new Map(), [true], false).geometry, "the terrain");
        };
        const blue = [0.01599629336550963, 0.021219010376003555, 0.8796223968878317];
        const hasBlue = (colors: readonly number[], count: number) => colors.slice(0, count * 3).some((value, index) => index % 3 === 2 && Math.abs(value - blue[2]) < 1e-9);
        // Tile 0 is drawn first: its four underlay triangles precede tile 64's.
        expect(hasBlue(required(build(false).colors, "colours"), 12)).toBe(false);
        const spread = build(true);
        expect(hasBlue(required(spread.colors, "colours"), 12)).toBe(true);
        expect(spread.terrainBlends?.length).toBeGreaterThan(0);
    });
    it("draws the underwater layer under surface tiles and fogs it from the surface overlay", () => {
        const build = (waterFogBias: number, visible = true) => {
            const source = region();
            source.floors.push({ id: 2, overlay: true, rgb: 0x0000ff, textureId: -1, secondaryRgb: null, textureScale: 512, overlayProperties: { priority: 8, spreadsToNeighbours: false, waterFogRgb: 0x00ff00, waterFogScale: 51200, waterFogBias, waterParameter20: 63, waterParameter21: 0, waterParameter22: 64 } });
            source.floors.push({ id: 3, overlay: false, rgb: 0xff0000, textureId: 7, secondaryRgb: null, textureScale: 512 });
            source.planes[0].overlays[0] = 2;
            const heights = source.planes[0].cornerHeights.map(height => (height ?? 0) + 200);
            source.planes.push({ ...source.planes[0], cornerHeights: heights, overlays: Array(4096).fill(0), underlays: [3, ...Array(4095).fill(0)] });
            return required(buildMapScene([source], new Map(), new Map(), [true, false, false, false, visible], false).geometry, "the terrain");
        };
        const surfaceVertices = 12;
        const seabedY = (geometry: ReturnType<typeof build>) => geometry.positions[surfaceVertices * 3 + 1];
        const partial = build(0), opaque = build(255);
        expect(partial.positions.length / 3).toBe(surfaceVertices + 12);
        expect(partial.waterDepth).toEqual([...Array(surfaceVertices).fill(199), ...Array(12).fill(0)]);
        expect(seabedY(partial)).toBe(partial.positions[1] - 200);
        expect(partial.textureIds?.[4]).toBe(7);
        expect(partial.colors?.[surfaceVertices * 3 + 1]).toBeGreaterThan(0);
        expect(partial.colors?.[surfaceVertices * 3 + 1]).toBeLessThan(1);
        expect(opaque.textureIds?.[4]).toBe(-1);
        expect(opaque.colors?.slice(surfaceVertices * 3, surfaceVertices * 3 + 3)).toEqual([0, 1, 0]);
        expect(build(255, false).positions.length / 3).toBe(surfaceVertices);
    });
    it("uses the client zero-array fallback and masks nonqualifying shoreline vertices", () => {
        const source = region();
        source.floors.push({ id: 2, overlay: true, rgb: 0x0000ff, textureId: 7, secondaryRgb: null, textureScale: 512 });
        source.planes[0].overlays[0] = 2;
        source.planes[0].overlayShapes[0] = 1;
        const build = (regions: MapRenderRegion[], preparation = new MapScenePreparation()) =>
            required(buildMapScene(regions, new Map(), new Map(), [true], false, preparation).geometry, "terrain");
        const dry = build([source]);
        expect(dry.waterDepth).toEqual(Array(dry.positions.length / 3).fill(0));

        const planes = source.planes;
        planes.push({ ...planes[0], cornerHeights: planes[0].cornerHeights.map(height => height === null ? null : height + 200) });
        const regions = [source];
        const preparation = new MapScenePreparation();
        const wet = build(regions, preparation);
        expect(new Set(wet.waterDepth)).toEqual(new Set([-1, 199]));
        expect(wet.waterDepth).toHaveLength(wet.positions.length / 3);
        expect(build(regions, preparation).waterDepth).toBe(wet.waterDepth);
    });
    it.each([
        [7, 0, 256],
        [9, 256, 384],
    ])("masks the client's shoreline vertex on shape %i at (%i, %i)", (shape, pointX, pointZ) => {
        const source = region();
        source.floors.push({ id: 2, overlay: true, rgb: 0x0000ff, textureId: 7, secondaryRgb: null, textureScale: 512 });
        source.planes[0].overlays[0] = 2;
        source.planes[0].overlayShapes[0] = shape;
        source.planes.push({ ...source.planes[0], cornerHeights: source.planes[0].cornerHeights.map(height => height === null ? null : height + 200) });
        const terrain = required(buildMapScene([source], new Map(), new Map(), [true], false, new MapScenePreparation()).geometry, "terrain");
        const xs = terrain.positions.filter((_, index) => index % 3 === 0);
        const zs = terrain.positions.filter((_, index) => index % 3 === 2);
        const [originX, originZ] = [Math.min(...xs), Math.min(...zs)];
        const depths = terrain.waterDepth?.filter((_, vertex) => xs[vertex] === originX + pointX && zs[vertex] === originZ + pointZ);
        expect(depths?.length).toBeGreaterThan(0);
        expect(new Set(depths)).toEqual(new Set([-1]));
    });
    it("removes only water-effect floors when the water surface is hidden and rebuilds on toggle", () => {
        const source = region();
        source.floors.push({ id: 2, overlay: true, rgb: 0x0000ff, textureId: 7, secondaryRgb: null, textureScale: 512 }, { id: 3, overlay: true, rgb: 0x00ff00, textureId: 8, secondaryRgb: null, textureScale: 512 });
        source.planes[0].overlays[0] = 2;
        source.planes[0].overlays[1] = 3;
        const textures = new Map<number, ModelTexture>([[7, { image: {} as ImageBitmap, alphaMode: 0, effectId: 4 }], [8, { image: {} as ImageBitmap, alphaMode: 0 }]]);
        const preparation = new MapScenePreparation();
        const vertices = (waterSurface: boolean) => buildMapScene([source], new Map(), textures, [true], false, preparation, null, undefined, { terrain: true, waterSurface, objectShadows: true, hiddenShapes: new Set(), hiddenPlacements: new Set() }).geometry?.positions.length;
        expect(vertices(true)).toBe(24 * 3);
        expect(vertices(false)).toBe(12 * 3);
        expect(vertices(true)).toBe(24 * 3);
    });
    it("hides terrain, object shapes and single placements through the scene filter", () => {
        const source = region();
        const appearances = new Map([[mapAppearanceKey({ id: 1, shape: 10, rotation: 0 }), appearance()]]);
        const placed = { id: 1, shape: 10, rotation: 0, x: 0, y: 0, plane: 0 };
        source.objects = [placed];
        const filter = (overrides: object) => ({ terrain: true, waterSurface: true, objectShadows: true, hiddenShapes: new Set<number>(), hiddenPlacements: new Set<typeof placed>(), ...overrides });
        const preparation = new MapScenePreparation();
        const placements = (overrides: object) => buildMapScene([source], appearances, new Map(), [true], true, preparation, null, undefined, filter(overrides)).geometry?.placements.length;
        expect(placements({})).toBe(1);
        expect(placements({ hiddenShapes: new Set([10]) })).toBe(0);
        expect(placements({ hiddenPlacements: new Set([placed]) })).toBe(0);
        const noTerrain = buildMapScene([source], appearances, new Map(), [true], true, preparation, null, undefined, filter({ terrain: false })).geometry;
        expect(noTerrain?.placements[0].startVertex).toBe(0);
    });
    it("omits unavailable boundaries and absent floors rather than inventing geometry", () => {
        const source = region();
        source.planes[0].cornerHeights[0] = null;
        expect(buildMapScene([source], new Map(), new Map(), [true], false).geometry).toBeNull();
        source.floors = [];
        expect(buildMapScene([source], new Map(), new Map(), [true], false).geometry).toBeNull();
        expect(mapHeight([source], 0, 10000, 10000)).toBeNull();
        expect(buildMapScene([], new Map(), new Map(), [true], true).geometry).toBeNull();
    });
    it("contours each placement without mutating a reusable appearance", () => {
        const source = region(),
            asset = appearance();
        source.planes[0].underlays[0] = 0;
        const first = { id: 1, shape: 10, rotation: 0, x: 0, y: 0, plane: 0 };
        source.objects = [first, { ...first, x: 1 }];
        const scene = buildMapScene([source], new Map([[mapAppearanceKey(first), asset]]), new Map(), [true], true);
        expect(scene.geometry?.positions.slice(0, 9)).toEqual([256, 48, 256, 256, 560, 256, 768, 80, 256]);
        expect(scene.geometry?.positions.slice(9, 18)).toEqual([768, 80, 256, 768, 592, 256, 1280, 112, 256]);
        expect(asset.parts[0].positions).toEqual([0, 0, 0, 512, 0, 0, 0, -512, 0]);
        expect(buildMapScene([source], new Map([[mapAppearanceKey(first), asset]]), new Map(), [true], false).geometry).toBeNull();
        asset.contourMode = 3;
        expect(buildMapScene([source], new Map([[mapAppearanceKey(first), asset]]), new Map(), [true], true).warnings).toContain(
            "Object 1: unsupported contour mode 3."
        );
    });
    it("contours objects to terrain edits without touching the stored region", () => {
        const source = region(),
            placement = { id: 1, shape: 10, rotation: 0, x: 0, y: 0, plane: 0 };
        source.planes[0].underlays[0] = 0;
        source.objects = [placement];
        const raised = new Map(Array.from({ length: 16 }, (_, tile) => [(tile >> 2) << 6 | (tile & 3), { height: 40 }] as const));
        const [preview] = applyTerrainEdits([source], new Map([[source.id, raised]]), new Map());
        const appearances = new Map([[mapAppearanceKey(placement), appearance()]]);
        expect(buildMapScene([source], appearances, new Map(), [true], true).geometry?.positions.slice(0, 9)).toEqual([256, 48, 256, 256, 560, 256, 768, 80, 256]);
        expect(buildMapScene([preview], appearances, new Map(), [true], true).geometry?.positions.slice(0, 9)).toEqual([256, 1280, 256, 256, 1792, 256, 768, 1280, 256]);
    });

    it("uses wall displacement for decorations and keeps paired decoration parts independent", () => {
        const source = region(),
            asset = appearance();
        source.planes[0].underlays[0] = 0;
        asset.contourMode = 0;
        asset.parts.push(asset.parts[0]);
        const wall = { id: 2, shape: 0, rotation: 0, x: 0, y: 0, plane: 0 };
        const decoration = { ...wall, id: 1, shape: 8 };
        source.objects = [wall, decoration];
        const wallAsset = { ...appearance(), decorDisplacement: 80, parts: [] };
        const scene = buildMapScene(
            [source],
            new Map([
                [mapAppearanceKey(wall), wallAsset],
                [mapAppearanceKey(decoration), asset],
            ]),
            new Map(),
            [true],
            true
        );
        expect(scene.geometry?.positions.slice(0, 3)).toEqual([297, 48, 215]);
        expect(scene.geometry?.positions.slice(9, 12)).toEqual([256, 48, 256]);
        expect(buildMapScene([source], new Map([[mapAppearanceKey(decoration), asset]]), new Map(), [false], true).geometry).toBeNull();
    });
    it("tapers contouring at the configured height and reports unavailable placement terrain", () => {
        const source = region(),
            asset = appearance();
        source.planes[0].underlays[0] = 0;
        asset.contourMode = 2;
        asset.contourParameter = 32768;
        const placement = { id: 1, shape: 10, rotation: 1, x: 0, y: 0, plane: 0 };
        source.objects = [placement];
        asset.sizeX = 2;
        const scene = buildMapScene([source], new Map([[mapAppearanceKey(placement), asset]]), new Map(), [true], true);
        expect(scene.geometry?.positions.slice(0, 9)).toEqual([256, 80, 512, 256, 592, 512, 768, 112, 512]);
        source.planes = [];
        const missing = buildMapScene([source], new Map([[mapAppearanceKey(placement), asset]]), new Map(), [true], true);
        expect(missing.geometry).toBeNull();
        expect(missing.warnings).toEqual(["Region 258: some objects require unavailable terrain heights."]);
    });
});

