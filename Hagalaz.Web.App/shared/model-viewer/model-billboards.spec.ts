import * as THREE from "three";
import { expect, it, vi } from "vitest";
import { prepareCacheModel } from "./cache-model-geometry";
import { ModelBillboards } from "./model-billboards";

it("replaces the source face with a posed quad and releases the quad on removal", () => {
    const scene = new THREE.Scene();
    const camera = new THREE.PerspectiveCamera();
    camera.position.z = 100;
    const texture = new THREE.Texture();
    const prepared = prepareCacheModel({
        positions: [0, 0, 0, 6, 0, 0, 0, -6, 0],
        indices: [0, 1, 2],
        faceColors: [0x407f],
        faceAlpha: [55],
        billboards: [{
            face: 0, depthOffset: 0,
            definition: { textureId: 7, halfWidth: 12, halfHeight: 10, blendMode: 1, hideFace: true },
            pose: { x: 3, y: 4, angle: 4096, scaleX: 64, scaleY: 192 },
        }],
    });
    expect(prepared.opacities).toEqual([0, 0, 0]);
    const renderer = new ModelBillboards(scene, new Map([[7, texture]]));
    renderer.reset(prepared.billboards ?? []);
    renderer.update(new THREE.Float32BufferAttribute(prepared.positions, 3), camera);
    const quad = scene.children[0] as THREE.Mesh<THREE.BufferGeometry, THREE.MeshBasicMaterial>;
    expect(quad.position.toArray()).toEqual([5, -2, 0]);
    expect(quad.rotation.z).toBeCloseTo(-Math.PI / 2);
    expect(quad.scale.toArray()).toEqual([6, 15, 1]);
    expect(quad.material.map).toBe(texture);
    expect(quad.material.opacity).toBeCloseTo(200 / 255);
    const release = vi.spyOn(quad.material, "dispose");
    renderer.dispose();
    expect(scene.children).toHaveLength(0);
    expect(release).toHaveBeenCalledOnce();
    texture.dispose();
});
