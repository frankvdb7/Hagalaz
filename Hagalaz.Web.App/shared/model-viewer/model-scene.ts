import * as THREE from "three";
import { OrbitControls } from "three/addons/controls/OrbitControls.js";
import { clientFixedWater, clientHdWater, clientReflection, fixedWaterParameters, isFixedWater, sampleMaterialEffect, type MaterialEffect } from "./material-effects";
import {ModelParticles, type ModelParticle, type PosedEffector} from "./model-particles";
import { ModelBillboards, type RenderBillboard } from "./model-billboards";
import { animationTickMs } from "./animation-tween";
import { OrientationGizmo } from "./orientation-gizmo";
import { PlacementGizmo, type PlacementGizmoTarget } from "./placement-gizmo";
import { applyTerrainMaterials, TerrainMaterial, terrainSurfaceMaterial, type TerrainBlend } from "./terrain-material";
import { MapLightMaterial, type MapLightField } from "./map-light-material";
import { MapPointerNavigation } from "./map-pointer-navigation";
import { waterTurbulence, waterTurbulenceRows, waterVolume, waterVolumeSize, waterVolumeSlices, type WaterVolume } from "./water-noise";

type SurfaceMaterial = THREE.MeshStandardMaterial | THREE.MeshBasicMaterial;

/** Renderer-neutral geometry accepted by both web applications. */
export interface ModelGeometry {
    sourceId?: number;
    preparedLighting?: boolean;
    positions: readonly number[] | Float32Array;
    indices: readonly number[] | Uint16Array | Uint32Array;
    colors?: readonly number[] | Float32Array;
    opacities?: readonly number[] | Float32Array;
    vertexRgba?: Float32Array;
    uvs?: readonly number[] | Float32Array;
    firstTexel?: readonly number[] | Uint8Array;
    textureIds?: readonly number[] | Int32Array;
    terrainBlends?: readonly TerrainBlend[];
    terrainWeights?: readonly number[] | Float32Array;
    mapPlanes?: readonly number[] | Uint8Array;
    waterDepth?: readonly number[] | Float32Array;
    facePriorities?: readonly number[] | Int16Array;
    particles?: readonly ModelParticle[];
    effectors?: readonly PosedEffector[];
    billboards?: readonly RenderBillboard[];
    particleScale?: number;
    animations?: readonly ModelAnimationRange[];
    /** This geometry equals `base` except for the vertices in `ranges`, as [start, end) pairs. */
    patchOf?: { base: ModelGeometry; ranges: readonly (readonly [number, number])[] };
}

/** A contiguous set of map triangles whose vertices can be replaced together. */
export interface ModelAnimationRange {
    startVertex: number;
    vertexCount: number;
    /** The map plane the range belongs to; hidden planes do not animate. */
    plane?: number;
    alphaFaces: readonly boolean[];
    startBillboard?: number;
    billboardCount?: number;
    /** The renderer copies the pose before sampling another range. */
    sample(elapsed: number, previousElapsed?: number): { delay: number; pose?: { positions: Float32Array; rgba: Float32Array; billboards?: readonly RenderBillboard[] } };
}

/** Tile edges of one plane's terrain, so the grid marks the active plane. */
function mapGridGeometry({ positions, indices, mapPlanes }: ModelGeometry, plane: number): THREE.BufferGeometry {
    const tileSize = 512;
    const lines: number[] = [];
    for (let face = 0; face < indices.length; face += 3) {
        if (mapPlanes && mapPlanes[indices[face]] !== plane) continue;
        for (let edge = 0; edge < 3; edge++) {
            const from = indices[face + edge] * 3;
            const to = indices[face + (edge + 1) % 3] * 3;
            const x = positions[from], z = positions[from + 2];
            if ((x === positions[to] && x % tileSize === 0) || (z === positions[to + 2] && z % tileSize === 0)) {
                // Lift the lines slightly so they don't fight the terrain for depth.
                lines.push(x, positions[from + 1] + 2, z, positions[to], positions[to + 1] + 2, positions[to + 2]);
            }
        }
    }
    return new THREE.BufferGeometry().setAttribute("position", new THREE.Float32BufferAttribute(lines, 3));
}

export interface StrokePreviewGeometry {
    positions: number[];
    uvs: number[];
    rgb: number[];
    textureId: number | null;
    opacity: number;
}

/** One mesh and the state that belongs to it. */
interface MeshSurface {
    mesh?: THREE.Mesh<THREE.BufferGeometry, SurfaceMaterial[]>;
    currentModel?: ModelGeometry;
    orderingDirty: boolean;
    sourceIndices: ModelGeometry["indices"];
    /** Opaque draw ranges, laid out by material and then by chunk, so the chunks in view merge into few draw calls. */
    opaqueGroups: { start: number; count: number; materialIndex: number; chunk: number }[];
    blendedSurfaces: { sourceStart: number; material: number; depth: number; bin: number; priority: number; chunk: number }[];
    /** Bounds of each spatial chunk, in row order. A surface with one chunk is never culled. */
    chunks: THREE.Box3[];
    /** Chunks in the camera frustum at the last cull, or undefined while all of them are drawn. */
    visibleChunks?: Uint8Array;
    /** The draw ranges must be rewritten for a new set of visible chunks. */
    groupsDirty?: boolean;
    materials: Map<string, { material: SurfaceMaterial; texture?: THREE.Texture; asset?: ModelTexture }>;
    texturesDirty: boolean;
    animatedWater: boolean;
    /** The chunks holding faces with animated water. */
    waterChunks: number[];
    /** The draw index needs a whole upload at the next render, so a re-sort must not narrow it to a range. */
    wholeIndexPending?: boolean;
}

interface SurfaceHit { surface: MeshSurface; vertex: number; plane?: number; x: number; z: number; distance: number; }

/** The active plane carries the grid; see-through and hidden planes let picks through to the floors below. */
interface PlaneFocus { plane: number; ghosted: ReadonlySet<number>; hidden: ReadonlySet<number>; }

/**
 * Translucent faces sort back to front in depth slabs, by material within a slab. Exact per-face order split large maps
 * into one draw call per material change, tens of thousands per frame. Each slab spans this fraction of its distance from
 * the camera, so close faces keep a fine order and distant ones, whose errors are too small to see, share draw calls.
 * Slabs only grow with depth, so pans and zooms keep a valid back-to-front order without a re-sort.
 */
const blendedSlabGrowth = Math.log(1.02);
/** Faces closer than this share the nearest slab. */
const nearestSlabDistance = 64;
/** About a quarter degree: smaller turns, and pans, leave translucent order unchanged. */
const reorderTurn = 1e-5;
/** Milliseconds between translucent re-sorts while a map camera keeps turning. */
const reorderInterval = 120;
/** Map geometry is culled in square chunks of 16 tiles; smaller chunks would split the draw calls of a full view further. */
const cullChunkSize = 16 * 512;
/** Animated parts may move a little beyond the bounds of their rest pose. */
const cullChunkMargin = 512;
/** Animated map objects smaller than this many pixels on screen, or outside the view, keep their last pose. */
const minAnimatedPixels = 4;
/**
 * Animated water redraws the whole map. Water at least this many pixels across on screen redraws every display frame,
 * smaller visible water at `smallWaterInterval`, and water under `minAnimatedPixels` or out of view not at all.
 */
const fullRateWaterPixels = 96;
const smallWaterInterval = 100;

function createMeshSurface(): MeshSurface {
    return { orderingDirty: true, sourceIndices: [], opaqueGroups: [], blendedSurfaces: [], chunks: [], waterChunks: [], materials: new Map(), texturesDirty: false, animatedWater: false };
}

export interface ModelTexture extends MaterialEffect {
    image: ImageBitmap;
    alphaMode: 0 | 1 | 2;
    colorBlend?: number;
    brightness?: number;
    repeatU?: boolean;
    repeatV?: boolean;
    hdr?: {width: number; height: number; rgba: number[]};
}

export interface RegionSun {
    lightRgb: number;
    directionX: number;
    directionY: number;
    directionZ: number;
    intensity?: number;
    fogValue?: number;
}

const defaultRegionSun: RegionSun = { lightRgb: 0xffffff, directionX: -200, directionY: -240, directionZ: -200, intensity: 1.1523438 };

function isHdWater(effectId: number | undefined): effectId is 8 | 9 {
    return effectId === 8 || effectId === 9;
}

function isAnimatedFixedWater({ effectId, effectParameter = 0 }: MaterialEffect): boolean {
    return isFixedWater(effectId) && (effectId !== 4 || (effectParameter & 1) !== 0);
}

/** Map objects already carry the region tint in their vertices. Only terrain takes the effect 2 ambient term in its program. */
function hasBakedRegionTint(model: ModelGeometry): boolean {
    return !!model.mapPlanes && !model.waterDepth;
}

/** Owns the canvas and GPU resources for a single model inspection surface. */
export class ModelScene {
    particlePlaybackSpeed = 1;
    private readonly renderer: THREE.WebGLRenderer;
    private readonly scene = new THREE.Scene();
    private readonly camera = new THREE.PerspectiveCamera(40, 1, 0.01, 100000);
    private readonly controls: OrbitControls;
    private readonly mapNavigation?: MapPointerNavigation;
    private readonly resizeObserver: ResizeObserver;
    /** Holds the model, or a map scene's objects. */
    private readonly primary = createMeshSurface();
    /** Only map scenes use it, so strokes can replace the ground without touching objects. */
    private readonly terrain = createMeshSurface();
    /**
     * A map scene's animated objects, copied out of the primary surface and drawn here instead. Updating part of a large
     * static buffer stalled the GPU in proportion to the whole buffer, so animation ticks only upload this small one.
     */
    private readonly animated = createMeshSurface();
    /** Where each animation range of the primary model starts in the animated surface. */
    private animatedRanges: { range: ModelAnimationRange; start: number }[] = [];
    private readonly animatedParts = new WeakMap<ModelGeometry, ModelGeometry>();
    private readonly animationBounds = new WeakMap<ModelAnimationRange, THREE.Sphere>();
    /** Whether the last tick left ranges unposed because they were out of view, and the camera it judged that from. */
    private animationViewSkipped = false;
    private readonly animationCamera = new THREE.Matrix4();
    private readonly mapLighting = new MapLightMaterial();
    private readonly viewDepthAxis = new THREE.Vector3(0, 0, -1);
    private epoch?: number;
    private lastTime?: number;
    private particleTickRemainder = 0;
    private particles?: ModelParticles;
    private billboards?: ModelBillboards;
    private readonly effectUpdates = new Map<THREE.Material, (seconds: number) => void>();
    private disposed = false;
    private textureImages?: ReadonlyMap<number, ModelTexture>;
    private readonly textures = new Map<number, THREE.Texture>();
    private readonly waterTextures = new Map<WaterVolume, THREE.IUniform<THREE.Data3DTexture | null>>();
    private turbulenceTexture?: THREE.DataTexture;
    private hdWaterActive = false;
    private readonly sunDirection = { value: new THREE.Vector3() };
    private readonly sunColor = { value: new THREE.Vector3(1, 1, 1) };
    private readonly waterAmbient = { value: new THREE.Vector3(1.1523438, 1.1523438, 1.1523438) };
    private readonly waterFogWidth = { value: 1 };
    private grid?: THREE.GridHelper;
    private readonly gridFittedTo = new THREE.Box3();
    private mapGrid?: THREE.LineSegments<THREE.BufferGeometry, THREE.LineBasicMaterial>;
    private mapGridTerrain?: ModelGeometry;
    private mapGridPlane?: number;
    private planeFocus: PlaneFocus = { plane: 0, ghosted: new Set(), hidden: new Set() };
    private lastBlendedOrder = -Infinity;
    private readonly frustum = new THREE.Frustum();
    private readonly viewProjection = new THREE.Matrix4();
    private reorderTimer?: ReturnType<typeof setTimeout>;
    private waterTimer?: ReturnType<typeof setTimeout>;
    private showMapGrid = false;
    private orientationGizmo?: OrientationGizmo;
    private selectionBounds?: THREE.Box3Helper;
    private placementGizmo?: PlacementGizmo;
    private brushOutline?: THREE.LineLoop<THREE.BufferGeometry, THREE.LineBasicMaterial>;
    private strokePreview?: THREE.Mesh<THREE.BufferGeometry, THREE.MeshBasicMaterial>;
    private tileOverlay?: THREE.Mesh<THREE.BufferGeometry, THREE.MeshBasicMaterial>;
    private wireframe = false;
    private pendingFrame?: number;
    private animationTimer?: ReturnType<typeof setTimeout>;
    private objectAnimationsEnabled = true;
    private objectAnimationEpoch?: number;
    private lastObjectAnimationTime?: number;
    private nextObjectAnimationTime = 0;
    private environmentFaces?: readonly ModelTexture[];
    // Shared by the reflective material programs.
    private readonly environmentMap = { value: null as THREE.CubeTexture | null };
    private readonly environmentEnabled = { value: false };

