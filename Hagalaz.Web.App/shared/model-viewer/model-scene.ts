import * as THREE from "three";
import { OrbitControls } from "three/addons/controls/OrbitControls.js";
import { clientFixedWater, clientHdWater, clientReflection, fixedWaterParameters, isFixedWater, sampleMaterialEffect, type MaterialEffect } from "./material-effects";
import {ModelParticles, type ModelParticle} from "./model-particles";
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
    positions: readonly number[];
    indices: readonly number[] | Uint16Array | Uint32Array;
    colors?: readonly number[];
    opacities?: readonly number[];
    vertexRgba?: Float32Array;
    uvs?: readonly number[] | Float32Array;
    firstTexel?: readonly number[] | Uint8Array;
    textureIds?: readonly number[];
    terrainBlends?: readonly TerrainBlend[];
    terrainWeights?: readonly number[];
    mapPlanes?: readonly number[];
    waterDepth?: readonly number[];
    facePriorities?: readonly number[];
    particles?: readonly ModelParticle[];
    particleScale?: number;
    animations?: readonly ModelAnimationRange[];
    /** This geometry equals `base` except for the vertices in `ranges`, as [start, end) pairs. */
    patchOf?: { base: ModelGeometry; ranges: readonly (readonly [number, number])[] };
}

/** A contiguous set of map triangles whose vertices can be replaced together. */
export interface ModelAnimationRange {
    startVertex: number;
    vertexCount: number;
    alphaFaces: readonly boolean[];
    /** The renderer copies the pose before sampling another range. */
    sample(elapsed: number, previousElapsed?: number): { delay: number; pose?: { positions: Float32Array; rgba: Float32Array } };
}

/** Unindexed triangles drawn over a scene: six vertices and one sRGB colour per tile. */
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
    opaqueGroups: { start: number; count: number; materialIndex: number }[];
    blendedSurfaces: { sourceStart: number; material: number; depth: number; priority: number }[];
    materials: Map<string, { material: SurfaceMaterial; texture?: THREE.Texture; asset?: ModelTexture }>;
    texturesDirty: boolean;
    animatedWater: boolean;
}

interface SurfaceHit { surface: MeshSurface; vertex: number; x: number; z: number; distance: number; }

