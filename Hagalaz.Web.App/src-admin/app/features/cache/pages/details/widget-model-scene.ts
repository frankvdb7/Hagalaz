import * as THREE from "three";
import { prepareCacheModel } from "../../../../../../shared/model-viewer/cache-model-geometry";
import { poseFaceAlpha, poseFaceColors, poseModel } from "../../../../../../shared/model-viewer/model-pose";
import type { GetAnimationFrameDto, GetModelDto, GetTextureMaterialDto } from "../../services/dtos";
import type { WidgetComponent } from "../../services/widget-component";

export type WidgetCamera = NonNullable<WidgetComponent["modelCamera"]>;
export type WidgetTexture = { image: ImageBitmap; material: GetTextureMaterialDto };
export interface WidgetModelAsset {
    model: GetModelDto;
    textures: ReadonlyMap<number, WidgetTexture>;
}

export interface WidgetModelDraw {
    asset: WidgetModelAsset;
    camera?: WidgetCamera;
    width: number;
    height: number;
    pose?: { id: number; frame: GetAnimationFrameDto };
}

export function widgetModelDrawKey(draw: WidgetModelDraw) {
    const c = draw.camera;
    return JSON.stringify([
        draw.asset.model.id,
        draw.width,
        draw.height,
        draw.pose?.id,
        c && [c.flags, c.x, c.y, c.z, c.pitch, c.yaw, c.roll, c.zoom, c.baseWidth, c.baseHeight],
    ]);
}

export function groupWidgetModelDraws(queue: ReadonlyMap<HTMLCanvasElement, WidgetModelDraw>) {
    const groups = new Map<string, { draw: WidgetModelDraw; targets: HTMLCanvasElement[] }>();
    for (const [target, draw] of queue) {
        const key = widgetModelDrawKey(draw);
        const existing = groups.get(key);
        if (existing) existing.targets.push(target);
        else groups.set(key, { draw, targets: [target] });
    }
    return groups.values();
}

export function widgetModelCamera(camera: WidgetCamera, width: number, height: number) {
    const modern = (camera.flags & 3) === 2;
    const angle = (raw: number) => (raw * Math.PI * 2) / (modern ? 16384 : 2048);
    const pitch = angle(camera.pitch);
    const yaw = new THREE.Matrix4().makeRotationY(angle(camera.yaw));
    const roll = new THREE.Matrix4().makeRotationZ(angle(modern ? camera.roll : -camera.roll));
    const tilt = new THREE.Matrix4().makeRotationX(pitch);
    const zoom = modern ? camera.zoom : camera.zoom * 4;
    const translation = modern
        ? new THREE.Matrix4().makeTranslation(camera.x, camera.y, camera.z)
        : new THREE.Matrix4().makeTranslation(0, Math.floor(zoom * Math.sin(pitch)), Math.floor(zoom * Math.cos(pitch)));
    const transform = modern ? translation.multiply(roll).multiply(yaw).multiply(tilt) : tilt.multiply(translation).multiply(yaw).multiply(roll);
    const matrix = new THREE.Matrix4()
        .makeScale(1, -1, -1)
        .multiply(transform)
        .multiply(new THREE.Matrix4().makeScale(1, -1, 1));
    const focalX = camera.baseWidth ? Math.trunc((width * 512) / camera.baseWidth) : 512;
    const focalY = camera.baseHeight ? Math.trunc((height * 512) / camera.baseHeight) : 512;
    const x = Math.trunc(width / 2) + (modern ? 0 : Math.floor((focalX * camera.x) / 512));
    const y = Math.trunc(height / 2) + (modern ? 0 : Math.floor((focalY * camera.y) / 512));
    const ortho = Boolean(camera.flags & 4);
    const near = 50;
    const distance = ortho ? zoom : near;
    const view = new THREE.Camera();
    const planes = [(-x * distance) / focalX, ((width - x) * distance) / focalX, (y * distance) / focalY, (-(height - y) * distance) / focalY] as const;
    if (ortho) view.projectionMatrix.makeOrthographic(...planes, near, 262144);
    else view.projectionMatrix.makePerspective(...planes, near, 262144);
    view.projectionMatrixInverse.copy(view.projectionMatrix).invert();
    return { matrix, view };
}

type Mesh = THREE.Mesh<THREE.BufferGeometry, THREE.MeshBasicMaterial[]>;

export class WidgetModelScene {
    private readonly renderer = new THREE.WebGLRenderer({ alpha: true, antialias: false });
    private readonly scene = new THREE.Scene();
    private readonly meshes = new Map<string, Mesh>();
    private readonly textures = new Map<number, THREE.Texture>();