    private readonly requestRender = (): void => {
        if (this.framing !== "map" || this.disposed || this.pendingFrame !== undefined) return;
        this.pendingFrame = requestAnimationFrame(time => {
            try { this.renderFrame(time); }
            finally { this.pendingFrame = undefined; }
            this.scheduleWaterFrame();
        });
    };

    /** Redraws animated water at a rate that follows how large it is on screen. Camera changes redraw on their own. */
    private scheduleWaterFrame(): void {
        const delay = this.waterFrameDelay();
        if (delay === 0) this.requestRender();
        else if (delay < Infinity) this.waterTimer ??= setTimeout(() => {
            this.waterTimer = undefined;
            this.requestRender();
        }, delay);
    }

    private waterFrameDelay(): number {
        const pixels = Math.max(0, ...this.surfaces.filter(surface => surface.mesh?.visible && surface.animatedWater).map(surface => this.visibleWaterPixels(surface)));
        if (pixels >= fullRateWaterPixels) return 0;
        return pixels >= minAnimatedPixels ? smallWaterInterval : Infinity;
    }

    /** The on-screen size of the largest water chunk of a surface in view, in pixels. */
    private visibleWaterPixels(surface: MeshSurface): number {
        const pixelsPerUnit = this.host.clientHeight / (2 * Math.tan(THREE.MathUtils.degToRad(this.camera.fov / 2)));
        const sphere = new THREE.Sphere();
        let pixels = 0;
        for (const box of surface.waterChunks.map(chunk => surface.chunks[chunk])) {
            // The frame's culling pass has just set the frustum.
            if (!box || !this.frustum.intersectsBox(box)) continue;
            box.getBoundingSphere(sphere);
            pixels = Math.max(pixels, sphere.radius * 2 * pixelsPerUnit / Math.max(sphere.center.distanceTo(this.camera.position), 1));
        }
        return pixels;
    }

    showOrientationGizmo(): void {
        if (this.disposed || this.orientationGizmo) return;
        this.orientationGizmo = new OrientationGizmo(this.camera);
        this.requestRender();
    }

    /** The primary surface's vertex under the point, unless other geometry is in front of it. */
    pickVertex(clientX: number, clientY: number): number | null {
        const hit = this.nearestHit(clientX, clientY, this.surfaces);
        if (hit?.surface === this.primary) return hit.vertex;
        if (hit?.surface !== this.animated) return null;
        const entry = this.animatedRanges.find(({ range, start }) => hit.vertex >= start && hit.vertex < start + range.vertexCount);
        return entry ? entry.range.startVertex + hit.vertex - entry.start : null;
    }

    /** The ground under the point, looking through objects. */
    pickSurface(clientX: number, clientY: number): { vertex: number; x: number; z: number } | null {
        const hit = this.nearestHit(clientX, clientY, [this.terrain]);
        return hit ? { vertex: hit.vertex, x: hit.x, z: hit.z } : null;
    }

    /** The map planes of every pickable surface under the point, nearest first. */
    pickPlanes(clientX: number, clientY: number): number[] {
        const ray = this.pointerRay(clientX, clientY);
        if (!ray) return [];
        const hits = this.surfaces.flatMap(surface => pickableHits(ray, surface, this.planeFocus)).sort((left, right) => left.distance - right.distance);
        return [...new Set(hits.flatMap(hit => hit.plane === undefined ? [] : [hit.plane]))];
    }

    /**
     * Marks `plane` with the grid, draws `ghosted` planes see-through and `hidden` planes not at all; picks pass both.
     * Geometry and materials stay as they are, so toggling a plane costs no rebuild.
     */
    setPlaneFocus(plane: number, ghosted: readonly number[], hidden: readonly number[] = []): void {
        const shown = [...this.planeFocus.hidden].some(previous => !hidden.includes(previous));
        this.planeFocus = { plane, ghosted: new Set(ghosted), hidden: new Set(hidden) };
        // Animations on a hidden plane were skipped without scheduling a tick, so a shown plane must resume them now.
        if (shown) this.nextObjectAnimationTime = 0;
        this.mapLighting.setPlaneMasks(ghosted, hidden);
        this.updateMapGrid();
        this.requestRender();
    }

    /** Centres the camera on the geometry of one plane, keeping its distance and direction. False when the plane draws nothing. */
    focusMapPlane(plane: number): boolean {
        const box = new THREE.Box3();
        const point = new THREE.Vector3();
        for (const { mesh, currentModel } of this.surfaces) {
            const planes = currentModel?.mapPlanes;
            if (!mesh || !planes) continue;
            const position = mesh.geometry.getAttribute("position");
            for (let vertex = 0; vertex < position.count; vertex++) if (planes[vertex] === plane) box.expandByPoint(point.fromBufferAttribute(position, vertex));
        }
        if (box.isEmpty()) return false;
        const offset = this.camera.position.clone().sub(this.controls.target);
        box.getCenter(this.controls.target);
        this.camera.position.copy(this.controls.target).add(offset);
        this.controls.update();
        this.requestRender();
        return true;
    }

    private nearestHit(clientX: number, clientY: number, surfaces: readonly MeshSurface[]): SurfaceHit | null {
        const ray = this.pointerRay(clientX, clientY);
        if (!ray) return null;
        const hits = surfaces.flatMap(surface => pickableHits(ray, surface, this.planeFocus).slice(0, 1));
        return hits.reduce<SurfaceHit | null>((nearest, hit) => !nearest || hit.distance < nearest.distance ? hit : nearest, null);
    }

    private pointerRay(clientX: number, clientY: number): THREE.Raycaster | null {
        const { left, top, width, height } = this.host.getBoundingClientRect();
        if (!width || !height) return null;
        this.orderSurfaces();
        const ray = new THREE.Raycaster();
        ray.setFromCamera(new THREE.Vector2(2 * (clientX - left) / width - 1, 1 - 2 * (clientY - top) / height), this.camera);
        return ray;
    }

    showVertexBounds(range: { startVertex: number; endVertex: number } | null): void {
        const mesh = this.primary.mesh;
        if (!range || !mesh) {
            if (this.selectionBounds?.visible) {
                this.selectionBounds.visible = false;
                this.requestRender();
            }
            return;
        }
        if (!this.selectionBounds) {
            const helper = new THREE.Box3Helper(new THREE.Box3(), 0xffb800);
            const material = helper.material as THREE.LineBasicMaterial;
            material.depthTest = false;
            material.depthWrite = false;
            material.transparent = true;
            helper.renderOrder = 1;
            this.scene.add(helper);
            this.selectionBounds = helper;
        }
        const position = mesh.geometry.getAttribute("position");
        const bounds = this.selectionBounds.box.makeEmpty();
        const point = new THREE.Vector3();
        for (let vertex = range.startVertex; vertex < range.endVertex; vertex++) {
            bounds.expandByPoint(point.fromBufferAttribute(position, vertex));
        }
        bounds.expandByScalar(8);
        this.selectionBounds.visible = true;
        this.requestRender();
    }

    /** Shows move, rotate or scale handles on a placement, or removes them. */
    showPlacementGizmo(target: PlacementGizmoTarget | null): void {
        if (!target && !this.placementGizmo) return;
        this.placementGizmo ??= new PlacementGizmo({
            scene: this.scene,
            camera: this.camera,
            element: this.renderer.domElement,
            positions: () => this.primary.mesh?.geometry.getAttribute("position") as THREE.BufferAttribute | undefined,
            setOrbitEnabled: enabled => { this.controls.enabled = enabled; },
            rangeMoved: range => this.showVertexBounds(range),
            redraw: this.requestRender,
        });
        this.placementGizmo.show(target);
        this.requestRender();
    }

    get placementGizmoDragging(): boolean {
        return this.placementGizmo?.dragging ?? false;
    }

    /** Draws a closed line over the scene from x, y, z triples, or removes it. */
    setBrushOutline(points: readonly number[] | null): void {
        if (!points) {
            if (this.brushOutline?.visible) {
                this.brushOutline.visible = false;
                this.requestRender();
            }
            return;
        }
        if (!this.brushOutline) {
            this.brushOutline = new THREE.LineLoop(new THREE.BufferGeometry(), new THREE.LineBasicMaterial({ color: 0xffb800, depthTest: false, depthWrite: false, transparent: true }));
            this.brushOutline.renderOrder = 1;
            this.brushOutline.frustumCulled = false;
            this.scene.add(this.brushOutline);
        }
        this.brushOutline.geometry.setAttribute("position", new THREE.Float32BufferAttribute(points, 3));
        this.brushOutline.visible = true;
        this.requestRender();
    }

    /** Draws an in-progress stroke over the ground, or removes it. Cost follows the tiles drawn, not the scene. */
    setStrokePreview(preview: StrokePreviewGeometry | null): void {
        if (!preview) {
            if (this.strokePreview?.visible) {
                this.strokePreview.visible = false;
                this.requestRender();
            }
            return;
        }
        this.strokePreview ??= this.createTileMesh(1, -4);
        this.fillTileMesh(this.strokePreview, preview);
        this.strokePreview.visible = true;
        this.requestRender();
    }

    /**
     * Draws per-tile markers over the ground, under any stroke preview, or removes them and releases their
     * buffers. It is separate from the terrain, so showing or changing it rebuilds nothing else.
     */
    setTileOverlay(overlay: StrokePreviewGeometry | null): void {
        if (!overlay) {
            if (!this.tileOverlay) return;
            this.releaseTileMesh(this.tileOverlay);
            this.tileOverlay = undefined;
        } else {
            this.tileOverlay ??= this.createTileMesh(0.5, -2);
            this.fillTileMesh(this.tileOverlay, overlay);
        }
        this.requestRender();
    }

    private createTileMesh(renderOrder: number, polygonOffset: number): THREE.Mesh<THREE.BufferGeometry, THREE.MeshBasicMaterial> {
        const material = new THREE.MeshBasicMaterial({ vertexColors: true, side: THREE.DoubleSide, depthWrite: false, polygonOffset: true, polygonOffsetFactor: polygonOffset, polygonOffsetUnits: polygonOffset });
        const mesh = new THREE.Mesh(new THREE.BufferGeometry(), material);
        mesh.frustumCulled = false;
        mesh.renderOrder = renderOrder;
        this.scene.add(mesh);
        return mesh;
    }

    private fillTileMesh({ geometry, material }: THREE.Mesh<THREE.BufferGeometry, THREE.MeshBasicMaterial>, tiles: StrokePreviewGeometry): void {
        geometry.setAttribute("position", new THREE.Float32BufferAttribute(tiles.positions, 3));
        geometry.setAttribute("uv", new THREE.Float32BufferAttribute(tiles.uvs, 2));
        geometry.setAttribute("color", new THREE.BufferAttribute(strokePreviewColors(tiles.rgb), 3));
        const map = (tiles.textureId === null ? undefined : this.textures.get(tiles.textureId)) ?? null;
        if (material.map !== map) {
            material.map = map;
            material.needsUpdate = true;
        }
        material.opacity = tiles.opacity;
        material.transparent = tiles.opacity < 1;
    }

    private releaseTileMesh(mesh: THREE.Mesh<THREE.BufferGeometry, THREE.MeshBasicMaterial>): void {
        this.scene.remove(mesh);
        mesh.geometry.dispose();
        mesh.material.dispose();
    }

    /** While off, a primary drag or one-finger touch is left to the page instead of orbiting. */
    setPrimaryDragEnabled(enabled: boolean): void {
        this.controls.mouseButtons.LEFT = enabled ? THREE.MOUSE.ROTATE : null;
        this.controls.touches.ONE = enabled ? THREE.TOUCH.ROTATE : null;
    }

    /** Draws a frame and reads it in the same task, because the drawing buffer is cleared once the browser composites. */
    captureImage(): string {
        const handles = this.placementGizmo?.helper;
        const handlesVisible = handles?.visible ?? false;
        if (handles) handles.visible = false;
        this.renderFrame(performance.now());
        const image = this.renderer.domElement.toDataURL("image/png");
        if (handles) handles.visible = handlesVisible;
        this.requestRender();
        return image;
    }

    setDisplay(grid: boolean, wireframe: boolean): void {
        this.wireframe = wireframe;
        if (this.framing === "map") {
            this.showMapGrid = grid;
            this.updateMapGrid();
        } else if (grid && !this.grid) {
            this.grid = new THREE.GridHelper(20, 40, 0x253141, 0x121a24);
            this.gridFittedTo.makeEmpty();
            this.scene.add(this.grid);
            this.fitGrid();
        }
        if (this.grid) this.grid.visible = grid;
        this.applyWireframe();
        this.requestRender();
    }

