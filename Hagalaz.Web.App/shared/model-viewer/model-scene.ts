import * as THREE from "three";
import { OrbitControls } from "three/addons/controls/OrbitControls.js";
import { sampleMaterialEffect, type MaterialEffect } from "./material-effects";
import {ModelParticles, type ModelParticle} from "./model-particles";

/** Renderer-neutral geometry accepted by both web applications. */
export interface ModelGeometry {
    sourceId?: number;
    positions: readonly number[];
    indices: readonly number[];
    colors?: readonly number[];
    opacities?: readonly number[];
    uvs?: readonly number[];
    textureIds?: readonly number[];
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
    private epoch?: number;
    private lastTime?: number;
    private currentModel?: ModelGeometry;
    private particles?: ModelParticles;
    private readonly effectUpdates: ((seconds: number) => void)[] = [];
    private disposed = false;
    private textureImages?: ReadonlyMap<number, ModelTexture>;
    private readonly textures = new Map<number, THREE.Texture>();

    setTextures(images: ReadonlyMap<number, ModelTexture>): void {
        if (images === this.textureImages) return;
        this.clear();
        for (const texture of this.textures.values()) texture.dispose();
        this.textures.clear();
        this.textureImages = images;
        for (const [id, asset] of images) {
            let texture: THREE.Texture;
            if (asset.hdr) {
                const {width, height, rgba} = asset.hdr;
                // Match the vertically flipped ImageBitmap upload; DataTexture does not flip on upload.
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
            this.textures.set(id, texture);
        }
    }

    constructor(private readonly host: HTMLElement) {
        this.renderer = new THREE.WebGLRenderer({ antialias: true, alpha: true });
        this.renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
        this.renderer.domElement.setAttribute("aria-label", "Interactive 3D model");
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
            for (const update of this.effectUpdates) update(seconds);
            if (this.currentModel) this.particles?.update(this.lastTime === undefined ? 0 : (time-this.lastTime)/1000, this.currentModel,
                this.host.clientHeight * Math.min(window.devicePixelRatio,2) * this.camera.projectionMatrix.elements[5] / 2);
            this.lastTime = time;
            this.controls.update();
            this.renderer.render(this.scene, this.camera);
        });
    }

    setGeometry(model: ModelGeometry): void {
        if (this.disposed) throw new Error("The model scene has been disposed.");
        this.currentModel = model;
        const existing = this.mesh;
        if (existing && model.sourceId !== undefined && existing.name === String(model.sourceId)
            && existing.geometry.index?.count === model.indices.length
            && model.indices.every((index, offset) => existing.geometry.index?.getX(offset) === index)) {
            const positions = existing.geometry.getAttribute("position");
            if (positions.count * 3 === model.positions.length) {
                for (let vertex = 0; vertex < positions.count; vertex++)
                    positions.setXYZ(vertex, model.positions[vertex * 3], model.positions[vertex * 3 + 1], model.positions[vertex * 3 + 2]);
                positions.needsUpdate = true;
                const colors = existing.geometry.getAttribute("color");
                if (colors) {
                    for (let vertex = 0; vertex < colors.count; vertex++) {
                        colors.setXYZ(vertex, model.colors?.[vertex * 3] ?? 1, model.colors?.[vertex * 3 + 1] ?? 1, model.colors?.[vertex * 3 + 2] ?? 1);
                        if (colors.itemSize === 4) colors.setW(vertex, model.opacities?.[vertex] ?? 1);
                    }
                    colors.needsUpdate = true;
                }
                this.assignSurfaceGroups(existing.geometry, model);
                existing.geometry.computeVertexNormals();
                existing.geometry.computeBoundingSphere();
                return;
            }
        }
        this.clear();
        this.epoch = undefined;
        this.lastTime = undefined;
        const geometry = new THREE.BufferGeometry();
        geometry.setAttribute("position", new THREE.Float32BufferAttribute(model.positions, 3));
        geometry.setIndex(Array.from(model.indices));
        if (model.uvs) geometry.setAttribute("uv", new THREE.Float32BufferAttribute(model.uvs, 2));
        if (model.opacities) {
            const rgba = model.opacities.flatMap((opacity, vertex) => [
                model.colors?.[vertex * 3] ?? 1,
                model.colors?.[vertex * 3 + 1] ?? 1,
                model.colors?.[vertex * 3 + 2] ?? 1,
                opacity,
            ]);
            geometry.setAttribute("color", new THREE.Float32BufferAttribute(rgba, 4));
        } else if (model.colors) geometry.setAttribute("color", new THREE.Float32BufferAttribute(model.colors, 3));
        geometry.computeVertexNormals();
        geometry.computeBoundingSphere();
        const options = { vertexColors: !!model.colors || !!model.opacities, roughness: 0.8 };
        const materials: (THREE.MeshStandardMaterial | THREE.MeshBasicMaterial)[] = [
            new THREE.MeshStandardMaterial({ ...options, transparent: false, depthWrite: true }),
            new THREE.MeshStandardMaterial({ ...options, transparent: true, depthWrite: false }),
        ];
        for (const [id, texture] of this.textures) {
            const mode = this.textureImages?.get(id)?.alphaMode ?? 0;
            const alphaTest = mode === 1 ? 0.5 : 0;
            const asset: MaterialEffect = this.textureImages?.get(id) ?? {};
            const create = (transparent: boolean) => {
                const appearance = {...options, map: texture, alphaTest, transparent, depthWrite: !transparent};
                if (asset.effectId === 6) return new THREE.MeshBasicMaterial({vertexColors: appearance.vertexColors, map: texture, alphaTest, transparent, depthWrite: !transparent});
                const material = new THREE.MeshStandardMaterial(appearance);
                this.applyEffect(material, asset);
                return material;
            };
            materials.push(create(mode === 2));
            materials.push(create(true));
        }
        this.assignSurfaceGroups(geometry, model);
        this.mesh = new THREE.Mesh(geometry, materials);
        this.mesh.name = model.sourceId === undefined ? "" : String(model.sourceId);
        this.scene.add(this.mesh);
        if (model.particles?.length) this.particles = new ModelParticles(this.scene, model.particles, this.textures);
        this.resetCamera();
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

    dispose(): void {
        if (this.disposed) return;
        this.disposed = true;
        this.renderer.setAnimationLoop(null);
        this.resizeObserver.disconnect();
        this.controls.dispose();
        this.clear();
        for (const texture of this.textures.values()) texture.dispose();
        this.textures.clear();
        this.renderer.dispose();
        this.renderer.domElement.remove();
    }

    private clear(): void {
        this.particles?.dispose();
        this.particles = undefined;
        this.effectUpdates.length = 0;
        if (!this.mesh) return;
        this.scene.remove(this.mesh);
        this.mesh.geometry.dispose();
        for (const material of this.mesh.material) material.dispose();
        this.mesh = undefined;
    }

    private applyEffect(material: THREE.MeshStandardMaterial, asset: MaterialEffect): void {
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
        this.effectUpdates.push(update);
        material.customProgramCacheKey = () => `cache-effect-${id}`;
        material.onBeforeCompile = shader => {
            shader.uniforms.cachePhase = phase;
            shader.uniforms.cacheWave = wave;
            shader.uniforms.cacheLake = lake;
            shader.vertexShader = `varying vec3 cachePosition;\n${shader.vertexShader}`.replace("#include <begin_vertex>", "#include <begin_vertex>\ncachePosition = vec3(position.x, -position.y, position.z);");
            shader.fragmentShader = `uniform vec4 cachePhase;\nuniform vec3 cacheWave;\nuniform vec2 cacheLake;\nvarying vec3 cachePosition;\n${shader.fragmentShader}`;
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

    private assignSurfaceGroups(geometry: THREE.BufferGeometry, model: ModelGeometry): void {
        geometry.clearGroups();
        let start = 0;
        let previous = -1;
        for (let face = 0; face < model.indices.length; face += 3) {
            const translucent = [0, 1, 2].some(corner => (model.opacities?.[model.indices[face + corner]] ?? 1) < 1);
            const textureIndex = [...this.textures.keys()].indexOf(model.textureIds?.[face / 3] ?? -1);
            const material = (textureIndex < 0 ? 0 : (textureIndex + 1) * 2) + (translucent ? 1 : 0);
            if (material !== previous) {
                if (previous !== -1) geometry.addGroup(start, face - start, previous);
                start = face;
                previous = material;
            }
        }
        if (previous !== -1) geometry.addGroup(start, model.indices.length - start, previous);
    }
}
