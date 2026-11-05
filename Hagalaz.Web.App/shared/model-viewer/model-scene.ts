import * as THREE from "three";
import { OrbitControls } from "three/addons/controls/OrbitControls.js";
import { sampleMaterialEffect, type MaterialEffect } from "./material-effects";
import {ModelParticles, type ModelParticle} from "./model-particles";
import { applyTerrainMaterials, TerrainMaterial, terrainSurfaceMaterial, type TerrainBlend } from "./terrain-material";

type SurfaceMaterial = THREE.MeshStandardMaterial | THREE.MeshBasicMaterial;

/** Renderer-neutral geometry accepted by both web applications. */
export interface ModelGeometry {
    sourceId?: number;
    preparedLighting?: boolean;
    positions: readonly number[];
    indices: readonly number[];
    colors?: readonly number[];
    opacities?: readonly number[];
    uvs?: readonly number[];
    firstTexel?: readonly number[];
    textureIds?: readonly number[];
    terrainBlends?: readonly TerrainBlend[];
    terrainWeights?: readonly number[];
    facePriorities?: readonly number[];
    particles?: readonly ModelParticle[];
    particleScale?: number;
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

/** Owns the canvas and GPU resources for a single model inspection surface. */
export class ModelScene {
    private readonly renderer: THREE.WebGLRenderer;
    private readonly scene = new THREE.Scene();
    private readonly camera = new THREE.PerspectiveCamera(40, 1, 0.01, 100000);
    private readonly controls: OrbitControls;
    private readonly resizeObserver: ResizeObserver;
    private mesh?: THREE.Mesh<THREE.BufferGeometry, (THREE.MeshStandardMaterial | THREE.MeshBasicMaterial)[]>;
    private orderingDirty = true;
    private sourceIndices: number[] = [];
    private opaqueGroups: { start: number; count: number; materialIndex: number }[] = [];
    private blendedSurfaces: { sourceStart: number; start: number; material: number; depth: number; priority: number }[] = [];
    private readonly viewMatrix = new THREE.Matrix4();
    private readonly centroid = new THREE.Vector3();
    private epoch?: number;
    private lastTime?: number;
    private currentModel?: ModelGeometry;
    private particles?: ModelParticles;
    private readonly effectUpdates = new Map<THREE.Material, (seconds: number) => void>();
    private readonly materials = new Map<string, { material: SurfaceMaterial; texture?: THREE.Texture; asset?: ModelTexture }>();
    private texturesDirty = false;
    private disposed = false;
    private textureImages?: ReadonlyMap<number, ModelTexture>;
    private readonly textures = new Map<number, THREE.Texture>();
    private grid?: THREE.GridHelper;
    private wireframe = false;

    setDisplay(grid: boolean, wireframe: boolean): void {
        this.wireframe = wireframe;
        if (grid && !this.grid) {
            this.grid = new THREE.GridHelper(20, 40, 0x253141, 0x121a24);
            this.scene.add(this.grid);
            this.fitGrid();
        }
        if (this.grid) this.grid.visible = grid;
        this.applyWireframe();
    }

    private applyWireframe(): void {
        if (this.mesh) for (const material of this.mesh.material) material.wireframe = this.wireframe;
    }

    private fitGrid(): void {
        const bounds = this.mesh?.geometry.boundingSphere;
        if (this.grid && this.mesh && bounds) {
            this.grid.scale.setScalar(Math.max(bounds.radius, 1) / 2);
            this.grid.position.set(bounds.center.x, new THREE.Box3().setFromObject(this.mesh).min.y, bounds.center.z);
        }
    }