    private updateMapGrid(): void {
        const terrain = this.terrain.currentModel;
        // A hidden active plane has no grid.
        const plane = this.planeFocus.hidden.has(this.planeFocus.plane) ? -1 : this.planeFocus.plane;
        if (terrain !== this.mapGridTerrain || plane !== this.mapGridPlane) this.releaseMapGrid();
        if (this.showMapGrid && terrain && !this.mapGrid) {
            this.mapGrid = new THREE.LineSegments(mapGridGeometry(terrain, plane), new THREE.LineBasicMaterial({ color: 0x253141 }));
            this.mapGridTerrain = terrain;
            this.mapGridPlane = plane;
            this.scene.add(this.mapGrid);
        }
        if (this.mapGrid) this.mapGrid.visible = this.showMapGrid;
    }

    private releaseMapGrid(): void {
        if (!this.mapGrid) return;
        this.scene.remove(this.mapGrid);
        this.mapGrid.geometry.dispose();
        this.mapGrid.material.dispose();
        this.mapGrid = undefined;
        this.mapGridTerrain = this.mapGridPlane = undefined;
    }

    /**
     * Clears to `skyRgb` and fades geometry towards it over an eye-depth range. Null restores the transparent canvas.
     * `environmentFaces` (+X, -X, +Y, -Y, +Z, -Z in client space) replace the procedural sky in reflective effects.
     */
    setEnvironment(skyRgb: number | null, fog: { start: number; end: number } | null, environmentFaces: readonly ModelTexture[] | null = null, sun?: RegionSun | null): void {
        this.scene.background = skyRgb === null ? null : new THREE.Color(skyRgb);
        this.scene.fog = skyRgb === null || !fog ? null : new THREE.Fog(skyRgb, fog.start, fog.end);
        const { lightRgb: rgb, directionX, directionY, directionZ, intensity = 1.1523438, fogValue } = sun ?? defaultRegionSun;
        this.sunDirection.value.set(-directionX, -directionY, -directionZ).normalize();
        this.sunColor.value.set((rgb >> 16 & 255) / 255, (rgb >> 8 & 255) / 255, (rgb & 255) / 255);
        this.waterAmbient.value.copy(this.sunColor.value).multiplyScalar(intensity);
        this.waterFogWidth.value = fogValue === undefined ? (fog ? fog.end - fog.start : 1) : (fogValue + 256) << 2;
        this.setEnvironmentMap(environmentFaces);
        this.requestRender();
    }

    private setEnvironmentMap(faces: readonly ModelTexture[] | null): void {
        const current = this.environmentFaces;
        if (faces?.length === current?.length && (faces ?? []).every((face, index) => face === current?.[index])) return;
        this.environmentFaces = faces ?? undefined;
        this.releaseEnvironmentMap();
        this.refreshHdWater();
        if (!faces) return;
        const size = Math.max(...faces.map(face => Math.max(face.image.width, face.image.height)));
        // Cache bitmaps are flipped for 2D sampling; cube faces take source row order. The client upscales smaller faces by repetition.
        Promise.all(faces.map(face => createImageBitmap(face.image, { imageOrientation: "flipY", resizeWidth: size, resizeHeight: size, resizeQuality: "pixelated" })))
            .then(images => {
                if (this.disposed || this.environmentFaces !== faces) {
                    for (const image of images) image.close();
                    return;
                }
                const cube = new THREE.CubeTexture(images);
                cube.colorSpace = THREE.SRGBColorSpace;
                cube.needsUpdate = true;
                this.environmentMap.value = cube;
                this.environmentEnabled.value = true;
                if (this.framing === "map" && [...this.textureImages?.values() ?? []].some(asset => isHdWater(asset.effectId))) this.waterTexture("normal");
                this.refreshHdWater();
                this.requestRender();
            }, () => undefined);
    }

    private releaseEnvironmentMap(): void {
        const cube = this.environmentMap.value;
        if (!cube) return;
        this.environmentMap.value = null;
        this.environmentEnabled.value = false;
        cube.dispose();
        for (const image of cube.images as ImageBitmap[]) image.close();
    }

    private refreshHdWater(): void {
        const active = this.environmentEnabled.value && !!this.waterTextures.get("normal")?.value;
        if (this.disposed || this.hdWaterActive === active) return;
        this.hdWaterActive = active;
        for (const surface of this.surfaces) if (surface.mesh?.visible) this.rebuildHdWaterMaterials(surface);
    }

    private rebuildHdWaterMaterials(surface: MeshSurface): void {
        const model = surface.currentModel;
        if (!model?.textureIds?.some(id => isHdWater(this.textureImages?.get(id)?.effectId))) return;
        surface.texturesDirty = true;
        this.setGeometry(model, false, surface);
    }

    private applyWireframe(): void {
        for (const { mesh } of this.surfaces) if (mesh) for (const material of mesh.material) material.wireframe = this.wireframe;
    }

    private fitGrid(): void {
        if (!this.grid) return;
        const box = this.sceneBox();
        // Strokes rarely change the scene's extent, and measuring every vertex is not free.
        if (box.isEmpty() || box.equals(this.gridFittedTo)) return;
        this.gridFittedTo.copy(box);
        const bounds = this.sceneSphere();
        if (!bounds) return;
        this.grid.scale.setScalar(Math.max(bounds.radius, 1) / 2);
        this.grid.position.set(bounds.center.x, box.min.y, bounds.center.z);
    }

    setTextures(images: ReadonlyMap<number, ModelTexture>): void {
        if (images === this.textureImages) return;
        const previous = this.textureImages;
        this.textureImages = images;
        if (images.size === this.textures.size && [...images].every(([id, asset]) => previous?.get(id) === asset)) return;
        this.syncTextures(images, previous);
        const liveTextures = new Set(this.textures.values());
        for (const surface of this.surfaces) {
            surface.texturesDirty = true;
            // Callers publish the matching geometry next; do not draw retired image bindings meanwhile.
            if (surface.mesh) surface.mesh.visible = false;
            for (const entry of surface.materials.values()) {
                if (entry.texture && !liveTextures.has(entry.texture)) this.effectUpdates.delete(entry.material);
            }
        }
    }

    private syncTextures(images: ReadonlyMap<number, ModelTexture>, previous: ReadonlyMap<number, ModelTexture> | undefined): void {
        for (const [id, texture] of this.textures) {
            if (images.get(id) === previous?.get(id)) continue;
            texture.dispose();
            this.textures.delete(id);
        }
        for (const [id, asset] of images) {
            if (this.textures.has(id)) continue;
            this.textures.set(id, createModelTexture(asset));
        }
    }

    constructor(private readonly host: HTMLElement, label = "Interactive 3D model", private readonly framing: "model" | "map" = "model") {
        this.renderer = new THREE.WebGLRenderer({ antialias: true, alpha: true });
        this.renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
        this.renderer.domElement.setAttribute("aria-label", label);
        host.appendChild(this.renderer.domElement);
        this.controls = new OrbitControls(this.camera, this.renderer.domElement);
        if (this.framing === "map") {
            this.controls.zoomToCursor = true;
            this.controls.addEventListener("change", this.requestRender);
            this.mapNavigation = new MapPointerNavigation(this.renderer.domElement, this.camera, this.controls, () => {
                const ground = this.terrain.mesh?.geometry;
                if (!ground) return undefined;
                if (!ground.boundingBox) ground.computeBoundingBox();
                const box = ground.boundingBox!;
                return box.isEmpty() ? undefined : (box.min.y + box.max.y) / 2;
            });
        }
        this.scene.add(new THREE.HemisphereLight(0xffffff, 0x667788, 2));
        const light = new THREE.DirectionalLight(0xffffff, 3);
        light.position.set(1, 2, 3);
        this.scene.add(light);
        this.resizeObserver = new ResizeObserver(() => this.resize());
        this.resizeObserver.observe(host);
        this.resize();
        if (this.framing === "model") this.renderer.setAnimationLoop(this.renderFrame);
    }

    private updateParticles(time: number): void {
        if (this.particles && this.primary.currentModel) {
            const elapsed = this.lastTime === undefined ? 0 : Math.min(100, Math.max(0, time - this.lastTime));
            const ticks = this.particleTickRemainder + elapsed * this.particlePlaybackSpeed / animationTickMs;
            const wholeTicks = Math.floor(ticks);
            this.particleTickRemainder = ticks - wholeTicks;
            // Particle definitions use client ticks normalized to seconds in the preview DTO.
            this.particles.update(wholeTicks / 1000, this.primary.currentModel,
                this.host.clientHeight * Math.min(window.devicePixelRatio,2) * this.camera.projectionMatrix.elements[5] / 2);
        }
    }

    private readonly renderFrame = (time: number): void => {
        this.epoch ??= time;
        const seconds = (time - this.epoch) / 1000;
        this.updateMaterialEffects(seconds);
        this.updateParticles(time);
        this.lastTime = time;
        if (this.framing === "map") this.renderObjectAnimations(time);
        this.controls.update();
        this.updateBillboards();
        this.placementGizmo?.fitToCamera();
        this.updateDrawRanges();
        this.renderer.render(this.scene, this.camera);
        for (const surface of this.surfaces) surface.wholeIndexPending = false;
        this.renderOrientationGizmo();
    };

    private updateMaterialEffects(seconds: number): void {
        for (const [id, texture] of this.textures) {
            const phase = sampleMaterialEffect(this.textureImages?.get(id) ?? {}, seconds);
            texture.offset.set(phase.u, phase.v);
        }
        for (const update of this.effectUpdates.values()) update(seconds);
    }

    /** Culls chunks, re-sorts translucent faces when due, and rewrites draw ranges whose visible chunks changed. */
    private updateDrawRanges(): void {
        this.cullSurfaces();
        this.orderSurfaces();
        for (const surface of this.surfaces) if (surface.groupsDirty && surface.mesh) this.writeDrawGroups(surface.mesh.geometry, surface);
    }

    private renderOrientationGizmo(): void {
        if (!this.orientationGizmo) return;
        const autoClear = this.renderer.autoClear;
        this.renderer.autoClear = false;
        try {
            this.orientationGizmo.render(this.renderer);
        } finally {
            this.renderer.autoClear = autoClear;
        }
    }

    setObjectAnimationsEnabled(enabled: boolean): void {
        this.objectAnimationsEnabled = enabled;
        this.requestRender();
    }

    private renderObjectAnimations(time: number): void {
        const surface = this.primary;
        const model = surface.currentModel;
        const mesh = surface.mesh;
        const animations = model?.animations;
        if (animations && this.waitingForObjectAnimation(time)) return;
        clearTimeout(this.animationTimer);
        this.animationTimer = undefined;
        if (!animations || !model.vertexRgba || !mesh) return this.resetObjectAnimationTime();
        if (!this.objectAnimationsEnabled) {
            this.restoreObjectAnimationPose(mesh, animations, model.positions, model.vertexRgba);
            return;
        }
        this.objectAnimationEpoch ??= time;
        const elapsed = Math.max(time - this.objectAnimationEpoch, 0);
        const next = this.updateObjectAnimationRanges(mesh, animations, model.positions, model.vertexRgba, elapsed);
        this.lastObjectAnimationTime = elapsed;
        this.nextObjectAnimationTime = time + next;
        this.scheduleObjectAnimation(next);
    }

    private scheduleObjectAnimation(next: number): void {
        if (next < Infinity) this.animationTimer = setTimeout(() => {
            // A frame timestamp can precede a timer callback in the same browser frame.
            this.nextObjectAnimationTime = 0;
            this.requestRender();
        }, next);
    }

    private waitingForObjectAnimation(time: number): boolean {
        if (!this.objectAnimationsEnabled || this.lastObjectAnimationTime === undefined || time >= this.nextObjectAnimationTime) return false;
        // Ranges skipped as out of view or too small resume as soon as the camera moves.
        this.camera.updateMatrixWorld();
        return !this.animationViewSkipped || this.animationCamera.equals(this.camera.matrixWorld);
    }

    private resetObjectAnimationTime(): void {
        this.objectAnimationEpoch = undefined;
        this.lastObjectAnimationTime = undefined;
    }

    private restoreObjectAnimationPose(mesh: NonNullable<MeshSurface["mesh"]>, animations: readonly ModelAnimationRange[], positions: readonly number[] | Float32Array, colors: Float32Array): void {
        if (this.lastObjectAnimationTime !== undefined) this.updateObjectAnimationRanges(mesh, animations, positions, colors, null);
        this.resetObjectAnimationTime();
    }

