import * as THREE from "three";
import type { BillboardPose } from "./model-pose";

export interface ModelBillboard {
    face: number;
    depthOffset: number;
    definition: {
        textureId: number;
        halfWidth: number;
        halfHeight: number;
        blendMode: number;
        hideFace: boolean;
    } | null;
    pose?: BillboardPose;
}

export interface RenderBillboard {
    face: number;
    textureId: number;
    halfWidth: number;
    halfHeight: number;
    blendMode: number;
    depthOffset: number;
    x: number;
    y: number;
    angle: number;
    rgb: number;
    opacity: number;
}

const opaqueBlend = { alphaTest: 0, transparent: false, blending: THREE.NoBlending, blendSrc: THREE.OneFactor };
const billboardBlendModes = new Map([
    [1, { ...opaqueBlend, alphaTest: Number.EPSILON, transparent: true, blending: THREE.NormalBlending }],
    [2, { ...opaqueBlend, transparent: true, blending: THREE.CustomBlending }],
    [128, { ...opaqueBlend, alphaTest: Number.EPSILON, transparent: true, blending: THREE.CustomBlending, blendSrc: THREE.DstColorFactor }],
]);

/** One camera-facing quad per decoded attachment, sharing the scene's textures. */
export class ModelBillboards {
    private readonly geometry = new THREE.PlaneGeometry(2, 2);
    private readonly quads: THREE.Mesh<THREE.PlaneGeometry, THREE.MeshBasicMaterial>[] = [];
    private readonly states: RenderBillboard[] = [];
    private readonly direction = new THREE.Vector3();
    private readonly offset = new THREE.Vector3();

    constructor(private readonly scene: THREE.Scene, private readonly textures: ReadonlyMap<number, THREE.Texture>) {}

    reset(states: readonly RenderBillboard[]): void {
        while (this.quads.length > states.length) {
            const quad = this.quads.pop()!;
            quad.removeFromParent();
            quad.material.dispose();
        }
        this.states.length = states.length;
        this.write(0, states);
        while (this.quads.length < states.length) {
            const quad = new THREE.Mesh(this.geometry, new THREE.MeshBasicMaterial({ depthWrite: false, side: THREE.DoubleSide }));
            this.quads.push(quad);
            this.scene.add(quad);
        }
    }

    write(start: number, states: readonly RenderBillboard[]): void {
        for (const [index, state] of states.entries()) this.states[start + index] = state;
    }

    update(positions: THREE.BufferAttribute, camera: THREE.PerspectiveCamera): void {
        for (const [index, state] of this.states.entries()) {
            const quad = this.quads[index];
            quad.visible = this.updateMaterial(quad.material, state);
            if (!quad.visible) continue;
            const vertex = state.face * 3;
            quad.position.set(
                (positions.getX(vertex) + positions.getX(vertex + 1) + positions.getX(vertex + 2)) / 3,
                (positions.getY(vertex) + positions.getY(vertex + 1) + positions.getY(vertex + 2)) / 3,
                (positions.getZ(vertex) + positions.getZ(vertex + 1) + positions.getZ(vertex + 2)) / 3,
            );
            this.direction.subVectors(camera.position, quad.position).normalize();
            quad.position.addScaledVector(this.direction, state.depthOffset);
            this.offset.set(state.x, -state.y, 0).applyQuaternion(camera.quaternion);
            quad.position.add(this.offset);
            quad.quaternion.copy(camera.quaternion);
            quad.rotateZ(-state.angle * Math.PI / 8192);
            quad.scale.set(state.halfWidth, state.halfHeight, 1);
        }
    }

    private updateMaterial(material: THREE.MeshBasicMaterial, state: RenderBillboard): boolean {
        const map = this.textures.get(state.textureId) ?? null;
        if (state.textureId >= 0 && !map) return false;
        const { alphaTest, transparent, blending, blendSrc } = billboardBlendModes.get(state.blendMode) ?? opaqueBlend;
        if (material.map !== map || material.transparent !== transparent) {
            material.map = map;
            material.transparent = transparent;
            material.needsUpdate = true;
        }
        material.alphaTest = alphaTest;
        material.color.setHex(state.rgb);
        material.opacity = state.opacity;
        material.blending = blending;
        material.blendSrc = blendSrc;
        material.blendDst = THREE.OneFactor;
        return true;
    }

    dispose(): void {
        this.reset([]);
        this.geometry.dispose();
    }
}
