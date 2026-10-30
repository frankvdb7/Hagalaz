import * as THREE from "three";
import { OrbitControls } from "three/addons/controls/OrbitControls.js";

/** Renderer-neutral geometry accepted by both web applications. */
export interface ModelGeometry {
    sourceId?: number;
    positions: readonly number[];
    indices: readonly number[];
    colors?: readonly number[];
    opacities?: readonly number[];
    uvs?: readonly number[];
    textureIds?: readonly number[];
}

export interface ModelTexture { image: ImageBitmap; alphaMode: 0 | 1 | 2; colorBlend?: number; brightness?: number; }

/** Owns the canvas and GPU resources for a single model inspection surface. */
export class ModelScene {
    private readonly renderer: THREE.WebGLRenderer;
    private readonly scene = new THREE.Scene();
    private readonly camera = new THREE.PerspectiveCamera(40, 1, 0.01, 100000);
    private readonly controls: OrbitControls;
    private readonly resizeObserver: ResizeObserver;
    private mesh?: THREE.Mesh<THREE.BufferGeometry, THREE.MeshStandardMaterial[]>;
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
            const texture = new THREE.Texture(asset.image);
            texture.colorSpace = THREE.SRGBColorSpace;
            texture.wrapS = texture.wrapT = THREE.RepeatWrapping;
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
        this.renderer.setAnimationLoop(() => {
            this.controls.update();
            this.renderer.render(this.scene, this.camera);
        });
    }

    setGeometry(model: ModelGeometry): void {
        if (this.disposed) throw new Error("The model scene has been disposed.");
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
        const materials = [
            new THREE.MeshStandardMaterial({ ...options, transparent: false, depthWrite: true }),
            new THREE.MeshStandardMaterial({ ...options, transparent: true, depthWrite: false }),
        ];
        for (const [id, texture] of this.textures) {
            const mode = this.textureImages?.get(id)?.alphaMode ?? 0;
            const alphaTest = mode === 1 ? 0.5 : 0;
            materials.push(new THREE.MeshStandardMaterial({ ...options, map: texture, alphaTest, transparent: mode === 2, depthWrite: mode !== 2 }));
            materials.push(new THREE.MeshStandardMaterial({ ...options, map: texture, alphaTest, transparent: true, depthWrite: false }));
        }
        this.assignSurfaceGroups(geometry, model);
        this.mesh = new THREE.Mesh(geometry, materials);
        this.mesh.name = model.sourceId === undefined ? "" : String(model.sourceId);
        this.scene.add(this.mesh);
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
        if (!this.mesh) return;
        this.scene.remove(this.mesh);
        this.mesh.geometry.dispose();
        for (const material of this.mesh.material) material.dispose();
        this.mesh = undefined;
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