    private updateObjectAnimationRanges(
        mesh: NonNullable<MeshSurface["mesh"]>, animations: readonly ModelAnimationRange[],
        basePositions: readonly number[] | Float32Array, baseColors: Float32Array, elapsed: number | null,
    ): number {
        const { geometry } = mesh;
        // The primary arrays keep the pose for picking, bounds, handles and billboards; only a drawn copy is uploaded.
        const positions = geometry.getAttribute("position") as THREE.BufferAttribute;
        const colors = geometry.getAttribute("color") as THREE.BufferAttribute;
        const drawn = this.animated.mesh?.geometry ?? geometry;
        const target = this.drawnRangeStart(drawn === geometry);
        const visible = elapsed === null ? undefined : this.animationVisibility(basePositions);
        this.animationViewSkipped = false;
        let next = Infinity;
        let updated = false;
        for (const animation of animations) {
            const result = elapsed === null
                ? this.restoreAnimationRange(animation, positions, colors, basePositions, baseColors)
                : this.poseAnimationRange(animation, positions, colors, elapsed, visible);
            next = Math.min(next, result.delay);
            if (!result.posed) continue;
            writeDrawnPose(drawn, positions, colors, animation.startVertex, target(animation), animation.vertexCount);
            updated = true;
        }
        if (updated) markPoseUploaded(geometry, drawn);
        return next;
    }

    /** Where a range's vertices sit in the drawn geometry: the same place, or their copy in the animated surface. */
    private drawnRangeStart(primaryDrawn: boolean): (range: ModelAnimationRange) => number {
        if (primaryDrawn) return range => range.startVertex;
        const offsets = new Map(this.animatedRanges.map(({ range, start }) => [range, start]));
        return range => offsets.get(range) ?? range.startVertex;
    }

    /** Writes a range's current pose into the primary arrays, unless it is hidden, off screen or too small to be worth posing. */
    private poseAnimationRange(animation: ModelAnimationRange, positions: THREE.BufferAttribute, colors: THREE.BufferAttribute, elapsed: number,
        visible: ((range: ModelAnimationRange) => boolean) | undefined): { delay: number; posed: boolean } {
        // Posing is the costliest part of an animation tick.
        if (animation.plane !== undefined && this.planeFocus.hidden.has(animation.plane)) return { delay: Infinity, posed: false };
        if (visible && !visible(animation)) {
            this.animationViewSkipped = true;
            return { delay: Infinity, posed: false };
        }
        const { delay, pose } = animation.sample(elapsed, this.lastObjectAnimationTime);
        if (!pose) return { delay, posed: false };
        positions.array.set(pose.positions, animation.startVertex * 3);
        colors.array.set(pose.rgba, animation.startVertex * 4);
        this.writeBillboards(animation, pose.billboards);
        return { delay, posed: true };
    }

    private restoreAnimationRange(animation: ModelAnimationRange, positions: THREE.BufferAttribute, colors: THREE.BufferAttribute,
        basePositions: readonly number[] | Float32Array, baseColors: Float32Array): { delay: number; posed: boolean } {
        const start = animation.startVertex, end = start + animation.vertexCount;
        positions.array.set(basePositions.slice(start * 3, end * 3), start * 3);
        colors.array.set(baseColors.subarray(start * 4, end * 4), start * 4);
        this.writeBillboards(animation);
        return { delay: Infinity, posed: true };
    }

    /** Whether an animation range is in view and large enough on screen to be worth posing. */
    private animationVisibility(basePositions: ArrayLike<number>): (range: ModelAnimationRange) => boolean {
        this.camera.updateMatrixWorld();
        this.animationCamera.copy(this.camera.matrixWorld);
        this.frustum.setFromProjectionMatrix(this.viewProjection.multiplyMatrices(this.camera.projectionMatrix, this.camera.matrixWorldInverse));
        const pixelsPerUnit = this.host.clientHeight / (2 * Math.tan(THREE.MathUtils.degToRad(this.camera.fov / 2)));
        return range => {
            let bounds = this.animationBounds.get(range);
            if (!bounds) {
                bounds = vertexRangeSphere(basePositions, range.startVertex, range.vertexCount);
                this.animationBounds.set(range, bounds);
            }
            if (!this.frustum.intersectsSphere(bounds)) return false;
            const distance = Math.max(bounds.center.distanceTo(this.camera.position), 1);
            return bounds.radius * 2 * pixelsPerUnit / distance >= minAnimatedPixels;
        };
    }

    setGeometry(model: ModelGeometry, fitCamera = true, surface = this.primary): void {
        if (this.disposed) throw new Error("The model scene has been disposed.");
        const previousModel = surface.currentModel;
        if (this.isCurrentSurface(surface, model)) return;
        if (this.applyPositionPatch(surface, model)) return;
        surface.currentModel = model;
        surface.orderingDirty = true;
        this.requestRender();
        const existing = surface.mesh;
        const firstTexel = model.firstTexel?.some(value => value !== 0) ?? false;
        if (this.updateInPlace(surface, model, previousModel, firstTexel)) return;
        const retainedGeometry = previousModel === model ? existing?.geometry : undefined;
        const { geometry, materials } = this.rebuildSurface(model, surface, firstTexel, retainedGeometry);
        this.attachSurface(geometry, materials, model, surface, firstTexel);
        if (fitCamera) this.resetCamera();
    }

    private isCurrentSurface(surface: MeshSurface, model: ModelGeometry): boolean {
        return surface.currentModel === model && !!surface.mesh && !surface.texturesDirty;
    }

    private rebuildSurface(model: ModelGeometry, surface: MeshSurface, firstTexel: boolean, retainedGeometry?: THREE.BufferGeometry): { geometry: THREE.BufferGeometry; materials: SurfaceMaterial[] } {
        this.clear(true, !!retainedGeometry, surface);
        if (surface === this.primary) {
            this.epoch = undefined;
            this.lastTime = undefined;
            this.particleTickRemainder = 0;
            this.lastObjectAnimationTime = undefined;
        }
        const geometry = retainedGeometry ?? new THREE.BufferGeometry();
        if (!retainedGeometry) fillGeometry(geometry, model, firstTexel);
        const materials = this.prepareSurfaceMaterials(geometry, model, surface, firstTexel, !!retainedGeometry);
        return { geometry, materials };
    }

    private attachSurface(geometry: THREE.BufferGeometry, materials: SurfaceMaterial[], model: ModelGeometry, surface: MeshSurface, firstTexel: boolean): void {
        this.assignSurfaceGroups(geometry, model, surface);
        surface.mesh = new THREE.Mesh(geometry, materials);
        surface.mesh.frustumCulled = !hasObjectAnimations(model);
        // Translucent ground draws first, then translucent objects sort among themselves.
        if (surface === this.terrain) surface.mesh.renderOrder = -1;
        surface.mesh.userData["preparedLighting"] = !!model.preparedLighting;
        surface.mesh.userData["firstTexel"] = firstTexel;
        this.applyWireframe();
        surface.mesh.name = model.sourceId === undefined ? "" : String(model.sourceId);
        this.scene.add(surface.mesh);
        surface.texturesDirty = false;
        this.fitGrid();
        if (model.particles?.length) this.particles = new ModelParticles(this.scene, model.particles, this.textures);
        this.setBillboards(model, surface);
    }

    private prepareSurfaceMaterials(geometry: THREE.BufferGeometry, model: ModelGeometry, surface: MeshSurface, firstTexel: boolean, retainGeometry: boolean): SurfaceMaterial[] {
        const usedMaterials = new Set<string>();
        const useMaterial = this.surfaceMaterialCache(surface, model, usedMaterials);
        const colorSize = surfaceColorSize(model);
        const options = { vertexColors: colorSize !== 0, ...(model.preparedLighting ? {} : {roughness: 0.8}) };
        const surfaceKey = `${!!model.preparedLighting}:${colorSize}:${!!model.mapPlanes}`;
        const OrdinaryMaterial = model.preparedLighting ? THREE.MeshBasicMaterial : THREE.MeshStandardMaterial;
        const materials: (THREE.MeshStandardMaterial | THREE.MeshBasicMaterial)[] = [
            useMaterial(`plain:${surfaceKey}:false`, () => new OrdinaryMaterial({ ...options, transparent: false, depthWrite: true })),
            useMaterial(`plain:${surfaceKey}:true`, () => new OrdinaryMaterial({ ...options, transparent: true, depthWrite: false })),
        ];
        for (const [id, texture] of this.textures) {
            const asset = this.textureImages?.get(id);
            const hd = this.hdWaterActive && isHdWater(asset?.effectId);
            const create = (transparent: boolean) => useMaterial(
                `texture:${id}:${surfaceKey}:${firstTexel}:${transparent}:${hd}:${!!model.waterDepth}`,
                () => this.createTexturedMaterial(texture, asset, model, options.vertexColors, transparent, firstTexel),
                texture, asset,
            );
            materials.push(create(asset?.alphaMode === 2));
            materials.push(create(true));
        }
        applyTerrainMaterials(geometry, materials, retainGeometry ? undefined : model.terrainWeights, model.terrainBlends, this.textures, blend => {
            const key = `terrain:${!!model.mapPlanes}:${blend.layers.map(layer => `${layer.textureId}:${layer.textureScale}:${!!layer.reflective}`).join("|")}`;
            const material = useMaterial(key, () => new TerrainMaterial(blend, this.textures, { map: this.environmentMap, enabled: this.environmentEnabled }));
            material.updateTextures(this.textures);
            return material;
        });
        this.pruneSurfaceMaterials(surface, usedMaterials);
        return materials;
    }

    private surfaceMaterialCache(surface: MeshSurface, model: ModelGeometry, usedMaterials: Set<string>) {
        return <T extends SurfaceMaterial>(key: string, create: () => T, texture?: THREE.Texture, asset?: ModelTexture): T => {
            usedMaterials.add(key);
            const existing = surface.materials.get(key);
            if (existing && existing.texture === texture && existing.asset === asset) return existing.material as T;
            if (existing) {
                existing.material.dispose();
                this.effectUpdates.delete(existing.material);
            }
            const material = create();
            if (model.mapPlanes) this.mapLighting.apply(material);
            surface.materials.set(key, { material, texture, asset });
            return material;
        };
    }

    private pruneSurfaceMaterials(surface: MeshSurface, usedMaterials: ReadonlySet<string>): void {
        for (const [key, entry] of surface.materials) {
            if (usedMaterials.has(key)) continue;
            entry.material.dispose();
            this.effectUpdates.delete(entry.material);
            surface.materials.delete(key);
        }
    }

    private setBillboards(model: ModelGeometry, surface: MeshSurface): void {
        if (surface !== this.primary) return;
        if (!model.billboards?.length) {
            this.billboards?.dispose();
            this.billboards = undefined;
            return;
        }
        this.billboards ??= new ModelBillboards(this.scene, this.textures);
        this.billboards.reset(model.billboards);
    }

    private updateBillboards(): void {
        if (this.billboards && this.primary.mesh) this.billboards.update(this.primary.mesh.geometry.getAttribute("position") as THREE.BufferAttribute, this.camera);
    }

    private writeBillboards({ startBillboard, billboardCount }: ModelAnimationRange, pose?: readonly RenderBillboard[]): void {
        if (!this.billboards || startBillboard === undefined) return;
        const base = this.primary.currentModel?.billboards ?? [];
        this.billboards.write(startBillboard, pose ?? base.slice(startBillboard, startBillboard + (billboardCount ?? 0)));
    }

    private createTexturedMaterial(texture: THREE.Texture, asset: ModelTexture | undefined, model: ModelGeometry, vertexColors: boolean, transparent: boolean, firstTexel: boolean): SurfaceMaterial {
        const hdWater = this.createHdWaterMaterial(texture, asset, !!model.waterDepth);
        if (hdWater) return hdWater;
        const appearance = { vertexColors, map: texture, alphaTest: asset?.alphaMode === 1 ? 0.5 : 0, transparent, depthWrite: !transparent };
        const material = asset && isFixedWater(asset.effectId)
            ? this.createFixedWaterMaterial(asset, asset.effectId, appearance, hasBakedRegionTint(model))
            : this.createBaseMaterial(asset, appearance, !!model.preparedLighting);
        if (firstTexel && asset) applyFirstTexelSampling(material, asset);
        return material;
    }

    private createHdWaterMaterial(texture: THREE.Texture, asset: ModelTexture | undefined, hasDepth: boolean): THREE.MeshBasicMaterial | null {
        const effect = asset?.effectId;
        if (!asset || !isHdWater(effect) || this.framing !== "map" || !this.environmentEnabled.value) return null;
        const noise = this.waterTexture("normal");
        if (!this.hdWaterActive) return null;
        const material = new THREE.MeshBasicMaterial({ map: texture, transparent: true, depthWrite: true });
        this.applyHdWater(material, asset, effect, noise, hasDepth);
        return material;
    }

