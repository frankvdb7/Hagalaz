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
    private readonly maps: { id: number; texture: { value: THREE.Texture | null }; enabled: { value: boolean }; average: { value: THREE.Vector3 } }[];

    constructor(blend: TerrainBlend, textures: ReadonlyMap<number, THREE.Texture>, environment: TerrainEnvironment = { map: { value: null }, enabled: { value: false } }) {
        super({ vertexColors: true });
        this.maps = blend.layers.map(layer => ({ id: layer.textureId, texture: { value: null }, enabled: { value: false }, average: { value: new THREE.Vector3(1, 1, 1) } }));
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
                shader.uniforms[`floorAverage${index}`] = this.maps[index].average;
                declarations.push(`uniform float floorScale${index}; uniform sampler2D floorMap${index}; uniform bool floorMapEnabled${index}; uniform vec3 floorAverage${index};`);
                const sample = `floorSample(floorMapEnabled${index}, floorMap${index}, floorScale${index}, floorAverage${index}, floorDx, floorDy)`;
                const weight = `floorWeights${index < 4 ? "A" : "B"}[${index % 4}]`;
                samples.push(`${sample} * ${weight}`);
                if (layer.reflective) reflections.push(`(floorMapEnabled${index} ? texture2D(floorMap${index}, floorUv / floorScale${index}).a : 0.0) * ${weight}`);
            });
            shader.fragmentShader = `varying vec4 floorWeightsA;\nvarying vec4 floorWeightsB;\nvarying vec2 floorUv;\n${declarations.join("\n")}\n${floorSampleSource}\n${shader.fragmentShader}`;
            shader.fragmentShader = shader.fragmentShader.replace("#include <map_fragment>", `vec2 floorDx = dFdx(floorUv), floorDy = dFdy(floorUv);\ndiffuseColor.rgb *= ${samples.join(" + ")};`);
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
            if (map.texture.value) map.average.value.copy(textureAverage(map.texture.value));
        }
    }
}

/**
 * Texture LOD: once a pixel spans a whole repeat of a floor texture, its mipmap has converged to the texture's average
 * colour, so the layer uses that precomputed colour instead of a fetch. It fades in from half a repeat per pixel.
 * Explicit gradients keep the fetch well defined inside the per-pixel branch.
 */
const floorSampleSource = /* glsl */ `
    vec3 floorSample(bool enabled, sampler2D map, float scale, vec3 average, vec2 dx, vec2 dy) {
        if (!enabled) return vec3(1.0);
        float lod = smoothstep(0.5, 1.0, max(length(dx), length(dy)) / scale);
        if (lod >= 1.0) return average;
        return mix(textureGrad(map, floorUv / scale, dx / scale, dy / scale).rgb, average, lod);
    }
`;

const averages = new WeakMap<object, THREE.Vector3>();

/** A texture's mean colour in linear space, which is what its smallest mipmap holds. White when the image cannot be read. */
export function textureAverage(texture: THREE.Texture): THREE.Vector3 {
    const image = texture.image as (CanvasImageSource & { width: number; height: number }) | { data?: ArrayLike<number>; width: number; height: number } | undefined;
    if (!image) return new THREE.Vector3(1, 1, 1);
    const cached = averages.get(image);
    if (cached) return cached;
    const average = readAverage(image, texture.colorSpace === THREE.SRGBColorSpace);
    averages.set(image, average);
    return average;
}

function readAverage(image: object & { width: number; height: number }, srgb: boolean): THREE.Vector3 {
    const raw = (image as { data?: ArrayLike<number> }).data;
    const data: ArrayLike<number> | undefined = raw ?? bitmapPixels(image as CanvasImageSource & { width: number; height: number });
    if (!data) return new THREE.Vector3(1, 1, 1);
    // Byte pixels are sRGB-encoded; float pixels are already linear.
    const decode = (value: number) => data instanceof Uint8ClampedArray ? srgbToLinear(value / 255, srgb) : value;
    const sum = new THREE.Vector3();
    for (let pixel = 0; pixel < data.length; pixel += 4) sum.add(new THREE.Vector3(decode(data[pixel]), decode(data[pixel + 1]), decode(data[pixel + 2])));
    return sum.divideScalar(Math.max(data.length / 4, 1));
}

function bitmapPixels(image: CanvasImageSource & { width: number; height: number }): Uint8ClampedArray | undefined {
    if (typeof OffscreenCanvas === "undefined" || !image.width || !image.height) return undefined;
    const context = new OffscreenCanvas(image.width, image.height).getContext("2d");
    if (!context) return undefined;
    context.drawImage(image, 0, 0);
    return context.getImageData(0, 0, image.width, image.height).data;
}

function srgbToLinear(value: number, srgb: boolean): number {
    if (!srgb) return value;
    return value <= 0.04045 ? value / 12.92 : ((value + 0.055) / 1.055) ** 2.4;
}

export function applyTerrainMaterials(geometry: THREE.BufferGeometry, materials: THREE.Material[], weights: ArrayLike<number> | undefined, blends: readonly TerrainBlend[] | undefined, textures: ReadonlyMap<number, THREE.Texture>, resolve = (blend: TerrainBlend) => terrainMaterial(blend, textures)): void {
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
