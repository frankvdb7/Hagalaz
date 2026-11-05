import * as THREE from "three";

export interface TerrainLayer {
    textureId: number;
    textureScale: number;
    color: readonly number[];
}
export interface TerrainBlend { id: number; layers: readonly TerrainLayer[]; }

/** Opaque floor splatting. Texture ownership stays with ModelScene. */
export function terrainMaterial(blend: TerrainBlend, textures: ReadonlyMap<number, THREE.Texture>): THREE.MeshBasicMaterial {
    return new TerrainMaterial(blend, textures);
}

/** Retains one program per layer count while image samplers change independently. */
export class TerrainMaterial extends THREE.MeshBasicMaterial {
    private readonly maps: { id: number; texture: { value: THREE.Texture | null }; enabled: { value: boolean } }[];

    constructor(blend: TerrainBlend, textures: ReadonlyMap<number, THREE.Texture>) {
        super({ vertexColors: true });
        this.maps = blend.layers.map(layer => ({ id: layer.textureId, texture: { value: null }, enabled: { value: false } }));
        this.updateTextures(textures);
        this.customProgramCacheKey = () => `terrain-spatial-uniforms-${blend.layers.length}`;
        this.onBeforeCompile = shader => {
            shader.vertexShader = `attribute vec4 terrainWeightsA;\nattribute vec4 terrainWeightsB;\nvarying vec4 floorWeightsA;\nvarying vec4 floorWeightsB;\nvarying vec2 floorUv;\n${shader.vertexShader}`;
            shader.vertexShader = shader.vertexShader.replace("#include <begin_vertex>", "#include <begin_vertex>\nfloorWeightsA = terrainWeightsA; floorWeightsB = terrainWeightsB; floorUv = uv;");
            const declarations: string[] = [], samples: string[] = [];
            blend.layers.forEach((layer, index) => {
                shader.uniforms[`floorScale${index}`] = { value: layer.textureScale };
                shader.uniforms[`floorMap${index}`] = this.maps[index].texture;
                shader.uniforms[`floorMapEnabled${index}`] = this.maps[index].enabled;
                declarations.push(`uniform float floorScale${index}; uniform sampler2D floorMap${index}; uniform bool floorMapEnabled${index};`);
                const sample = `(floorMapEnabled${index} ? texture2D(floorMap${index}, floorUv / floorScale${index}).rgb : vec3(1.0))`;
                const weight = `floorWeights${index < 4 ? "A" : "B"}[${index % 4}]`;
                samples.push(`${sample} * ${weight}`);
            });
            shader.fragmentShader = `varying vec4 floorWeightsA;\nvarying vec4 floorWeightsB;\nvarying vec2 floorUv;\n${declarations.join("\n")}\n${shader.fragmentShader}`;
            shader.fragmentShader = shader.fragmentShader.replace("#include <map_fragment>", `diffuseColor.rgb *= ${samples.join(" + ")};`);
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
    if (weights) {
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