    private createBaseMaterial(asset: ModelTexture | undefined, appearance: THREE.MeshBasicMaterialParameters, preparedLighting: boolean): SurfaceMaterial {
        const effect = asset?.effectId;
        if (effect === 6 || (preparedLighting && effect !== 1 && effect !== 7)) return new THREE.MeshBasicMaterial(appearance);
        const material = new THREE.MeshStandardMaterial({ ...appearance, ...(preparedLighting ? {} : { roughness: 0.8 }) });
        if (asset) this.applyEffect(material, asset, preparedLighting);
        return material;
    }

    private createFixedWaterMaterial(asset: ModelTexture, effect: 2 | 4 | 5, appearance: THREE.MeshBasicMaterialParameters, bakedTint: boolean): THREE.MeshBasicMaterial {
        const material = new THREE.MeshBasicMaterial({ ...appearance, alphaTest: 0, transparent: true, depthWrite: this.framing === "map" || appearance.depthWrite });
        this.applyFixedWater(material, asset, effect, bakedTint);
        return material;
    }

    /** Models with a source ID and an unchanged layout only need their buffers rewritten. */
    private updateInPlace(surface: MeshSurface, model: ModelGeometry, previousModel: ModelGeometry | undefined, firstTexel: boolean): boolean {
        const existing = surface.mesh;
        if (!existing || !this.canUpdateInPlace(surface, existing, model, previousModel, firstTexel)) return false;
        const positions = existing.geometry.getAttribute("position");
        if (positions.count * 3 !== model.positions.length) return false;
        writePositions(positions, model);
        if (firstTexel) writeFirstTexel(existing.geometry.getAttribute("cacheFirstTexel"), model);
        const colors = existing.geometry.getAttribute("color");
        if (colors) writeColors(colors, model);
        this.assignSurfaceGroups(existing.geometry, model, surface);
        // Normal computation needs a complete index before the next frame.
        this.orderBlendedSurfaces(existing.geometry, model, surface);
        existing.geometry.computeVertexNormals();
        existing.geometry.computeBoundingSphere();
        if (existing.geometry.boundingBox) existing.geometry.computeBoundingBox();
        this.setBillboards(model, surface);
        return true;
    }

    private canUpdateInPlace(surface: MeshSurface, existing: NonNullable<MeshSurface["mesh"]>, model: ModelGeometry, previousModel: ModelGeometry | undefined, firstTexel: boolean): boolean {
        if (surface.texturesDirty || model.vertexRgba || previousModel?.vertexRgba || model.sourceId === undefined) return false;
        return usesSameMaterials(existing, model, firstTexel) && hasSameLayout(surface, existing, model);
    }

    /** Terrain and objects are separate surfaces, so changing one leaves the other's buffers alone. */
    setSceneGeometry({ terrain, objects }: { terrain: ModelGeometry | null; objects: ModelGeometry | null }, fitCamera: boolean): void {
        this.setSurface(this.terrain, terrain);
        this.setSurface(this.primary, objects);
        this.setSurface(this.animated, this.framing === "map" && objects ? this.animatedPart(objects) : null);
        const drawn = this.animated.mesh?.geometry;
        if (drawn) {
            drawn.computeBoundingSphere();
            drawn.boundingSphere!.radius += cullChunkMargin;
        }
        this.updateMapGrid();
        if (fitCamera) this.resetCamera();
    }

    setMapPointLights(field: MapLightField | null, enabled: boolean): void {
        this.mapLighting.set(field, enabled);
        this.requestRender();
    }

    /** The primary model's animated vertices as their own geometry, or null when nothing animates. */
    private animatedPart(model: ModelGeometry): ModelGeometry | null {
        const ranges = [...model.animations ?? []].sort((left, right) => left.startVertex - right.startVertex);
        if (!ranges.length) {
            this.animatedRanges = [];
            return null;
        }
        let start = 0;
        this.animatedRanges = ranges.map(range => {
            const entry = { range, start };
            start += range.vertexCount;
            return entry;
        });
        // A terrain patch keeps the animated vertices, which it never moves, so the copy is shared with the patch base.
        const source = model.patchOf?.base ?? model;
        const cached = this.animatedParts.get(source);
        if (cached) return cached;
        const part = copyVertexRanges(source, ranges);
        this.animatedParts.set(source, part);
        return part;
    }

    private setSurface(surface: MeshSurface, model: ModelGeometry | null): void {
        if (model) this.setGeometry(model, false, surface);
        else if (surface.mesh) {
            this.clear(true, false, surface);
            surface.currentModel = undefined;
            this.fitGrid();
            this.requestRender();
        }
    }

    /** Rewrites only the vertices a patch names, for changes that keep the surface's layout. */
    private applyPositionPatch(surface: MeshSurface, model: ModelGeometry): boolean {
        const patch = model.patchOf;
        if (!patch || !surface.mesh || surface.currentModel !== patch.base || surface.texturesDirty) return false;
        const positions = surface.mesh.geometry.getAttribute("position") as THREE.BufferAttribute;
        positions.clearUpdateRanges();
        for (const [start, end] of patch.ranges) {
            for (let vertex = start; vertex < end; vertex++)
                positions.setXYZ(vertex, model.positions[vertex * 3], model.positions[vertex * 3 + 1], model.positions[vertex * 3 + 2]);
            positions.addUpdateRange(start * 3, (end - start) * 3);
        }
        positions.needsUpdate = true;
        surface.mesh.geometry.computeBoundingSphere();
        surface.currentModel = model;
        surface.orderingDirty = true;
        this.requestRender();
        return true;
    }

    private get surfaces(): MeshSurface[] {
        return [this.terrain, this.primary, this.animated];
    }

    /** The sphere one buffer holding every surface would have; merging spheres would overestimate it. */
    private sceneSphere(): THREE.Sphere | undefined {
        const box = this.sceneBox();
        if (box.isEmpty()) return undefined;
        const center = box.getCenter(new THREE.Vector3());
        let furthest = 0;
        for (const { mesh } of this.surfaces) {
            if (!mesh) continue;
            const position = mesh.geometry.getAttribute("position");
            for (let vertex = 0; vertex < position.count; vertex++) {
                const dx = position.getX(vertex) - center.x, dy = position.getY(vertex) - center.y, dz = position.getZ(vertex) - center.z;
                furthest = Math.max(furthest, dx * dx + dy * dy + dz * dz);
            }
        }
        return new THREE.Sphere(center, Math.sqrt(furthest));
    }

    private sceneBox(): THREE.Box3 {
        const box = new THREE.Box3();
        for (const { mesh } of this.surfaces) {
            if (!mesh) continue;
            if (!mesh.geometry.boundingBox) mesh.geometry.computeBoundingBox();
            box.union(mesh.geometry.boundingBox!);
        }
        return box;
    }

    resetCamera(): void {
        const bounds = this.sceneSphere();
        if (!bounds) return;
        const radius = Math.max(bounds.radius, 1);
        this.controls.target.copy(bounds.center);
        const halfVerticalFov = THREE.MathUtils.degToRad(this.camera.fov / 2);
        const halfHorizontalFov = Math.atan(Math.tan(halfVerticalFov) * this.camera.aspect);
        const direction = (this.framing === "map" ? new THREE.Vector3(1, 0.4, 0.12) : new THREE.Vector3(1.5, 1, 3)).normalize();
        const distance = this.framing === "map"
            ? this.fitMapDistance(direction, Math.tan(halfHorizontalFov), Math.tan(halfVerticalFov))
            : radius / Math.sin(Math.min(halfVerticalFov, halfHorizontalFov)) * 1.1;
        this.camera.position.copy(this.controls.target).addScaledVector(direction, distance);
        this.camera.near = Math.max(radius / 1000, 0.01);
        this.camera.far = Math.max(radius * 100, distance * 2);
        this.camera.updateProjectionMatrix();
        this.controls.update();
        this.requestRender();
    }

    private fitMapDistance(direction: THREE.Vector3, horizontalSlope: number, verticalSlope: number): number {
        const box = this.sceneBox();
        if (box.isEmpty()) return 1;
        const { min, max } = box;
        box.getCenter(this.controls.target);
        const right = new THREE.Vector3().crossVectors(this.camera.up, direction).normalize();
        const up = new THREE.Vector3().crossVectors(direction, right);
        const corner = new THREE.Vector3();
        let distance = 1;
        for (const x of [min.x, max.x]) {
            for (const y of [min.y, max.y]) {
                for (const z of [min.z, max.z]) {
                    corner.set(x, y, z).sub(this.controls.target);
                    const clearance = Math.max(Math.abs(corner.dot(right)) / horizontalSlope, Math.abs(corner.dot(up)) / verticalSlope);
                    distance = Math.max(distance, corner.dot(direction) + clearance * 1.08);
                }
            }
        }
        return distance;
    }

    orbit(horizontal: number, vertical: number): void {
        const offset = this.camera.position.clone().sub(this.controls.target);
        const spherical = new THREE.Spherical().setFromVector3(offset);
        spherical.theta += horizontal;
        spherical.phi = THREE.MathUtils.clamp(spherical.phi + vertical, 0.05, Math.PI - 0.05);
        this.camera.position.copy(this.controls.target).add(new THREE.Vector3().setFromSpherical(spherical));
        this.controls.update();
        this.requestRender();
    }

    pan(horizontal: number, vertical: number): void {
        const distance = this.camera.position.distanceTo(this.controls.target) * 0.05;
        const offset = new THREE.Vector3(horizontal * distance, vertical * distance, 0).applyQuaternion(this.camera.quaternion);
        this.camera.position.add(offset);
        this.controls.target.add(offset);
        this.controls.update();
        this.requestRender();
    }

    zoom(factor: number): void {
        const offset = this.camera.position.clone().sub(this.controls.target).multiplyScalar(factor);
        this.camera.position.copy(this.controls.target).add(offset);
        this.controls.update();
        this.requestRender();
    }

    dispose(): void {
        if (this.disposed) return;
        this.disposed = true;
        if (this.framing === "model") this.renderer.setAnimationLoop(null);
        else this.controls.removeEventListener("change", this.requestRender);
        if (this.pendingFrame !== undefined) cancelAnimationFrame(this.pendingFrame);
        clearTimeout(this.animationTimer);
        clearTimeout(this.reorderTimer);
        clearTimeout(this.waterTimer);
        this.resizeObserver.disconnect();
        this.mapNavigation?.dispose();
        this.controls.dispose();
        this.orientationGizmo?.dispose();
        this.placementGizmo?.dispose();
        this.releaseOverlays();
        this.releaseMapGrid();
        this.clear(false, false, this.terrain);
        this.clear(false, false, this.animated);
        this.clear();
        if (this.grid) {
            this.scene.remove(this.grid);
            this.grid.geometry.dispose();
            const materials = Array.isArray(this.grid.material) ? this.grid.material : [this.grid.material];
            for (const material of materials) material.dispose();
        }
        for (const texture of this.textures.values()) texture.dispose();
        this.textures.clear();
        for (const texture of this.waterTextures.values()) texture.value?.dispose();
        this.waterTextures.clear();
        this.turbulenceTexture?.dispose();
        this.environmentFaces = undefined;
        this.releaseEnvironmentMap();
        this.mapLighting.dispose();
        this.renderer.dispose();
        this.renderer.domElement.remove();
    }

    private releaseOverlays(): void {
        if (this.selectionBounds) {
            this.scene.remove(this.selectionBounds);
            this.selectionBounds.dispose();
        }
        if (this.strokePreview) this.releaseTileMesh(this.strokePreview);
        if (this.tileOverlay) this.releaseTileMesh(this.tileOverlay);
    }

    private clear(preserveMaterials = false, preserveGeometry = false, surface = this.primary): void {
        if (surface === this.primary) {
            this.particles?.dispose();
            this.particles = undefined;
            this.billboards?.dispose();
            this.billboards = undefined;
        }
        if (!preserveMaterials) {
            for (const entry of surface.materials.values()) entry.material.dispose();
            surface.materials.clear();
            this.effectUpdates.clear();
        }
        if (!surface.mesh) return;
        this.scene.remove(surface.mesh);
        if (!preserveGeometry) surface.mesh.geometry.dispose();
        surface.mesh = undefined;
    }

