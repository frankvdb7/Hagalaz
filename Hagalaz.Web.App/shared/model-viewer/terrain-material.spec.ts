import { describe, expect, it } from "vitest";
import * as THREE from "three";
import { terrainMaterial, TerrainMaterial, terrainSurfaceMaterial, applyTerrainMaterials, textureAverage, type TerrainBlend, type TerrainEnvironment } from "./terrain-material";

const blend: TerrainBlend = { id: -2, layers: [
    { textureId: 10, textureScale: 512, color: [0, 1, 0] },
    { textureId: 20, textureScale: 1024, color: [1, 0, 0] },
] };

describe("opaque terrain texture blending", () => {
    it("reflects the shared environment by the weight and alpha of reflective layers only", () => {
        const compile = (target: TerrainBlend, environment?: TerrainEnvironment) => {
            const material = new TerrainMaterial(target, new Map(), environment);
            const shader = { uniforms: {} as Record<string, { value: unknown }>, vertexShader: THREE.ShaderLib.basic.vertexShader, fragmentShader: THREE.ShaderLib.basic.fragmentShader };
            material.onBeforeCompile(shader as THREE.WebGLProgramParametersWithUniforms, {} as THREE.WebGLRenderer);
            return { material, shader };
        };
        const environment: TerrainEnvironment = { map: { value: null }, enabled: { value: true } };
        const reflective = compile({ ...blend, layers: [blend.layers[0], { ...blend.layers[1], reflective: true }] }, environment);
        expect(reflective.shader.uniforms.cacheEnvironment).toBe(environment.map);
        expect(reflective.shader.uniforms.cacheEnvironmentEnabled).toBe(environment.enabled);
        expect(reflective.shader.vertexShader).toContain("floorNormal = normalMatrix * normal; floorViewPosition = -mvPosition.xyz;");
        expect(reflective.shader.fragmentShader).toContain("mix(cacheBaseGamma, cacheCubeGamma, (floorMapEnabled1 ? texture2D(floorMap1, floorUv / floorScale1).a : 0.0) * floorWeightsA[1])");
        expect(reflective.shader.fragmentShader).not.toContain("texture2D(floorMap0, floorUv / floorScale0).a");
        const plain = compile(blend);
        expect(plain.shader.fragmentShader).not.toContain("cacheEnvironment");
        expect(plain.shader.vertexShader).not.toContain("floorNormal");
        expect(reflective.material.customProgramCacheKey()).not.toBe(plain.material.customProgramCacheKey());
        reflective.material.dispose(); plain.material.dispose();
    });
    it("samples each texture with its scale and interpolated weights in one opaque draw", () => {
        const texture = new THREE.Texture();
        const material = terrainMaterial(blend, new Map([[10, texture]]));
        const shader = { uniforms: {} as Record<string, {value: unknown}>, vertexShader: THREE.ShaderLib.basic.vertexShader, fragmentShader: THREE.ShaderLib.basic.fragmentShader };
        material.onBeforeCompile(shader as THREE.WebGLProgramParametersWithUniforms, {} as THREE.WebGLRenderer);
        expect(shader.vertexShader).toContain("floorWeightsA = terrainWeightsA");
        expect(shader.fragmentShader).toContain("floorSample(floorMapEnabled0, floorMap0, floorScale0, floorAverage0, floorDx, floorDy) * floorWeightsA[0]");
        expect(shader.fragmentShader).toContain("floorSample(floorMapEnabled1, floorMap1, floorScale1, floorAverage1, floorDx, floorDy) * floorWeightsA[1]");
        expect(material.vertexColors).toBe(true);
        expect(shader.fragmentShader).toContain("#include <color_fragment>");
        expect(shader.fragmentShader).not.toContain("floorColor");
        expect(shader.uniforms["floorMap0"].value).toBe(texture);
        expect(shader.uniforms["floorScale1"].value).toBe(1024);
        expect(material.transparent).toBe(false);
        expect(material.depthWrite).toBe(true);
        const differentImages = terrainMaterial(blend, new Map([[20, texture]]));
        expect(differentImages.customProgramCacheKey()).toBe(material.customProgramCacheKey());
        const eight = terrainMaterial({ id: -3, layers: Array(8).fill(blend.layers[0]) }, new Map());
        const fullShader = { uniforms: {}, vertexShader: THREE.ShaderLib.basic.vertexShader, fragmentShader: THREE.ShaderLib.basic.fragmentShader };
        eight.onBeforeCompile(fullShader as THREE.WebGLProgramParametersWithUniforms, {} as THREE.WebGLRenderer);
        expect(fullShader.fragmentShader).toContain("floorAverage7, floorDx, floorDy) * floorWeightsB[3]");
        expect(eight.customProgramCacheKey()).not.toBe(material.customProgramCacheKey());
        eight.dispose();
        differentImages.dispose(); material.dispose(); texture.dispose();
    });
    it("uses each layer's average colour instead of a fetch once a pixel spans a whole texture repeat", () => {
        // Two sRGB pixels, white and black: their linear mean is 0.5, not the 0.5 encoded value.
        const bytes = new THREE.Texture({ data: Uint8ClampedArray.of(255, 255, 255, 255, 0, 0, 0, 255), width: 2, height: 1 });
        bytes.colorSpace = THREE.SRGBColorSpace;
        expect(textureAverage(bytes).toArray()).toEqual([0.5, 0.5, 0.5]);
        const floats = new THREE.Texture({ data: Float32Array.of(0.2, 0.4, 0.6, 1), width: 1, height: 1 });
        expect(textureAverage(floats).toArray().map(value => Math.round(value * 100) / 100)).toEqual([0.2, 0.4, 0.6]);
        expect(textureAverage(new THREE.Texture()).toArray()).toEqual([1, 1, 1]);

        const material = terrainMaterial(blend, new Map([[10, bytes]]));
        const shader = { uniforms: {} as Record<string, { value: unknown }>, vertexShader: THREE.ShaderLib.basic.vertexShader, fragmentShader: THREE.ShaderLib.basic.fragmentShader };
        material.onBeforeCompile(shader as THREE.WebGLProgramParametersWithUniforms, {} as THREE.WebGLRenderer);
        expect((shader.uniforms["floorAverage0"].value as THREE.Vector3).toArray()).toEqual([0.5, 0.5, 0.5]);
        expect(shader.fragmentShader).toContain("float lod = smoothstep(0.5, 1.0, max(length(dx), length(dy)) / scale);");
        expect(shader.fragmentShader).toContain("if (lod >= 1.0) return average;");
        expect(shader.fragmentShader).toContain("textureGrad(map, floorUv / scale, dx / scale, dy / scale)");
        expect(shader.fragmentShader).toContain("vec2 floorDx = dFdx(floorUv), floorDy = dFdy(floorUv);");
        material.dispose();
    });
    it("updates compiled sampler uniforms without changing terrain shader identity", () => {
        const material = new TerrainMaterial(blend, new Map());
        const shader = { uniforms: {} as Record<string, { value: unknown }>, vertexShader: THREE.ShaderLib.basic.vertexShader, fragmentShader: THREE.ShaderLib.basic.fragmentShader };
        material.onBeforeCompile(shader as THREE.WebGLProgramParametersWithUniforms, {} as THREE.WebGLRenderer);
        const key = material.customProgramCacheKey(), source = shader.fragmentShader;
        const grass = new THREE.Texture(), path = new THREE.Texture();
        expect(shader.uniforms.floorMapEnabled0.value).toBe(false);
        material.updateTextures(new Map([[10, grass]]));
        expect(shader.uniforms.floorMap0.value).toBe(grass);
        expect(shader.uniforms.floorMapEnabled0.value).toBe(true);
        expect(shader.uniforms.floorMapEnabled1.value).toBe(false);
        material.updateTextures(new Map([[20, path]]));
        expect(shader.uniforms.floorMap0.value).toBeNull();
        expect(shader.uniforms.floorMapEnabled0.value).toBe(false);
        expect(shader.uniforms.floorMap1.value).toBe(path);
        expect(shader.uniforms.floorMapEnabled1.value).toBe(true);
        expect(material.customProgramCacheKey()).toBe(key);
        const other = new TerrainMaterial(blend, new Map([[10, grass], [20, path]]));
        const otherShader = { uniforms: {}, vertexShader: THREE.ShaderLib.basic.vertexShader, fragmentShader: THREE.ShaderLib.basic.fragmentShader };
        other.onBeforeCompile(otherShader as THREE.WebGLProgramParametersWithUniforms, {} as THREE.WebGLRenderer);
        expect(otherShader.fragmentShader).toBe(source);
        material.dispose(); other.dispose(); grass.dispose(); path.dispose();
    });
    it("keeps terrain and model materials separate with shared texture ownership", () => {
        const geometry = new THREE.BufferGeometry(), materials: THREE.Material[] = [];
        applyTerrainMaterials(geometry, materials, [1, 0, 0, 0, 0, 0, 0, 0], [blend], new Map());
        expect(geometry.getAttribute("terrainWeightsA").getX(0)).toBe(1);
        expect(geometry.getAttribute("terrainWeightsB").getX(0)).toBe(0);
        expect(materials).toHaveLength(1);
        expect(terrainSurfaceMaterial(-2, [blend], 3, 0)).toBe(8);
        expect(terrainSurfaceMaterial(10, [blend], 3, 2)).toBe(2);
        expect(terrainSurfaceMaterial(-1, undefined, 0, 0)).toBe(0);
        geometry.dispose(); for (const material of materials) material.dispose();
    });
});