function createMeshSurface(): MeshSurface {
    return { orderingDirty: true, sourceIndices: [], opaqueGroups: [], blendedSurfaces: [], materials: new Map(), texturesDirty: false, animatedWater: false };
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

interface RegionSun {
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
    private readonly mapLighting = new MapLightMaterial();
    private readonly viewDepthAxis = new THREE.Vector3(0, 0, -1);
    private readonly centroid = new THREE.Vector3();
    private epoch?: number;
    private lastTime?: number;
    private particles?: ModelParticles;
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
    private orientationGizmo?: OrientationGizmo;
    private selectionBounds?: THREE.Box3Helper;
    private placementGizmo?: PlacementGizmo;
    private brushOutline?: THREE.LineLoop<THREE.BufferGeometry, THREE.LineBasicMaterial>;
    private strokePreview?: THREE.Mesh<THREE.BufferGeometry, THREE.MeshBasicMaterial>;
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
            if (this.surfaces.some(surface => surface.mesh?.visible && surface.animatedWater)) this.requestRender();
        });
    };

    showOrientationGizmo(): void {
        if (this.disposed || this.orientationGizmo) return;
        this.orientationGizmo = new OrientationGizmo(this.camera);
        this.requestRender();
    }

    /** The primary surface's vertex under the point, unless other geometry is in front of it. */
    pickVertex(clientX: number, clientY: number): number | null {
        const hit = this.nearestHit(clientX, clientY, this.surfaces);
        return hit?.surface === this.primary ? hit.vertex : null;
    }

    /** The ground under the point, looking through objects. */
    pickSurface(clientX: number, clientY: number): { vertex: number; x: number; z: number } | null {
        const hit = this.nearestHit(clientX, clientY, [this.terrain]);
        return hit ? { vertex: hit.vertex, x: hit.x, z: hit.z } : null;
    }

    private nearestHit(clientX: number, clientY: number, surfaces: readonly MeshSurface[]): SurfaceHit | null {
        const ray = this.pointerRay(clientX, clientY);
        if (!ray) return null;
        const hits = surfaces.flatMap(surface => visibleHit(ray, surface));
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
        this.strokePreview ??= this.createStrokePreview();
        const { geometry, material } = this.strokePreview;
        geometry.setAttribute("position", new THREE.Float32BufferAttribute(preview.positions, 3));
        geometry.setAttribute("uv", new THREE.Float32BufferAttribute(preview.uvs, 2));
        geometry.setAttribute("color", new THREE.Float32BufferAttribute(strokePreviewColors(preview.rgb), 3));
        const map = (preview.textureId === null ? undefined : this.textures.get(preview.textureId)) ?? null;
        if (material.map !== map) {
            material.map = map;
            material.needsUpdate = true;
        }
        material.opacity = preview.opacity;
        material.transparent = preview.opacity < 1;
        this.strokePreview.visible = true;
        this.requestRender();
    }

    private createStrokePreview(): THREE.Mesh<THREE.BufferGeometry, THREE.MeshBasicMaterial> {
        const material = new THREE.MeshBasicMaterial({ vertexColors: true, side: THREE.DoubleSide, depthWrite: false, polygonOffset: true, polygonOffsetFactor: -4, polygonOffsetUnits: -4 });
        const mesh = new THREE.Mesh(new THREE.BufferGeometry(), material);
        mesh.frustumCulled = false;
        mesh.renderOrder = 1;
        this.scene.add(mesh);
        return mesh;
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
        if (grid && !this.grid) {
            this.grid = new THREE.GridHelper(20, 40, 0x253141, 0x121a24);
            this.gridFittedTo.makeEmpty();
            this.scene.add(this.grid);
            this.fitGrid();
        }
        if (this.grid) this.grid.visible = grid;
        this.applyWireframe();
        this.requestRender();
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

    private readonly renderFrame = (time: number): void => {
        this.epoch ??= time;
        const seconds = (time - this.epoch) / 1000;
        for (const [id, texture] of this.textures) {
            const phase = sampleMaterialEffect(this.textureImages?.get(id) ?? {}, seconds);
            texture.offset.set(phase.u, phase.v);
        }
        for (const update of this.effectUpdates.values()) update(seconds);
        if (this.primary.currentModel) this.particles?.update(this.lastTime === undefined ? 0 : (time-this.lastTime)/1000, this.primary.currentModel,
            this.host.clientHeight * Math.min(window.devicePixelRatio,2) * this.camera.projectionMatrix.elements[5] / 2);
        this.lastTime = time;
        if (this.framing === "map") this.renderObjectAnimations(time);
        this.controls.update();
        this.placementGizmo?.fitToCamera();
        this.orderSurfaces();
        this.renderer.render(this.scene, this.camera);
        if (this.orientationGizmo) {
            const autoClear = this.renderer.autoClear;
            this.renderer.autoClear = false;
            try {
                this.orientationGizmo.render(this.renderer);
            } finally {
                this.renderer.autoClear = autoClear;
            }
        }
    };

    setObjectAnimationsEnabled(enabled: boolean): void {
        this.objectAnimationsEnabled = enabled;
        this.requestRender();
    }

    private renderObjectAnimations(time: number): void {
        const surface = this.primary;
        const model = surface.currentModel;
        const mesh = surface.mesh;
        const animations = model?.animations;
        if (animations && this.objectAnimationsEnabled && this.lastObjectAnimationTime !== undefined &&
            time < this.nextObjectAnimationTime) return;
        clearTimeout(this.animationTimer);
        this.animationTimer = undefined;
        if (!animations || !model.vertexRgba || !mesh) {
            this.objectAnimationEpoch = undefined;
            this.lastObjectAnimationTime = undefined;
            return;
        }
        if (!this.objectAnimationsEnabled) {
            if (this.lastObjectAnimationTime !== undefined) this.updateObjectAnimationRanges(mesh, animations, model.positions, model.vertexRgba, null);
            this.lastObjectAnimationTime = undefined;
            this.objectAnimationEpoch = undefined;
            return;
        }
        this.objectAnimationEpoch ??= time;
        const elapsed = Math.max(time - this.objectAnimationEpoch, 0);
        const next = this.updateObjectAnimationRanges(mesh, animations, model.positions, model.vertexRgba, elapsed);
        this.lastObjectAnimationTime = elapsed;
        this.nextObjectAnimationTime = time + next;
        if (next < Infinity) this.animationTimer = setTimeout(() => {
            // A frame timestamp can precede a timer callback in the same browser frame.
            this.nextObjectAnimationTime = 0;
            this.requestRender();
        }, next);
    }

    private updateObjectAnimationRanges(
        mesh: NonNullable<MeshSurface["mesh"]>, animations: readonly ModelAnimationRange[],
        basePositions: readonly number[], baseColors: Float32Array, elapsed: number | null,
    ): number {
        const { geometry } = mesh;
        const positions = geometry.getAttribute("position") as THREE.BufferAttribute;
        const colors = geometry.getAttribute("color") as THREE.BufferAttribute;
        const normals = geometry.getAttribute("normal") as THREE.BufferAttribute;
        let next = Infinity;
        let updated = false;
        for (const animation of animations) {
            const start = animation.startVertex;
            const end = start + animation.vertexCount;
            if (elapsed !== null) {
                const { delay, pose } = animation.sample(elapsed, this.lastObjectAnimationTime);
                next = Math.min(next, delay);
                if (!pose) continue;
                positions.array.set(pose.positions, start * 3);
                colors.array.set(pose.rgba, start * 4);
            } else {
                positions.array.set(basePositions.slice(start * 3, end * 3), start * 3);
                colors.array.set(baseColors.subarray(start * 4, end * 4), start * 4);
            }
            positions.addUpdateRange(start * 3, animation.vertexCount * 3);
            colors.addUpdateRange(start * 4, animation.vertexCount * 4);
            writeTriangleNormals(positions, normals, start, end);
            updated = true;
        }
        if (updated) {
            positions.needsUpdate = true;
            colors.needsUpdate = true;
            normals.needsUpdate = true;
            geometry.boundingBox = null;
            geometry.boundingSphere = null;
            this.primary.orderingDirty = true;
        }
        return next;
    }

    setGeometry(model: ModelGeometry, fitCamera = true, surface = this.primary): void {
        if (this.disposed) throw new Error("The model scene has been disposed.");
        const previousModel = surface.currentModel;
        if (previousModel === model && surface.mesh && !surface.texturesDirty) return;
        if (this.applyPositionPatch(surface, model)) return;
        surface.currentModel = model;
        surface.orderingDirty = true;
        this.requestRender();
        const existing = surface.mesh;
        const firstTexel = model.firstTexel?.some(value => value !== 0) ?? false;
        if (this.updateInPlace(surface, model, previousModel, firstTexel)) return;
        const retainedGeometry = previousModel === model ? existing?.geometry : undefined;
        this.clear(true, !!retainedGeometry, surface);
        if (surface === this.primary) {
            this.epoch = undefined;
            this.lastTime = undefined;
            this.lastObjectAnimationTime = undefined;
        }
        const geometry = retainedGeometry ?? new THREE.BufferGeometry();
        if (!retainedGeometry) fillGeometry(geometry, model, firstTexel);
        const usedMaterials = new Set<string>();
        const useMaterial = <T extends SurfaceMaterial>(key: string, create: () => T, texture?: THREE.Texture, asset?: ModelTexture): T => {
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
        const colorSize = model.vertexRgba || model.opacities ? 4 : model.colors ? 3 : 0;
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
        applyTerrainMaterials(geometry, materials, retainedGeometry ? undefined : model.terrainWeights, model.terrainBlends, this.textures, blend => {
            const key = `terrain:${!!model.mapPlanes}:${blend.layers.map(layer => `${layer.textureId}:${layer.textureScale}:${!!layer.reflective}`).join("|")}`;
            const material = useMaterial(key, () => new TerrainMaterial(blend, this.textures, { map: this.environmentMap, enabled: this.environmentEnabled }));
            material.updateTextures(this.textures);
            return material;
        });
        for (const [key, entry] of surface.materials) {
            if (usedMaterials.has(key)) continue;
            entry.material.dispose();
            this.effectUpdates.delete(entry.material);
            surface.materials.delete(key);
        }
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
        if (fitCamera) this.resetCamera();
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
        if (fitCamera) this.resetCamera();
    }

    setMapPointLights(field: MapLightField | null, enabled: boolean): void {
        this.mapLighting.set(field, enabled);
        this.requestRender();
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
        return [this.terrain, this.primary];
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
        this.resizeObserver.disconnect();
        this.mapNavigation?.dispose();
        this.controls.dispose();
        this.orientationGizmo?.dispose();
        this.placementGizmo?.dispose();
        this.releaseOverlays();
        this.clear(false, false, this.terrain);
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
        if (this.strokePreview) {
            this.scene.remove(this.strokePreview);
            this.strokePreview.geometry.dispose();
            this.strokePreview.material.dispose();
        }
    }

    private clear(preserveMaterials = false, preserveGeometry = false, surface = this.primary): void {
        if (surface === this.primary) {
            this.particles?.dispose();
            this.particles = undefined;
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
        // Translation adds the same depth to every face. Ignore roundoff in the axis during a pan.
        const axis = this.viewDepthAxis;
        const turned = Math.max(Math.abs(x - axis.x), Math.abs(y - axis.y), Math.abs(z - axis.z)) > 1e-10;
        if (turned) axis.set(x, y, z);
        for (const surface of this.surfaces) {
            if (!surface.mesh || !surface.currentModel || (!turned && !surface.orderingDirty)) continue;
            surface.orderingDirty = false;
            this.orderBlendedSurfaces(surface.mesh.geometry, surface.currentModel, surface);
        }
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
        surface.animatedWater = false;
        const textureIds = [...this.textures.keys()];
        const textureIndices = new Map(textureIds.map((id, index) => [id, index]));
        const modes = this.textureDrawModes(textureIds);
        const opaque = new Map<number, number[]>();
        const sourceIndices = surface.sourceIndices;
        const opacities = model.vertexRgba ?? model.opacities;
        const animatedAlpha = animatedAlphaFaces(model);
        const stride = model.vertexRgba ? 4 : 1, offset = model.vertexRgba ? 3 : 0;
        for (let start = 0; start < model.indices.length; start += 3) {
            const face = start / 3;
            const a = sourceIndices[start];
            const b = sourceIndices[start + 1];
            const c = sourceIndices[start + 2];
            const translucent = animatedAlpha.has(face) || hasTranslucentVertex(opacities, stride, offset, a, b, c);
            const textureId = model.textureIds?.[face] ?? -1;
            const textureIndex = textureIndices.get(textureId) ?? -1;
            const material = terrainSurfaceMaterial(textureId, model.terrainBlends, textureIds.length, (textureIndex < 0 ? 0 : (textureIndex + 1) * 2) + (translucent ? 1 : 0));
            if (modes.animated.has(textureId)) surface.animatedWater = true;
            const transparent = translucent || modes.transparent.has(textureId);
            if (transparent) {
                surface.blendedSurfaces.push({ sourceStart: start, material, depth: 0, priority: model.facePriorities?.[face] ?? 0 });
            } else {
                let indices = opaque.get(material);
                if (!indices) { indices = []; opaque.set(material, indices); }
                indices.push(a, b, c);
            }
        }
        // Only the renderer's draw index is permuted. Animation and particles retain source face IDs.
        const index = geometry.index!;
        let cursor = 0;
        for (const [materialIndex, indices] of opaque) {
            surface.opaqueGroups.push({ start: cursor, count: indices.length, materialIndex });
            index.array.set(indices, cursor);
            cursor += indices.length;
        }
        index.needsUpdate = true;
        // The next frame writes the transparent tail using its final camera transform.
    }

    private orderBlendedSurfaces(geometry: THREE.BufferGeometry, model: ModelGeometry, surface: MeshSurface): void {
        // Opaque/cutout draw ranges are camera-independent; never rescan them during an orbit.
        if (!surface.blendedSurfaces.length && geometry.groups.length) return;
        geometry.clearGroups();
        for (const group of surface.opaqueGroups) geometry.addGroup(group.start, group.count, group.materialIndex);
        const positions = geometry.getAttribute("position");
        for (const face of surface.blendedSurfaces) {
            this.centroid.set(0, 0, 0);
            for (let corner = 0; corner < 3; corner++) {
                const vertex = model.indices[face.sourceStart + corner];
                this.centroid.x += positions.getX(vertex) / 3;
                this.centroid.y += positions.getY(vertex) / 3;
                this.centroid.z += positions.getZ(vertex) / 3;
            }
            face.depth = this.centroid.dot(this.viewDepthAxis);
        }
        // Groups share one object/depth in Three's stable transparent render list.
        surface.blendedSurfaces.sort((a, b) => a.priority - b.priority || b.depth - a.depth || a.sourceStart - b.sourceStart);
        const index = geometry.index!;
        const drawIndices = index.array;
        let cursor = surface.opaqueGroups.reduce((count, group) => count + group.count, 0);
        let changed = false;
        let previous = geometry.groups[geometry.groups.length - 1];
        for (const face of surface.blendedSurfaces) {
            const start = cursor;
            for (let corner = 0; corner < 3; corner++) {
                const vertex = surface.sourceIndices[face.sourceStart + corner];
                if (drawIndices[cursor] !== vertex) {
                    drawIndices[cursor] = vertex;
                    changed = true;
                }
                cursor++;
            }
            if (previous && previous.materialIndex === face.material && previous.start + previous.count === start)
                previous.count += 3;
            else {
                geometry.addGroup(start, 3, face.material);
                previous = geometry.groups[geometry.groups.length - 1];
            }
        }
        if (changed) index.needsUpdate = true;
    }
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

function channel(values: readonly number[] | undefined, index: number): number {
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

function colorsWithOpacity(model: ModelGeometry, opacities: readonly number[]): Float32Array {
    const rgba = new Float32Array(opacities.length * 4);
    const colors = model.colors;
    for (let vertex = 0; vertex < opacities.length; vertex++) {
        rgba.set(colors ? [colors[vertex * 3], colors[vertex * 3 + 1], colors[vertex * 3 + 2]] : [1, 1, 1], vertex * 4);
        rgba[vertex * 4 + 3] = opacities[vertex];
    }
    return rgba;
}

/** The closest face that is not fully transparent, since invisible surfaces must not block a pick. */
function visibleHit(ray: THREE.Raycaster, surface: MeshSurface): SurfaceHit[] {
    if (!surface.mesh?.visible) return [];
    const shown = vertexVisibility(surface.currentModel);
    const hit = ray.intersectObject(surface.mesh, false).find(({ face }) => face && (shown(face.a) || shown(face.b) || shown(face.c)));
    return hit?.face ? [{ surface, vertex: hit.face.a, x: hit.point.x, z: hit.point.z, distance: hit.distance }] : [];
}

function vertexVisibility(model: ModelGeometry | undefined): (vertex: number) => boolean {
    if (model?.vertexRgba) {
        const rgba = model.vertexRgba;
        return vertex => rgba[vertex * 4 + 3] > 0;
    }
    const opacities = model?.opacities;
    return vertex => !opacities || opacities[vertex] > 0;
}

function strokePreviewColors(tiles: readonly number[]): number[] {
    const color = new THREE.Color();
    return tiles.flatMap(rgb => {
        color.setHex(rgb);
        return Array.from({ length: 6 }, () => [color.r, color.g, color.b]).flat();
    });
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