    private applyEffect(material: THREE.MeshStandardMaterial, asset: MaterialEffect, preparedLighting = false): void {
        const id = asset.effectId ?? 0;
        if (id !== 1 && id !== 7) return;
        const phase = {value: new THREE.Vector4()};
        const update = (seconds: number) => {
            const sample = sampleMaterialEffect(asset, seconds);
            phase.value.set(
                sample.animateWater ? sample.secondary : 0,
                sample.noise,
                sample.distortion,
                asset.effectParameter ?? 0);
        };
        update(0);
        this.effectUpdates.set(material, update);
        material.customProgramCacheKey = () => `cache-effect-${id}-${preparedLighting}`;
        const postLightingEffect = id === 1 ? clientSpecularHighlight : id === 7 ? clientReflection("sampledDiffuseColor.a") : null;
        material.onBeforeCompile = shader => {
            shader.uniforms.cachePhase = phase;
            shader.uniforms.cacheEnvironment = this.environmentMap;
            shader.uniforms.cacheEnvironmentEnabled = this.environmentEnabled;
            shader.vertexShader = `varying vec3 cachePosition;\n${shader.vertexShader}`.replace("#include <begin_vertex>", "#include <begin_vertex>\ncachePosition = vec3(position.x, -position.y, position.z);");
            shader.fragmentShader = `uniform vec4 cachePhase;\nuniform samplerCube cacheEnvironment;\nuniform bool cacheEnvironmentEnabled;\nvarying vec3 cachePosition;\n${shader.fragmentShader}`;
            if (preparedLighting) shader.fragmentShader = shader.fragmentShader.replace("#include <opaque_fragment>", "outgoingLight = diffuseColor.rgb;\n#include <opaque_fragment>");
            if (postLightingEffect) shader.fragmentShader = shader.fragmentShader.replace("#include <opaque_fragment>", `${postLightingEffect}\n#include <opaque_fragment>`);
        };
    }

    private applyFixedWater(material: THREE.MeshBasicMaterial, asset: MaterialEffect, id: 2 | 4 | 5, bakedTint: boolean): void {
        const noise = this.waterTexture(id === 5 ? "lake" : "ridged");
        if (id === 2 && !this.turbulenceTexture) {
            this.turbulenceTexture = new THREE.DataTexture(waterTurbulence(), 128, waterTurbulenceRows, THREE.RedFormat, THREE.FloatType);
            this.turbulenceTexture.needsUpdate = true;
        }
        const parameters = { value: new THREE.Vector4() };
        const p = asset.effectParameter ?? 0;
        const flags = { value: new THREE.Vector4((p & 128) === 0 ? 1 : 0, (p & 64) === 0 && !bakedTint ? 1 : 0, (p & 128) === 0 ? 0 : 1, 0) };
        const scroll = { value: new THREE.Vector2() };
        const update = (seconds: number) => {
            const sample = sampleMaterialEffect(asset, seconds);
            parameters.value.set(...fixedWaterParameters(id, sample), 0);
            flags.value.w = noise.value ? 1 : 0;
            scroll.value.set(sample.u, sample.v);
        };
        update(0);
        this.effectUpdates.set(material, update);
        const source = clientFixedWater(id);
        material.customProgramCacheKey = () => `cache-fixed-water-${id}`;
        material.onBeforeCompile = shader => {
            shader.uniforms.cacheWaterNoise = noise;
            shader.uniforms.cacheTurbulence = { value: this.turbulenceTexture };
            shader.uniforms.cacheWaterParams = parameters;
            shader.uniforms.cacheWaterFlags = flags;
            shader.uniforms.cacheWaterScroll = scroll;
            shader.uniforms.cacheWaterAmbient = this.waterAmbient;
            shader.uniforms.cacheWaterFogWidth = this.waterFogWidth;
            shader.vertexShader = source.vertexDeclarations + shader.vertexShader.replace("#include <fog_vertex>", `#include <fog_vertex>\n${source.vertex}`);
            shader.fragmentShader = source.fragmentDeclarations + shader.fragmentShader
                .replace("#include <colorspace_fragment>", source.fragment)
                .replace("#include <fog_fragment>", "");
        };
    }

    private applyHdWater(material: THREE.MeshBasicMaterial, asset: MaterialEffect, id: 8 | 9, noise: THREE.IUniform<THREE.Data3DTexture | null>, hasDepth: boolean): void {
        const { hdScale, breakDepth, breakOffset, waveIntensity, waveExponent } = sampleMaterialEffect(asset, 0);
        const parameters = { value: new THREE.Vector4(0, hdScale, breakDepth, breakOffset) };
        const wave = { value: new THREE.Vector3(waveIntensity[0], waveIntensity[1], waveExponent) };
        const scroll = { value: new THREE.Vector2() };
        this.effectUpdates.set(material, seconds => {
            const sample = sampleMaterialEffect(asset, seconds);
            parameters.value.x = sample.hdPhase;
            scroll.value.set(sample.u, sample.v);
        });
        const source = clientHdWater(id, hasDepth);
        material.customProgramCacheKey = () => `cache-hd-water-${id}-${hasDepth}`;
        material.onBeforeCompile = shader => {
            shader.uniforms.cacheWaterNoise = noise;
            shader.uniforms.cacheEnvironment = this.environmentMap;
            shader.uniforms.cacheWaterParams = parameters;
            shader.uniforms.cacheWaterScroll = scroll;
            shader.uniforms.cacheWave = wave;
            shader.uniforms.cacheSunDirection = this.sunDirection;
            shader.uniforms.cacheSunColor = this.sunColor;
            shader.vertexShader = source.vertexDeclarations + shader.vertexShader.replace("#include <fog_vertex>", source.vertex);
            shader.fragmentShader = source.fragmentDeclarations + shader.fragmentShader
                .replace("#include <colorspace_fragment>", source.fragment)
                .replace("#include <fog_fragment>", "");
        };
    }

    private waterTexture(kind: WaterVolume): THREE.IUniform<THREE.Data3DTexture | null> {
        const existing = this.waterTextures.get(kind);
        if (existing) return existing;
        const uniform: THREE.IUniform<THREE.Data3DTexture | null> = { value: null };
        this.waterTextures.set(kind, uniform);
        void waterVolume(kind).then(data => {
            if (this.disposed) return;
            const texture = new THREE.Data3DTexture(data, waterVolumeSize, waterVolumeSize, waterVolumeSlices);
            texture.format = kind === "normal" ? THREE.RGBAFormat : THREE.RGFormat;
            texture.wrapS = texture.wrapT = texture.wrapR = THREE.RepeatWrapping;
            texture.magFilter = THREE.LinearFilter;
            texture.generateMipmaps = kind === "normal";
            texture.minFilter = texture.generateMipmaps ? THREE.LinearMipmapLinearFilter : THREE.LinearFilter;
            texture.needsUpdate = true;
            uniform.value = texture;
            if (kind === "normal") this.refreshHdWater();
            this.requestRender();
        });
        return uniform;
    }

    private resize(): void {
        const width = Math.max(this.host.clientWidth, 1);
        const height = Math.max(this.host.clientHeight, 1);
        this.renderer.setSize(width, height);
        this.camera.aspect = width / height;
        this.camera.updateProjectionMatrix();
        this.requestRender();
    }

    private orderSurfaces(): void {
        this.camera.updateMatrixWorld();
        const view = this.camera.matrixWorldInverse.elements;
        const x = -view[2], y = -view[6], z = -view[10];
        // Translation adds the same depth to every face, so pans and tiny turns keep the current order.
        const axis = this.viewDepthAxis;
        let turned = 1 - (x * axis.x + y * axis.y + z * axis.z) > reorderTurn;
        if (turned && this.framing === "map") turned = this.blendedOrderDue();
        if (turned) axis.set(x, y, z);
        for (const surface of this.surfaces) {
            if (!surface.mesh || !surface.currentModel || (!turned && !surface.orderingDirty)) continue;
            surface.orderingDirty = false;
            this.orderBlendedSurfaces(surface.mesh.geometry, surface.currentModel, surface);
        }
    }

    /**
     * Sorting every translucent face of a large map costs more than a frame, so a turning map camera re-sorts at most once per
     * interval and draws one more frame when the interval ends, leaving the final view correctly ordered.
     */
    private blendedOrderDue(): boolean {
        const now = performance.now();
        const wait = this.lastBlendedOrder + reorderInterval - now;
        if (wait <= 0) {
            this.lastBlendedOrder = now;
            return true;
        }
        this.reorderTimer ??= setTimeout(() => {
            this.reorderTimer = undefined;
            this.requestRender();
        }, wait);
        return false;
    }

    /** Marks the map chunks outside the camera frustum, so their draw ranges are left out until they come back into view. */
    private cullSurfaces(): void {
        if (this.framing !== "map") return;
        this.camera.updateMatrixWorld();
        this.frustum.setFromProjectionMatrix(this.viewProjection.multiplyMatrices(this.camera.projectionMatrix, this.camera.matrixWorldInverse));
        for (const surface of this.surfaces) {
            if (!surface.mesh || surface.chunks.length < 2) continue;
            const next = this.chunksInView(surface.chunks);
            if (sameChunks(next, surface.visibleChunks)) continue;
            surface.visibleChunks = next;
            surface.groupsDirty = true;
        }
    }

    /** The chunks the frustum intersects, or undefined when it intersects all of them. */
    private chunksInView(chunks: readonly THREE.Box3[]): Uint8Array | undefined {
        const visible = new Uint8Array(chunks.length);
        let all = true;
        chunks.forEach((box, chunk) => {
            visible[chunk] = this.frustum.intersectsBox(box) ? 1 : 0;
            all &&= visible[chunk] === 1;
        });
        return all ? undefined : visible;
    }

    private textureDrawModes(textureIds: number[]): { transparent: Set<number>; animated: Set<number> } {
        const transparent = new Set<number>();
        const animated = new Set<number>();
        for (const id of textureIds) {
            const asset = this.textureImages?.get(id);
            if (!asset) continue;
            const hd = this.hdWaterActive && isHdWater(asset.effectId);
            if (asset.alphaMode === 2 || isFixedWater(asset.effectId) || hd) transparent.add(id);
            if (hd || isAnimatedFixedWater(asset)) animated.add(id);
        }
        return { transparent, animated };
    }

    private assignSurfaceGroups(geometry: THREE.BufferGeometry, model: ModelGeometry, surface: MeshSurface): void {
        geometry.clearGroups();
        // Only models with a source ID can update an existing index layout in place.
        surface.sourceIndices = model.sourceId === undefined ? model.indices : Uint32Array.from(model.indices);
        surface.opaqueGroups = [];
        surface.blendedSurfaces = [];
        surface.visibleChunks = undefined;
        surface.animatedWater = false;
        const water = new Set<number>();
        const textureIds = [...this.textures.keys()];
        const textureIndices = new Map(textureIds.map((id, index) => [id, index]));
        const modes = this.textureDrawModes(textureIds);
        const opaque = new Map<number, Map<number, number[]>>();
        const chunks = new SurfaceChunks(model.positions);
        // The animated surface draws a map's animated faces.
        const skipped = surface === this.primary && this.framing === "map" ? animatedFaces(model) : undefined;
        this.collectSurfaceFaces(model, surface, textureIndices, modes, opaque, chunks, skipped, water);
        const order = chunks.finish(surface);
        surface.waterChunks = [...water].map(key => order.get(key) ?? 0);
        for (const face of surface.blendedSurfaces) face.chunk = order.get(face.chunk) ?? 0;
        this.writeOpaqueGroups(geometry, surface, opaque, order);
    }

    private collectSurfaceFaces(model: ModelGeometry, surface: MeshSurface, textureIndices: ReadonlyMap<number, number>,
        modes: ReturnType<ModelScene["textureDrawModes"]>, opaque: Map<number, Map<number, number[]>>, chunks: SurfaceChunks, skipped?: Uint8Array, water = new Set<number>()): void {
        const sourceIndices = surface.sourceIndices;
        const isTranslucent = faceTranslucency(model);
        for (let start = 0; start < model.indices.length; start += 3) {
            const face = start / 3;
            if (skipped?.[face]) continue;
            const a = sourceIndices[start], b = sourceIndices[start + 1], c = sourceIndices[start + 2];
            const translucent = isTranslucent(face, a, b, c);
            const textureId = model.textureIds?.[face] ?? -1;
            const material = this.faceMaterial(model, textureIndices, textureId, translucent);
            const chunk = chunks.add(a, b, c);
            if (modes.animated.has(textureId)) {
                surface.animatedWater = true;
                water.add(chunk);
            }
            if (translucent || modes.transparent.has(textureId)) surface.blendedSurfaces.push(blendedFace(model, start, material, chunk));
            else appendOpaqueFace(opaque, material, chunk, a, b, c);
        }
    }

    private faceMaterial(model: ModelGeometry, textureIndices: ReadonlyMap<number, number>, textureId: number, translucent: boolean): number {
        const textureIndex = textureIndices.get(textureId) ?? -1;
        return terrainSurfaceMaterial(textureId, model.terrainBlends, textureIndices.size, (textureIndex < 0 ? 0 : (textureIndex + 1) * 2) + (translucent ? 1 : 0));
    }