    draw(batch: ReadonlyMap<HTMLCanvasElement, WidgetModelDraw>) {
        const source = this.renderer.domElement;
        let surfaceWidth = source.width;
        let surfaceHeight = source.height;
        for (const { width, height } of batch.values()) {
            surfaceWidth = Math.max(surfaceWidth, width);
            surfaceHeight = Math.max(surfaceHeight, height);
        }
        if (source.width !== surfaceWidth || source.height !== surfaceHeight) this.renderer.setSize(surfaceWidth, surfaceHeight, false);
        this.renderer.setScissorTest(true);
        for (const { draw, targets } of groupWidgetModelDraws(batch)) {
            const mesh = this.mesh(draw);
            this.scene.add(mesh);
            const { camera, width, height } = draw;
            let view: THREE.Camera;
            if (camera && camera.zoom > 0) {
                const authored = widgetModelCamera(camera, width, height);
                mesh.matrix.copy(authored.matrix);
                view = authored.view;
            } else {
                mesh.matrix.identity();
                const sphere = mesh.geometry.boundingSphere ?? new THREE.Sphere();
                const fitted = new THREE.PerspectiveCamera(40, width / height, 0.01, 262144);
                const fov = Math.min(Math.PI / 9, Math.atan((Math.tan(Math.PI / 9) * width) / height));
                const distance = Math.max(1, sphere.radius / Math.sin(fov));
                fitted.position.copy(sphere.center).addScaledVector(new THREE.Vector3(1, 0.65, -1).normalize(), distance);
                fitted.lookAt(sphere.center);
                view = fitted;
            }
            mesh.matrixWorldNeedsUpdate = true;
            this.renderer.setViewport(0, surfaceHeight - height, width, height);
            this.renderer.setScissor(0, surfaceHeight - height, width, height);
            this.renderer.render(this.scene, view);
            // The 2D layers preserve the widget's paint order around sprites and text.
            for (const target of targets) {
                const context = target.getContext("2d");
                context?.clearRect(0, 0, width, height);
                context?.drawImage(source, 0, 0, width, height, 0, 0, width, height);
            }
            this.scene.remove(mesh);
        }
    }

    private mesh({ asset, pose }: WidgetModelDraw): Mesh {
        const key = `${asset.model.id}:${pose?.id ?? "base"}`;
        const cached = this.meshes.get(key);
        if (cached) {
            this.meshes.delete(key);
            this.meshes.set(key, cached);
            return cached;
        }
        const { model, textures } = asset;
        const posed = pose
            ? {
                  ...model,
                  positions: model.vertexSkins ? poseModel(model.positions, model.vertexSkins, pose.frame, model.coordinateScale) : model.positions,
                  faceAlpha: poseFaceAlpha(model.faceAlpha, model.faceSkins, pose.frame),
                  faceColors: poseFaceColors(model.faceColors, model.faceSkins, pose.frame),
              }
            : model;
        const scale = 1 / (model.coordinateScale || 1);
        const prepared = prepareCacheModel(
            { ...posed, particles: undefined, scale: model.scale?.map((value) => value * scale) ?? [scale, scale, scale], coordinateScale: 1 },
            new Map([...textures].map(([id, texture]) => [id, texture.material]))
        );
        const geometry = new THREE.BufferGeometry();
        geometry.setAttribute("position", new THREE.Float32BufferAttribute(prepared.positions, 3));
        geometry.setAttribute("color", new THREE.Float32BufferAttribute(prepared.colors ?? [], 3));
        geometry.setAttribute("uv", new THREE.Float32BufferAttribute(prepared.uvs ?? [], 2));
        const groups = new Map<string, { indices: number[]; material: THREE.MeshBasicMaterial }>();
        for (let face = 0; face < prepared.indices.length / 3; face++) {
            const opacity = prepared.opacities?.[face * 3] ?? 1;
            if (opacity === 0) continue;
            const textureId = prepared.textureIds?.[face] ?? -1;
            const key = `${textureId}:${opacity}`;
            let group = groups.get(key);
            if (!group) {
                const texture = textures.get(textureId);
                const transparent = opacity < 1 || texture?.material.alphaMode === 2;
                const map = texture && this.texture(textureId, texture);
                const material = new THREE.MeshBasicMaterial({
                    vertexColors: true,
                    map,
                    opacity,
                    transparent,
                    alphaTest: texture?.material.alphaMode === 1 ? 0.5 : 0,
                    depthWrite: !transparent,
                });
                group = { indices: [], material };
                groups.set(key, group);
            }
            const corner = face * 3;
            group.indices.push(prepared.indices[corner], prepared.indices[corner + 1], prepared.indices[corner + 2]);
        }
        const indices: number[] = [];
        const materials: THREE.MeshBasicMaterial[] = [];
        for (const group of groups.values()) {
            geometry.addGroup(indices.length, group.indices.length, materials.length);
            for (const index of group.indices) indices.push(index);
            materials.push(group.material);
        }
        geometry.setIndex(indices);
        geometry.computeBoundingSphere();
        const mesh = new THREE.Mesh(geometry, materials);
        mesh.matrixAutoUpdate = false;
        mesh.frustumCulled = false;
        this.meshes.set(key, mesh);
        if (this.meshes.size > 64) {
            const oldest = this.meshes.entries().next();
            if (!oldest.done) {
                const [key, mesh] = oldest.value;
                this.release(mesh);
                this.meshes.delete(key);
            }
        }
        return mesh;
    }

    private texture(id: number, { image }: WidgetTexture) {
        let texture = this.textures.get(id);
        if (!texture) {
            texture = new THREE.Texture(image);
            texture.colorSpace = THREE.SRGBColorSpace;
            texture.wrapS = texture.wrapT = THREE.RepeatWrapping;
            texture.needsUpdate = true;
            this.textures.set(id, texture);
        }
        return texture;
    }

    private release(mesh: Mesh) {
        mesh.geometry.dispose();
        for (const material of mesh.material) material.dispose();
    }

    dispose() {
        for (const mesh of this.meshes.values()) this.release(mesh);
        for (const texture of this.textures.values()) texture.dispose();
        this.renderer.dispose();
        this.renderer.forceContextLoss();
    }
}
