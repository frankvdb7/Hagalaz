import * as THREE from "three";
import { clientReflection } from "./material-effects";

export interface TerrainLayer {
    textureId: number;
    textureScale: number;
    color: readonly number[];
    /** Effect 7: reflects the region environment map, masked by texture alpha. */
    reflective?: boolean;
}
export interface TerrainBlend { id: number; layers: readonly TerrainLayer[]; }
/** Uniforms owned by ModelScene and shared with every reflective program. */
export interface TerrainEnvironment { map: { value: THREE.CubeTexture | null }; enabled: { value: boolean }; }

/** Opaque floor splatting. Texture ownership stays with ModelScene. */
export function terrainMaterial(blend: TerrainBlend, textures: ReadonlyMap<number, THREE.Texture>): THREE.MeshBasicMaterial {
    return new TerrainMaterial(blend, textures);
}

/** Retains one program per layer count and reflective layout while image samplers change independently. */
export class TerrainMaterial extends THREE.MeshBasicMaterial {
    private readonly maps: { id: number; texture: { value: THREE.Texture | null }; enabled: { value: boolean } }[];

    constructor(blend: TerrainBlend, textures: ReadonlyMap<number, THREE.Texture>, environment: TerrainEnvironment = { map: { value: null }, enabled: { value: false } }) {
        super({ vertexColors: true });
        this.maps = blend.layers.map(layer => ({ id: layer.textureId, texture: { value: null }, enabled: { value: false } }));
        this.updateTextures(textures);
        this.customProgramCacheKey = () => `terrain-spatial-uniforms-${blend.layers.length}-${blend.layers.map(layer => (layer.reflective ? 1 : 0)).join("")}`;
        this.onBeforeCompile = shader => {
            shader.vertexShader = `attribute vec4 terrainWeightsA;\nattribute vec4 terrainWeightsB;\nvarying vec4 floorWeightsA;\nvarying vec4 floorWeightsB;\nvarying vec2 floorUv;\n${shader.vertexShader}`;
            shader.vertexShader = shader.vertexShader.replace("#include <begin_vertex>", "#include <begin_vertex>\nfloorWeightsA = terrainWeightsA; floorWeightsB = terrainWeightsB; floorUv = uv;");
            const declarations: string[] = [], samples: string[] = [], reflections: string[] = [];
            blend.layers.forEach((layer, index) => {
                shader.uniforms[`floorScale${index}`] = { value: layer.textureScale };
                shader.uniforms[`floorMap${index}`] = this.maps[index].texture;
                shader.uniforms[`floorMapEnabled${index}`] = this.maps[index].enabled;
                declarations.push(`uniform float floorScale${index}; uniform sampler2D floorMap${index}; uniform bool floorMapEnabled${index};`);
                const sample = `(floorMapEnabled${index} ? texture2D(floorMap${index}, floorUv / floorScale${index}).rgb : vec3(1.0))`;
                const weight = `floorWeights${index < 4 ? "A" : "B"}[${index % 4}]`;
                samples.push(`${sample} * ${weight}`);
                if (layer.reflective) reflections.push(`(floorMapEnabled${index} ? texture2D(floorMap${index}, floorUv / floorScale${index}).a : 0.0) * ${weight}`);
            });
            shader.fragmentShader = `varying vec4 floorWeightsA;\nvarying vec4 floorWeightsB;\nvarying vec2 floorUv;\n${declarations.join("\n")}\n${shader.fragmentShader}`;
            shader.fragmentShader = shader.fragmentShader.replace("#include <map_fragment>", `diffuseColor.rgb *= ${samples.join(" + ")};`);
            if (!reflections.length) return;
            shader.uniforms.cacheEnvironment = environment.map;
            shader.uniforms.cacheEnvironmentEnabled = environment.enabled;
            shader.vertexShader = `varying vec3 floorNormal;\nvarying vec3 floorViewPosition;\n${shader.vertexShader}`.replace("#include <project_vertex>", "#include <project_vertex>\nfloorNormal = normalMatrix * normal; floorViewPosition = -mvPosition.xyz;");
            shader.fragmentShader = `uniform samplerCube cacheEnvironment;\nuniform bool cacheEnvironmentEnabled;\nvarying vec3 floorNormal;\nvarying vec3 floorViewPosition;\n${shader.fragmentShader}`.replace("#include <opaque_fragment>", `${clientReflection(reflections.join(" + "), "normalize(floorNormal)", "floorViewPosition")}\n#include <opaque_fragment>`);
        };
    }

    updateTextures(textures: ReadonlyMap<number, THREE.Texture>): void {
        for (const map of this.maps) {
            map.texture.value = textures.get(map.id) ?? null;
            map.enabled.value = map.texture.value !== null;
        }
    }
}

export function applyTerrainMaterials(geometry: THREE.BufferGeometry, materials: THREE.Material[], weights: readonly number[] | undefined, blends: readonly TerrainBlend[] | undefined, textures: ReadonlyMap<number, THREE.Texture>, resolve = (blend: TerrainBlend) => terrainMaterial(blend, textures)): void {
    if (weights && blends?.length) {
        const buffer = new THREE.InterleavedBuffer(new Float32Array(weights), 8);
        geometry.setAttribute("terrainWeightsA", new THREE.InterleavedBufferAttribute(buffer, 4, 0));
        geometry.setAttribute("terrainWeightsB", new THREE.InterleavedBufferAttribute(buffer, 4, 4));
    }
    for (const blend of blends ?? []) materials.push(resolve(blend));
}

export function terrainSurfaceMaterial(textureId: number, blends: readonly TerrainBlend[] | undefined, textureCount: number, ordinaryMaterial: number): number {
    const index = blends?.findIndex(blend => blend.id === textureId) ?? -1;
    return index >= 0 ? 2 + textureCount * 2 + index : ordinaryMaterial;
}