    private writeOpaqueGroups(geometry: THREE.BufferGeometry, surface: MeshSurface, opaque: ReadonlyMap<number, ReadonlyMap<number, number[]>>, order: ReadonlyMap<number, number>): void {
        // Only the renderer's draw index is permuted. Animation and particles retain source face IDs.
        const index = geometry.index!;
        let cursor = 0;
        for (const [materialIndex, byChunk] of opaque) {
            const runs = [...byChunk].map(([key, indices]) => ({ chunk: order.get(key) ?? 0, indices })).sort((left, right) => left.chunk - right.chunk);
            for (const { chunk, indices } of runs) {
                surface.opaqueGroups.push({ start: cursor, count: indices.length, materialIndex, chunk });
                index.array.set(indices, cursor);
                cursor += indices.length;
            }
        }
        // A whole-buffer upload, since the opaque layout changed.
        index.clearUpdateRanges();
        index.needsUpdate = true;
        surface.wholeIndexPending = true;
        // The next frame writes the transparent tail using its final camera transform.
    }

    private updateBlendedDepths(geometry: THREE.BufferGeometry, model: ModelGeometry, surface: MeshSurface): void {
        // Read the array directly: attribute accessors dominate on large maps.
        const positions = geometry.getAttribute("position").array;
        const { x, y, z } = this.viewDepthAxis;
        const camera = this.camera.position;
        const cameraDepth = camera.x * x + camera.y * y + camera.z * z;
        const indices = model.indices;
        for (const face of surface.blendedSurfaces) {
            const a = indices[face.sourceStart] * 3, b = indices[face.sourceStart + 1] * 3, c = indices[face.sourceStart + 2] * 3;
            face.depth = ((positions[a] + positions[b] + positions[c]) * x + (positions[a + 1] + positions[b + 1] + positions[c + 1]) * y
                + (positions[a + 2] + positions[b + 2] + positions[c + 2]) * z) / 3;
            face.bin = Math.floor(Math.log(Math.max(face.depth - cameraDepth, nearestSlabDistance)) / blendedSlabGrowth);
        }
    }

    private orderBlendedSurfaces(geometry: THREE.BufferGeometry, model: ModelGeometry, surface: MeshSurface): void {
        // Opaque/cutout draw ranges are camera-independent; never rescan them during an orbit.
        if (!surface.blendedSurfaces.length && geometry.groups.length && !surface.groupsDirty) return;
        this.updateBlendedDepths(geometry, model, surface);
        // Groups share one object/depth in Three's stable transparent render list.
        surface.blendedSurfaces.sort(compareBlendedFaces);
        this.writeDrawGroups(geometry, surface);
    }

    /** Draws the opaque ranges and the sorted translucent faces of the chunks in view, merging adjacent ranges of one material. */
    private writeDrawGroups(geometry: THREE.BufferGeometry, surface: MeshSurface): void {
        surface.groupsDirty = false;
        geometry.clearGroups();
        const visible = surface.visibleChunks;
        // Frames are bound by draw calls more than triangles, so opaque culling applies only while it adds no draw calls;
        // gaps between culled chunks split each material's ranges.
        const opaqueVisible = visible && opaqueRuns(surface.opaqueGroups, visible) <= opaqueRuns(surface.opaqueGroups) ? visible : undefined;
        const opaqueCount = addOpaqueGroups(geometry, surface.opaqueGroups, opaqueVisible);
        this.writeBlendedGroups(geometry, surface, opaqueCount, visible);
    }

    /** Writes the visible translucent faces after the opaque prefix, re-uploading only that range. */
    private writeBlendedGroups(geometry: THREE.BufferGeometry, surface: MeshSurface, opaqueCount: number, visible: Uint8Array | undefined): void {
        let previous = geometry.groups.at(-1);
        const index = geometry.index!;
        const drawIndices = index.array;
        let cursor = opaqueCount;
        let changed = false;
        for (const face of surface.blendedSurfaces) {
            if (visible && !visible[face.chunk]) continue;
            const start = cursor;
            changed = this.writeBlendedFace(drawIndices, cursor, surface.sourceIndices, face.sourceStart) || changed;
            cursor += 3;
            previous = extendBlendedGroup(geometry, previous, start, face.material);
        }
        if (!changed) return;
        // Only the translucent tail moves; re-uploading the opaque indices of a large map stalled every re-sort. A whole-buffer
        // upload that has not been drawn yet must stay whole.
        if (!surface.wholeIndexPending) index.addUpdateRange(opaqueCount, cursor - opaqueCount);
        index.needsUpdate = true;
    }

    private writeBlendedFace(drawIndices: ArrayLike<number> & { [index: number]: number }, cursor: number, sourceIndices: ArrayLike<number>, start: number): boolean {
        let changed = false;
        for (let corner = 0; corner < 3; corner++) {
            const vertex = sourceIndices[start + corner];
            if (drawIndices[cursor + corner] !== vertex) {
                drawIndices[cursor + corner] = vertex;
                changed = true;
            }
        }
        return changed;
    }
}

function extendBlendedGroup(geometry: THREE.BufferGeometry, previous: THREE.BufferGeometry["groups"][number] | undefined,
    start: number, material: number): THREE.BufferGeometry["groups"][number] {
    if (previous && previous.materialIndex === material && previous.start + previous.count === start) {
        previous.count += 3;
        return previous;
    }
    geometry.addGroup(start, 3, material);
    return geometry.groups[geometry.groups.length - 1];
}

function compareBlendedFaces(a: MeshSurface["blendedSurfaces"][number], b: MeshSurface["blendedSurfaces"][number]): number {
    return a.priority - b.priority || b.bin - a.bin || a.material - b.material || b.depth - a.depth || a.sourceStart - b.sourceStart;
}

function faceTranslucency(model: ModelGeometry): (face: number, a: number, b: number, c: number) => boolean {
    const opacities = model.vertexRgba ?? model.opacities;
    const animatedAlpha = animatedAlphaFaces(model);
    const stride = model.vertexRgba ? 4 : 1;
    const offset = model.vertexRgba ? 3 : 0;
    return (face, a, b, c) => animatedAlpha.has(face) || hasTranslucentVertex(opacities, stride, offset, a, b, c);
}

function appendOpaqueFace(opaque: Map<number, Map<number, number[]>>, material: number, chunk: number, a: number, b: number, c: number): void {
    let byChunk = opaque.get(material);
    if (!byChunk) {
        byChunk = new Map();
        opaque.set(material, byChunk);
    }
    let indices = byChunk.get(chunk);
    if (!indices) {
        indices = [];
        byChunk.set(chunk, indices);
    }
    indices.push(a, b, c);
}

function sameChunks(next: Uint8Array | undefined, previous: Uint8Array | undefined): boolean {
    if (!next || !previous) return next === previous;
    return next.every((value, chunk) => value === previous[chunk]);
}

/** Adds the visible opaque ranges, merging adjacent ranges of one material, and returns the opaque prefix length. */
function addOpaqueGroups(geometry: THREE.BufferGeometry, groups: MeshSurface["opaqueGroups"], visible: Uint8Array | undefined): number {
    let previous: THREE.BufferGeometry["groups"][number] | undefined;
    let count = 0;
    for (const group of groups) {
        count += group.count;
        if (visible && !visible[group.chunk]) continue;
        if (previous && previous.materialIndex === group.materialIndex && previous.start + previous.count === group.start) previous.count += group.count;
        else {
            geometry.addGroup(group.start, group.count, group.materialIndex);
            previous = geometry.groups.at(-1);
        }
    }
    return count;
}

/** Copies a posed range into the drawn geometry and recomputes its flat normals there, uploading only that range. */
function writeDrawnPose(drawn: THREE.BufferGeometry, positions: THREE.BufferAttribute, colors: THREE.BufferAttribute, source: number, target: number, count: number): void {
    const drawnPositions = drawn.getAttribute("position") as THREE.BufferAttribute;
    const drawnColors = drawn.getAttribute("color") as THREE.BufferAttribute;
    const normals = drawn.getAttribute("normal") as THREE.BufferAttribute;
    if (drawn.getAttribute("position") !== positions) {
        drawnPositions.array.set(positions.array.subarray(source * 3, (source + count) * 3), target * 3);
        drawnColors.array.set(colors.array.subarray(source * 4, (source + count) * 4), target * 4);
    }
    drawnPositions.addUpdateRange(target * 3, count * 3);
    drawnColors.addUpdateRange(target * 4, count * 4);
    normals.addUpdateRange(target * 3, count * 3);
    writeTriangleNormals(drawnPositions, normals, target, target + count);
}

/** The bounding sphere of a vertex range at rest, grown for animation. */
function vertexRangeSphere(positions: ArrayLike<number>, start: number, count: number): THREE.Sphere {
    const box = new THREE.Box3(), point = new THREE.Vector3();
    for (let vertex = start; vertex < start + count; vertex++) box.expandByPoint(point.set(positions[vertex * 3], positions[vertex * 3 + 1], positions[vertex * 3 + 2]));
    const sphere = box.getBoundingSphere(new THREE.Sphere());
    sphere.radius += cullChunkMargin;
    return sphere;
}

/** Faces inside the model's animation ranges. */
function animatedFaces(model: ModelGeometry): Uint8Array | undefined {
    if (!model.animations?.length) return undefined;
    const faces = new Uint8Array(model.indices.length / 3);
    for (const { startVertex, vertexCount } of model.animations) faces.fill(1, startVertex / 3, (startVertex + vertexCount) / 3);
    return faces;
}

/** A geometry of the given vertex ranges, with their attributes and face data. Map geometry is unindexed: face `n` is vertices `3n` to `3n + 2`. */
function copyVertexRanges(model: ModelGeometry, ranges: readonly ModelAnimationRange[]): ModelGeometry {
    const count = ranges.reduce((total, range) => total + range.vertexCount, 0);
    const faces = ranges.map(({ startVertex, vertexCount }) => ({ startVertex: startVertex / 3, vertexCount: vertexCount / 3 }));
    const indices = count > 65535 ? new Uint32Array(count) : new Uint16Array(count);
    for (let index = 0; index < count; index++) indices[index] = index;
    return {
        preparedLighting: model.preparedLighting, indices,
        positions: copyRanges(model.positions, ranges, 3, new Float32Array(count * 3)),
        vertexRgba: model.vertexRgba && copyRanges(model.vertexRgba, ranges, 4, new Float32Array(count * 4)),
        uvs: model.uvs && copyRanges(model.uvs, ranges, 2, new Float32Array(count * 2)),
        firstTexel: model.firstTexel && copyRanges(model.firstTexel, ranges, 1, new Uint8Array(count)),
        mapPlanes: model.mapPlanes && copyRanges(model.mapPlanes, ranges, 1, new Uint8Array(count)),
        textureIds: copyRanges(model.textureIds ?? new Int32Array(count / 3).fill(-1), faces, 1, new Int32Array(count / 3)),
        facePriorities: copyRanges(model.facePriorities ?? new Int16Array(count / 3), faces, 1, new Int16Array(count / 3)),
    };
}

/** Copies items of `size` values for each range, one range after another, into `target`. */
function copyRanges<T extends Float32Array | Uint8Array | Int16Array | Int32Array>(source: ArrayLike<number>, ranges: readonly { startVertex: number; vertexCount: number }[], size: number, target: T): T {
    let cursor = 0;
    for (const { startVertex, vertexCount } of ranges) {
        for (let value = 0; value < vertexCount * size; value++) target[cursor + value] = source[startVertex * size + value];
        cursor += vertexCount * size;
    }
    return target;
}

function blendedFace(model: ModelGeometry, start: number, material: number, chunk: number): MeshSurface["blendedSurfaces"][number] {
    return { sourceStart: start, material, depth: 0, bin: 0, priority: model.facePriorities?.[start / 3] ?? 0, chunk };
}

/** Uploads the drawn pose. A primary geometry that draws its own animated faces also needs fresh bounds. */
function markPoseUploaded(geometry: THREE.BufferGeometry, drawn: THREE.BufferGeometry): void {
    for (const name of ["position", "color", "normal"]) drawn.getAttribute(name).needsUpdate = true;
    // The renderer recomputes a null bounding sphere every frame to sort objects, which cost large maps most of a frame.
    // The drawn copy keeps a sphere grown for animation, and the primary geometry no longer draws its animated faces.
    // Poses move parts within their depth slab, so the translucent order is left to camera turns.
    if (drawn !== geometry) return;
    geometry.boundingBox = null;
    geometry.boundingSphere = null;
}

/** The draw calls the opaque ranges of the visible chunks need once adjacent ranges of one material merge. */
function opaqueRuns(groups: MeshSurface["opaqueGroups"], visible?: Uint8Array): number {
    let runs = 0;
    let end = -1, material = -1;
    for (const group of groups) {
        if (visible && !visible[group.chunk]) continue;
        if (group.start !== end || group.materialIndex !== material) runs++;
        end = group.start + group.count;
        material = group.materialIndex;
    }
    return runs;
}