    setTextures(images: ReadonlyMap<number, ModelTexture>): void {
        if (images === this.textureImages) return;
        const previous = this.textureImages;
        this.textureImages = images;
        if (images.size === this.textures.size && [...images].every(([id, asset]) => previous?.get(id) === asset)) return;
        this.texturesDirty = true;
        this.syncTextures(images, previous);
        // Callers publish the matching geometry next; do not draw retired image bindings meanwhile.
        if (this.mesh) this.mesh.visible = false;
        const liveTextures = new Set(this.textures.values());
        for (const entry of this.materials.values()) {
            if (entry.texture && !liveTextures.has(entry.texture)) this.effectUpdates.delete(entry.material);
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

    constructor(private readonly host: HTMLElement, label = "Interactive 3D model") {
        this.renderer = new THREE.WebGLRenderer({ antialias: true, alpha: true });
        this.renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
        this.renderer.domElement.setAttribute("aria-label", label);
        host.appendChild(this.renderer.domElement);
        this.controls = new OrbitControls(this.camera, this.renderer.domElement);
        this.scene.add(new THREE.HemisphereLight(0xffffff, 0x667788, 2));
        const light = new THREE.DirectionalLight(0xffffff, 3);
        light.position.set(1, 2, 3);
        this.scene.add(light);
        this.resizeObserver = new ResizeObserver(() => this.resize());
        this.resizeObserver.observe(host);
        this.resize();
        this.renderer.setAnimationLoop(time => {
            this.epoch ??= time;
            const seconds = (time - this.epoch) / 1000;
            for (const [id, texture] of this.textures) {
                const phase = sampleMaterialEffect(this.textureImages?.get(id) ?? {}, seconds);
                texture.offset.set(phase.u, phase.v);
            }
            for (const update of this.effectUpdates.values()) update(seconds);
            if (this.currentModel) this.particles?.update(this.lastTime === undefined ? 0 : (time-this.lastTime)/1000, this.currentModel,
                this.host.clientHeight * Math.min(window.devicePixelRatio,2) * this.camera.projectionMatrix.elements[5] / 2);
            this.lastTime = time;
            this.controls.update();
            this.orderSurfaces();
            this.renderer.render(this.scene, this.camera);
        });
    }

    setGeometry(model: ModelGeometry): void {
        if (this.disposed) throw new Error("The model scene has been disposed.");
        const previousModel = this.currentModel;
        if (previousModel === model && this.mesh && !this.texturesDirty) return;
        this.currentModel = model;
        this.orderingDirty = true;
        const existing = this.mesh;
        const firstTexel = model.firstTexel?.some(value => value !== 0) ?? false;
        if (!this.texturesDirty && existing && existing.userData["firstTexel"] === firstTexel && existing.userData["preparedLighting"] === !!model.preparedLighting && model.sourceId !== undefined && existing.name === String(model.sourceId)
            && existing.geometry.index?.count === model.indices.length
            && model.indices.every((index, offset) => this.sourceIndices[offset] === index)) {
            const positions = existing.geometry.getAttribute("position");
            if (positions.count * 3 === model.positions.length) {
                for (let vertex = 0; vertex < positions.count; vertex++)
                    positions.setXYZ(vertex, model.positions[vertex * 3], model.positions[vertex * 3 + 1], model.positions[vertex * 3 + 2]);
                positions.needsUpdate = true;
                if (firstTexel) {
                    const markers = existing.geometry.getAttribute("cacheFirstTexel");
                    for (let vertex = 0; vertex < markers.count; vertex++) markers.setX(vertex, model.firstTexel?.[vertex] ?? 0);
                    markers.needsUpdate = true;
                }
                const colors = existing.geometry.getAttribute("color");
                if (colors) {
                    for (let vertex = 0; vertex < colors.count; vertex++) {
                        colors.setXYZ(vertex, model.colors?.[vertex * 3] ?? 1, model.colors?.[vertex * 3 + 1] ?? 1, model.colors?.[vertex * 3 + 2] ?? 1);
                        if (colors.itemSize === 4) colors.setW(vertex, model.opacities?.[vertex] ?? 1);
                    }
                    colors.needsUpdate = true;
                }
                this.assignSurfaceGroups(existing.geometry, model);
                // Normal computation needs a complete index before the next frame.
                this.orderBlendedSurfaces(existing.geometry, model);
                existing.geometry.computeVertexNormals();
                existing.geometry.computeBoundingSphere();
                return;
            }
        }
        const retainedGeometry = previousModel === model ? existing?.geometry : undefined;
        this.clear(true, !!retainedGeometry);
        this.epoch = undefined;
        this.lastTime = undefined;
        const geometry = retainedGeometry ?? new THREE.BufferGeometry();
        if (!retainedGeometry) {
            geometry.setAttribute("position", new THREE.Float32BufferAttribute(model.positions, 3));
            geometry.setIndex(Array.from(model.indices));
            if (model.uvs) geometry.setAttribute("uv", new THREE.Float32BufferAttribute(model.uvs, 2));
            if (firstTexel) geometry.setAttribute("cacheFirstTexel", new THREE.Float32BufferAttribute(model.firstTexel!, 1));
            if (model.opacities) {
                const rgba = new Float32Array(model.opacities.length * 4);
                for (let vertex = 0; vertex < model.opacities.length; vertex++) {
                    const rgbOffset = vertex * 3;
                    const rgbaOffset = vertex * 4;
                    rgba[rgbaOffset] = model.colors?.[rgbOffset] ?? 1;
                    rgba[rgbaOffset + 1] = model.colors?.[rgbOffset + 1] ?? 1;
                    rgba[rgbaOffset + 2] = model.colors?.[rgbOffset + 2] ?? 1;
                    rgba[rgbaOffset + 3] = model.opacities[vertex];
                }
                geometry.setAttribute("color", new THREE.BufferAttribute(rgba, 4));
            } else if (model.colors) geometry.setAttribute("color", new THREE.Float32BufferAttribute(model.colors, 3));
            geometry.computeVertexNormals();
            geometry.computeBoundingSphere();
        }
        const usedMaterials = new Set<string>();
        const useMaterial = <T extends SurfaceMaterial>(key: string, create: () => T, texture?: THREE.Texture, asset?: ModelTexture): T => {
            usedMaterials.add(key);
            const existing = this.materials.get(key);
            if (existing && existing.texture === texture && existing.asset === asset) return existing.material as T;
            if (existing) {
                existing.material.dispose();
                this.effectUpdates.delete(existing.material);
            }
            const material = create();
            this.materials.set(key, { material, texture, asset });
            return material;
        };
        const options = { vertexColors: !!model.colors || !!model.opacities, ...(model.preparedLighting ? {} : {roughness: 0.8}) };
        const surfaceKey = `${!!model.preparedLighting}:${model.opacities ? 4 : model.colors ? 3 : 0}`;
        const OrdinaryMaterial = model.preparedLighting ? THREE.MeshBasicMaterial : THREE.MeshStandardMaterial;
        const materials: (THREE.MeshStandardMaterial | THREE.MeshBasicMaterial)[] = [
            useMaterial(`plain:${surfaceKey}:false`, () => new OrdinaryMaterial({ ...options, transparent: false, depthWrite: true })),
            useMaterial(`plain:${surfaceKey}:true`, () => new OrdinaryMaterial({ ...options, transparent: true, depthWrite: false })),
        ];
        for (const [id, texture] of this.textures) {
            const mode = this.textureImages?.get(id)?.alphaMode ?? 0;
            const alphaTest = mode === 1 ? 0.5 : 0;
            const asset: MaterialEffect = this.textureImages?.get(id) ?? {};
            const create = (transparent: boolean) => useMaterial(`texture:${id}:${surfaceKey}:${firstTexel}:${transparent}`, () => {
                const appearance = {...options, map: texture, alphaTest, transparent, depthWrite: !transparent};
                const basic = asset.effectId === 6 || (model.preparedLighting && ![1,2,4,5,7,8,9].includes(asset.effectId ?? 0));
                const material = basic ? new THREE.MeshBasicMaterial({vertexColors: appearance.vertexColors, map: texture, alphaTest, transparent, depthWrite: !transparent}) : new THREE.MeshStandardMaterial(appearance);
                if (!basic) this.applyEffect(material as THREE.MeshStandardMaterial, asset, !!model.preparedLighting);
                if (firstTexel) applyFirstTexelSampling(material, this.textureImages!.get(id)!);
                return material;
            }, texture, this.textureImages?.get(id));
            materials.push(create(mode === 2));
            materials.push(create(true));
        }
        applyTerrainMaterials(geometry, materials, retainedGeometry ? undefined : model.terrainWeights, model.terrainBlends, this.textures, blend => {
            const key = `terrain:${blend.layers.map(layer => `${layer.textureId}:${layer.textureScale}`).join("|")}`;
            const material = useMaterial(key, () => new TerrainMaterial(blend, this.textures));
            material.updateTextures(this.textures);
            return material;
        });
        for (const [key, entry] of this.materials) {
            if (usedMaterials.has(key)) continue;
            entry.material.dispose();
            this.effectUpdates.delete(entry.material);
            this.materials.delete(key);
        }
        this.assignSurfaceGroups(geometry, model);
        this.mesh = new THREE.Mesh(geometry, materials);
        this.mesh.userData["preparedLighting"] = !!model.preparedLighting;
        this.mesh.userData["firstTexel"] = firstTexel;
        this.applyWireframe();
        this.mesh.name = model.sourceId === undefined ? "" : String(model.sourceId);
        this.scene.add(this.mesh);
        this.texturesDirty = false;
        this.fitGrid();
        if (model.particles?.length) this.particles = new ModelParticles(this.scene, model.particles, this.textures);
        this.resetCamera();
    }

    setSceneGeometry(model: ModelGeometry, fitCamera: boolean): void {
        const position = this.camera.position.clone();
        const target = this.controls.target.clone();
        this.setGeometry(model);
        if (fitCamera) {
            this.resetCamera();
            return;
        }
        this.camera.position.copy(position);
        this.controls.target.copy(target);
        this.controls.update();
    }

    resetCamera(): void {
        const bounds = this.mesh?.geometry.boundingSphere;
        if (!bounds) return;
        const radius = Math.max(bounds.radius, 1);
        this.controls.target.copy(bounds.center);
        const halfVerticalFov = THREE.MathUtils.degToRad(this.camera.fov / 2);
        const halfHorizontalFov = Math.atan(Math.tan(halfVerticalFov) * this.camera.aspect);
        const distance = radius / Math.sin(Math.min(halfVerticalFov, halfHorizontalFov)) * 1.1;
        this.camera.position.copy(bounds.center).add(new THREE.Vector3(1.5, 1, 3).normalize().multiplyScalar(distance));
        this.camera.near = Math.max(radius / 1000, 0.01);
        this.camera.far = Math.max(radius * 100, distance * 2);
        this.camera.updateProjectionMatrix();
        this.controls.update();
    }

    orbit(horizontal: number, vertical: number): void {
        const offset = this.camera.position.clone().sub(this.controls.target);
        const spherical = new THREE.Spherical().setFromVector3(offset);
        spherical.theta += horizontal;
        spherical.phi = THREE.MathUtils.clamp(spherical.phi + vertical, 0.05, Math.PI - 0.05);
        this.camera.position.copy(this.controls.target).add(new THREE.Vector3().setFromSpherical(spherical));
        this.controls.update();
    }

    pan(horizontal: number, vertical: number): void {
        const distance = this.camera.position.distanceTo(this.controls.target) * 0.05;
        const offset = new THREE.Vector3(horizontal * distance, vertical * distance, 0).applyQuaternion(this.camera.quaternion);
        this.camera.position.add(offset);
        this.controls.target.add(offset);
        this.controls.update();
    }

    zoom(factor: number): void {
        const offset = this.camera.position.clone().sub(this.controls.target).multiplyScalar(factor);
        this.camera.position.copy(this.controls.target).add(offset);
        this.controls.update();
    }

    dispose(): void {
        if (this.disposed) return;
        this.disposed = true;
        this.renderer.setAnimationLoop(null);
        this.resizeObserver.disconnect();
        this.controls.dispose();
        this.clear();
        if (this.grid) {
            this.scene.remove(this.grid);
            this.grid.geometry.dispose();
            const materials = Array.isArray(this.grid.material) ? this.grid.material : [this.grid.material];
            for (const material of materials) material.dispose();
        }
        for (const texture of this.textures.values()) texture.dispose();
        this.textures.clear();
        this.renderer.dispose();
        this.renderer.domElement.remove();
    }

    private clear(preserveMaterials = false, preserveGeometry = false): void {
        this.particles?.dispose();
        this.particles = undefined;
        if (!preserveMaterials) {
            for (const entry of this.materials.values()) entry.material.dispose();
            this.materials.clear();
            this.effectUpdates.clear();
        }
        if (!this.mesh) return;
        this.scene.remove(this.mesh);
        if (!preserveGeometry) this.mesh.geometry.dispose();
        this.mesh = undefined;
    }

    private applyEffect(material: THREE.MeshStandardMaterial, asset: MaterialEffect, preparedLighting = false): void {
        const id = asset.effectId ?? 0;
        if (![1, 2, 4, 5, 7, 8, 9].includes(id)) return;
        const phase = {value: new THREE.Vector4()};
        const wave = {value: new THREE.Vector3()};
        const lake = {value: new THREE.Vector2()};
        const update = (seconds: number) => {
            const sample = sampleMaterialEffect(asset, seconds);
            phase.value.set(
                id === 4 ? sample.underwater : id === 5 ? sample.lakeNoise : id >= 8 ? sample.hdPhase : sample.animateWater ? sample.secondary : 0,
                id === 5 ? sample.lakeShift : sample.noise,
                id >= 8 ? sample.hdScale : sample.distortion,
                asset.effectParameter ?? 0);
            wave.value.set(sample.waveIntensity[0], sample.waveIntensity[1], sample.waveExponent);
            lake.value.set(sample.lakeScale, sample.lakeXAxis ? 1 : 0);
        };
        update(0);
        this.effectUpdates.set(material, update);
        material.customProgramCacheKey = () => `cache-effect-${id}-${preparedLighting}`;
        material.onBeforeCompile = shader => {
            shader.uniforms.cachePhase = phase;
            shader.uniforms.cacheWave = wave;
            shader.uniforms.cacheLake = lake;
            shader.vertexShader = `varying vec3 cachePosition;\n${shader.vertexShader}`.replace("#include <begin_vertex>", "#include <begin_vertex>\ncachePosition = vec3(position.x, -position.y, position.z);");
            shader.fragmentShader = `uniform vec4 cachePhase;\nuniform vec3 cacheWave;\nuniform vec2 cacheLake;\nvarying vec3 cachePosition;\n${shader.fragmentShader}`;
            if (preparedLighting) shader.fragmentShader = shader.fragmentShader.replace("#include <opaque_fragment>", "outgoingLight = diffuseColor.rgb;\n#include <opaque_fragment>");
            if ([2, 4, 5, 8, 9].includes(id)) {
                // Procedural inspection noise replaces the client's unavailable generated volume texture.
                shader.fragmentShader = shader.fragmentShader.replace("#include <normal_fragment_maps>", `
                    #include <normal_fragment_maps>
                    vec2 cachePoint = ${id === 5 ? "vec2(mix(cachePosition.z, cachePosition.x, cacheLake.y), cachePosition.y) * cacheLake.x" : "cachePosition.xz * " + (id >= 8 ? "cachePhase.z" : "0.025")};
                    normal = normalize(normal + vec3(sin(cachePoint.x + cachePhase.x * 6.2831853), cos(cachePoint.y + cachePhase.y * 6.2831853), 0.0) * ${id >= 8 ? "0.18" : "cachePhase.z"});
                `);
            }
            if ([1, 7, 8, 9].includes(id)) {
                shader.fragmentShader = shader.fragmentShader.replace("#include <opaque_fragment>", `
                    vec3 cacheReflection = inverseTransformDirection(reflect(-normalize(vViewPosition), normal), viewMatrix);
                    vec3 cacheSky = mix(vec3(0.035, 0.055, 0.08), vec3(0.55, 0.72, 0.9), clamp(cacheReflection.y * 0.5 + 0.5, 0.0, 1.0));
                    float cacheFresnel = pow(1.0 - abs(dot(normalize(vViewPosition), normal)), ${id === 8 ? "2.0" : "1.0"});
                    ${id === 9 ? "float cacheWaveShape = pow(0.5 + 0.5 * sin(cachePoint.x + cachePhase.x * 6.2831853), max(cacheWave.z, 1.0)); cacheSky += vec3(cacheWave.x * cacheWaveShape + cacheWave.y);" : ""}
                    outgoingLight = mix(outgoingLight, cacheSky, ${id === 1 ? "clamp(cachePhase.w / 3.0, 0.0, 1.0) * 0.5" : "0.2 + cacheFresnel * 0.6"});
                    #include <opaque_fragment>
                `);
            }
        };
    }

    private resize(): void {
        const width = Math.max(this.host.clientWidth, 1);
        const height = Math.max(this.host.clientHeight, 1);
        this.renderer.setSize(width, height);
        this.camera.aspect = width / height;
        this.camera.updateProjectionMatrix();
    }

    private orderSurfaces(): void {
        if (!this.mesh || !this.currentModel) return;
        this.camera.updateMatrixWorld();
        if (!this.orderingDirty && this.viewMatrix.equals(this.camera.matrixWorldInverse)) return;
        this.viewMatrix.copy(this.camera.matrixWorldInverse);
        this.orderingDirty = false;
        this.orderBlendedSurfaces(this.mesh.geometry, this.currentModel);
    }

    private assignSurfaceGroups(geometry: THREE.BufferGeometry, model: ModelGeometry): void {
        geometry.clearGroups();
        this.sourceIndices = Array.from(model.indices);
        this.opaqueGroups = [];
        this.blendedSurfaces = [];
        const textureIds = [...this.textures.keys()];
        const textureIndices = new Map(textureIds.map((id, index) => [id, index]));
        const opaque = new Map<number, number[]>();
        const sourceIndices = this.sourceIndices;
        const opacities = model.opacities;
        for (let start = 0; start < model.indices.length; start += 3) {
            const face = start / 3;
            const a = sourceIndices[start];
            const b = sourceIndices[start + 1];
            const c = sourceIndices[start + 2];
            const translucent = (opacities?.[a] ?? 1) < 1 || (opacities?.[b] ?? 1) < 1 || (opacities?.[c] ?? 1) < 1;
            const textureId = model.textureIds?.[face] ?? -1;
            const textureIndex = textureIndices.get(textureId) ?? -1;
            const material = terrainSurfaceMaterial(textureId, model.terrainBlends, textureIds.length, (textureIndex < 0 ? 0 : (textureIndex + 1) * 2) + (translucent ? 1 : 0));
            const transparent = translucent || (textureIndex >= 0 && this.textureImages?.get(textureId)?.alphaMode === 2);
            if (transparent) {
                this.blendedSurfaces.push({ sourceStart: start, start: 0, material, depth: 0, priority: model.facePriorities?.[face] ?? 0 });
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
            this.opaqueGroups.push({ start: cursor, count: indices.length, materialIndex });
            index.array.set(indices, cursor);
            cursor += indices.length;
        }
        index.needsUpdate = true;
        // The next frame writes the transparent tail using its final camera transform.
    }

    private orderBlendedSurfaces(geometry: THREE.BufferGeometry, model: ModelGeometry): void {
        // Opaque/cutout draw ranges are camera-independent; never rescan them during an orbit.
        if (!this.blendedSurfaces.length && geometry.groups.length) return;
        geometry.clearGroups();
        for (const group of this.opaqueGroups) geometry.addGroup(group.start, group.count, group.materialIndex);
        for (const face of this.blendedSurfaces) {
            this.centroid.set(0, 0, 0);
            for (let corner = 0; corner < 3; corner++) {
                const vertex = model.indices[face.sourceStart + corner] * 3;
                this.centroid.x += model.positions[vertex] / 3;
                this.centroid.y += model.positions[vertex + 1] / 3;
                this.centroid.z += model.positions[vertex + 2] / 3;
            }
            this.centroid.applyMatrix4(this.viewMatrix);
            face.depth = -this.centroid.z;
        }
        // Groups share one object/depth in Three's stable transparent render list.
        this.blendedSurfaces.sort((a, b) => a.priority - b.priority || b.depth - a.depth || a.sourceStart - b.sourceStart);
        const index = geometry.index!;
        let cursor = this.opaqueGroups.reduce((count, group) => count + group.count, 0);
        let changed = false;
        for (const face of this.blendedSurfaces) {
            face.start = cursor;
            for (let corner = 0; corner < 3; corner++) {
                const vertex = this.sourceIndices[face.sourceStart + corner];
                if (index.getX(cursor) !== vertex) {
                    index.setX(cursor, vertex);
                    changed = true;
                }
                cursor++;
            }
            const previous = geometry.groups.at(-1);
            if (previous && previous.materialIndex === face.material && previous.start + previous.count === face.start)
                previous.count += 3;
            else geometry.addGroup(face.start, 3, face.material);
        }
        if (changed) index.needsUpdate = true;
    }
}

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