/** Groups faces into square ground chunks by their first vertex and collects each chunk's bounds. */
class SurfaceChunks {
    private readonly bounds = new Map<number, THREE.Box3>();
    private readonly point = new THREE.Vector3();

    constructor(private readonly positions: ModelGeometry["positions"]) {}

    /** The chunk key of a face; keys are only ordered by `finish`. */
    add(a: number, b: number, c: number): number {
        const positions = this.positions;
        const key = Math.floor(positions[a * 3] / cullChunkSize) * 65536 + Math.floor(positions[a * 3 + 2] / cullChunkSize);
        let box = this.bounds.get(key);
        if (!box) {
            box = new THREE.Box3();
            this.bounds.set(key, box);
        }
        for (const vertex of [a, b, c]) box.expandByPoint(this.point.set(positions[vertex * 3], positions[vertex * 3 + 1], positions[vertex * 3 + 2]));
        return key;
    }

    /** Numbers the chunks in row order, so chunks in view tend to be adjacent in each material's ranges, and stores their bounds. */
    finish(surface: MeshSurface): Map<number, number> {
        const keys = [...this.bounds.keys()].sort((left, right) => (left % 65536) - (right % 65536) || left - right);
        surface.chunks = keys.map(key => this.bounds.get(key)!.expandByScalar(cullChunkMargin));
        return new Map(keys.map((key, chunk) => [key, chunk]));
    }
}

function surfaceColorSize(model: ModelGeometry): number {
    if (model.vertexRgba || model.opacities) return 4;
    return model.colors ? 3 : 0;
}

function hasObjectAnimations(model: ModelGeometry): boolean {
    return !!model.animations?.length;
}

function animatedAlphaFaces(model: ModelGeometry): Set<number> {
    const faces = new Set<number>();
    for (const range of model.animations ?? []) {
        range.alphaFaces.forEach((animated, face) => {
            if (animated) faces.add(range.startVertex / 3 + face);
        });
    }
    return faces;
}

function hasTranslucentVertex(opacities: readonly number[] | Float32Array | undefined, stride: number, offset: number, a: number, b: number, c: number): boolean {
    return (opacities?.[a * stride + offset] ?? 1) < 1 || (opacities?.[b * stride + offset] ?? 1) < 1 || (opacities?.[c * stride + offset] ?? 1) < 1;
}

function writeTriangleNormals(positions: THREE.BufferAttribute, normals: THREE.BufferAttribute, start: number, end: number): void {
    const origin = new THREE.Vector3(), edge = new THREE.Vector3(), normal = new THREE.Vector3();
    for (let vertex = start; vertex < end; vertex += 3) {
        origin.fromBufferAttribute(positions, vertex);
        edge.fromBufferAttribute(positions, vertex + 1).sub(origin);
        normal.fromBufferAttribute(positions, vertex + 2).sub(origin);
        normal.crossVectors(edge, normal).normalize();
        for (let corner = 0; corner < 3; corner++) normals.setXYZ(vertex + corner, normal.x, normal.y, normal.z);
    }
    normals.addUpdateRange(start * 3, (end - start) * 3);
}

function writePositions(positions: THREE.BufferAttribute | THREE.InterleavedBufferAttribute, model: ModelGeometry): void {
    for (let vertex = 0; vertex < positions.count; vertex++)
        positions.setXYZ(vertex, model.positions[vertex * 3], model.positions[vertex * 3 + 1], model.positions[vertex * 3 + 2]);
    positions.needsUpdate = true;
}

function writeFirstTexel(markers: THREE.BufferAttribute | THREE.InterleavedBufferAttribute, model: ModelGeometry): void {
    for (let vertex = 0; vertex < markers.count; vertex++) markers.setX(vertex, model.firstTexel?.[vertex] ?? 0);
    markers.needsUpdate = true;
}

function writeColors(colors: THREE.BufferAttribute | THREE.InterleavedBufferAttribute, model: ModelGeometry): void {
    for (let vertex = 0; vertex < colors.count; vertex++) {
        colors.setXYZ(vertex, channel(model.colors, vertex * 3), channel(model.colors, vertex * 3 + 1), channel(model.colors, vertex * 3 + 2));
        if (colors.itemSize === 4) colors.setW(vertex, channel(model.opacities, vertex));
    }
    colors.needsUpdate = true;
}

function channel(values: ArrayLike<number> | undefined, index: number): number {
    return values?.[index] ?? 1;
}

function usesSameMaterials(existing: NonNullable<MeshSurface["mesh"]>, model: ModelGeometry, firstTexel: boolean): boolean {
    return existing.userData["firstTexel"] === firstTexel && existing.userData["preparedLighting"] === !!model.preparedLighting && existing.name === String(model.sourceId);
}

function hasSameLayout(surface: MeshSurface, existing: NonNullable<MeshSurface["mesh"]>, model: ModelGeometry): boolean {
    return existing.geometry.index?.count === model.indices.length && model.indices.every((index, offset) => surface.sourceIndices[offset] === index);
}

function fillGeometry(geometry: THREE.BufferGeometry, model: ModelGeometry, firstTexel: boolean): void {
    geometry.setAttribute("position", new THREE.Float32BufferAttribute(model.positions, 3));
    if (model.mapPlanes) geometry.setAttribute("mapPlane", new THREE.Uint8BufferAttribute(model.mapPlanes, 1));
    if (model.mapPlanes && model.waterDepth) geometry.setAttribute("waterDepth", new THREE.Float32BufferAttribute(model.waterDepth, 1));
    geometry.setIndex(ArrayBuffer.isView(model.indices) ? new THREE.BufferAttribute(model.indices.slice(), 1) : Array.from(model.indices));
    if (model.uvs) geometry.setAttribute("uv", floatAttribute(model.uvs, 2));
    if (firstTexel) geometry.setAttribute("cacheFirstTexel", markerAttribute(model.firstTexel));
    const colors = colorAttribute(model);
    if (colors) geometry.setAttribute("color", colors);
    geometry.computeVertexNormals();
    geometry.computeBoundingSphere();
}

function floatAttribute(values: readonly number[] | Float32Array, size: number): THREE.BufferAttribute {
    return values instanceof Float32Array ? new THREE.BufferAttribute(values, size) : new THREE.Float32BufferAttribute(values, size);
}

function markerAttribute(markers: ModelGeometry["firstTexel"]): THREE.BufferAttribute {
    return markers instanceof Uint8Array ? new THREE.BufferAttribute(markers, 1) : new THREE.Float32BufferAttribute(markers ?? [], 1);
}

function colorAttribute(model: ModelGeometry): THREE.BufferAttribute | undefined {
    if (model.vertexRgba) return new THREE.BufferAttribute(hasObjectAnimations(model) ? model.vertexRgba.slice() : model.vertexRgba, 4);
    if (model.opacities) return new THREE.BufferAttribute(colorsWithOpacity(model, model.opacities), 4);
    return model.colors ? new THREE.Float32BufferAttribute(model.colors, 3) : undefined;
}

function colorsWithOpacity(model: ModelGeometry, opacities: ArrayLike<number>): Float32Array {
    const rgba = new Float32Array(opacities.length * 4);
    const colors = model.colors;
    for (let vertex = 0; vertex < opacities.length; vertex++) {
        rgba.set(colors ? [colors[vertex * 3], colors[vertex * 3 + 1], colors[vertex * 3 + 2]] : [1, 1, 1], vertex * 4);
        rgba[vertex * 4 + 3] = opacities[vertex];
    }
    return rgba;
}

/** Faces under the ray, nearest first, except fully transparent ones and see-through planes, which must not block a pick. */
function pickableHits(ray: THREE.Raycaster, surface: MeshSurface, focus: PlaneFocus): SurfaceHit[] {
    if (!surface.mesh?.visible) return [];
    const shown = vertexVisibility(surface.currentModel);
    const planes = surface.currentModel?.mapPlanes;
    const passesThrough = (plane: number | undefined) => plane !== undefined && plane < 4 && (focus.ghosted.has(plane) || focus.hidden.has(plane));
    return ray.intersectObject(surface.mesh, false).flatMap(({ face, point, distance }) => {
        if (!face || !(shown(face.a) || shown(face.b) || shown(face.c))) return [];
        const plane = planes?.[face.a];
        return passesThrough(plane) ? [] : [{ surface, vertex: face.a, plane, x: point.x, z: point.z, distance }];
    });
}

function vertexVisibility(model: ModelGeometry | undefined): (vertex: number) => boolean {
    if (model?.vertexRgba) {
        const rgba = model.vertexRgba;
        return vertex => rgba[vertex * 4 + 3] > 0;
    }
    const opacities = model?.opacities;
    return vertex => !opacities || opacities[vertex] > 0;
}

/** Six vertices share each tile's colour. */
function strokePreviewColors(tiles: readonly number[]): Float32Array {
    const color = new THREE.Color();
    const colors = new Float32Array(tiles.length * 18);
    tiles.forEach((rgb, tile) => {
        color.setHex(rgb);
        for (let vertex = 0; vertex < 6; vertex++) colors.set([color.r, color.g, color.b], tile * 18 + vertex * 3);
    });
    return colors;
}

/**
 * Effect 1 (GameClient Class49_Sub7): adds a generated specular lobe to the lit surface, in the client's gamma space.
 * The effect parameter picks the exponent and the texture's alpha masks the result. The lobe's cube is sampled by the eye-space reflection through a texture
 * matrix of 26 degrees about X and -12 about Y, which puts the highlight at this view-space direction.
 */
const clientSpecularHighlight = `
    float cacheLobeExponent = cachePhase.w < 0.5 ? 0.0 : cachePhase.w < 1.5 ? 128.0 : cachePhase.w < 2.5 ? 24.0 : 5.0;
    // Class49_Sub7 looks its lobe cube up by eye-space normal (GL_NORMAL_MAP), not by reflection.
    float cacheLobeFacing = clamp(dot(normal, normalize(vec3(0.091, 0.899, -0.428))), 0.0, 1.0);
    float cacheSpecularLobe = cacheLobeExponent > 0.0 && cacheLobeFacing > 0.0 ? min(1.0, pow(cacheLobeFacing, cacheLobeExponent) * 400.0 / 255.0) * sampledDiffuseColor.a : 0.0;
    outgoingLight = pow(min(pow(max(outgoingLight, vec3(0.0)), vec3(1.0 / 2.2)) + cacheSpecularLobe, vec3(1.0)), vec3(2.2));
`;

function applyFirstTexelSampling(material: THREE.MeshBasicMaterial | THREE.MeshStandardMaterial, asset: ModelTexture): void {
    const compile = material.onBeforeCompile;
    const key = material.customProgramCacheKey.bind(material);
    material.customProgramCacheKey = () => `${key()}|first-texel-v1`;
    material.onBeforeCompile = (shader, renderer) => {
        compile.call(material, shader, renderer);
        const width = asset.hdr?.width ?? asset.image.width;
        const height = asset.hdr?.height ?? asset.image.height;
        // ImageBitmap and HDR rows are flipped on upload. Sample the original top-left texel centre.
        shader.uniforms.cacheFirstTexelUv = { value: new THREE.Vector2(0.5 / width, 1 - 0.5 / height) };
        shader.vertexShader = `attribute float cacheFirstTexel;\nvarying float vCacheFirstTexel;\n${shader.vertexShader}`
            .replace("#include <begin_vertex>", "#include <begin_vertex>\nvCacheFirstTexel = cacheFirstTexel;");
        shader.fragmentShader = `varying float vCacheFirstTexel;\nuniform vec2 cacheFirstTexelUv;\n${shader.fragmentShader}`
            .replace("#include <map_fragment>", THREE.ShaderChunk.map_fragment.replace("vMapUv", "(vCacheFirstTexel > 0.5 ? cacheFirstTexelUv : vMapUv)"));
    };
}

function createModelTexture(asset: ModelTexture): THREE.Texture {
    let texture: THREE.Texture;
    if (asset.hdr) {
        const {width, height, rgba} = asset.hdr;
        // Match ImageBitmap orientation; DataTexture does not flip on upload.
        const data = new Float32Array(rgba.length);
        for (let y = 0; y < height; y++)
            data.set(rgba.slice(y * width * 4, (y + 1) * width * 4), (height - y - 1) * width * 4);
        texture = new THREE.DataTexture(data, width, height, THREE.RGBAFormat, THREE.FloatType);
        texture.colorSpace = THREE.LinearSRGBColorSpace;
    } else {
        texture = new THREE.Texture(asset.image);
        texture.colorSpace = THREE.SRGBColorSpace;
    }
    texture.wrapS = asset.repeatU === false ? THREE.ClampToEdgeWrapping : THREE.RepeatWrapping;
    texture.wrapT = asset.repeatV === false ? THREE.ClampToEdgeWrapping : THREE.RepeatWrapping;
    texture.needsUpdate = true;
    return texture;
}
