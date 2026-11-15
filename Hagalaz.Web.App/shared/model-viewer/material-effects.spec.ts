import {describe, expect, it} from "vitest";
import {clientFixedWater, clientHdWater, describeMaterialEffect, sampleMaterialEffect} from "./material-effects";

describe("source material effect semantics", () => {
    it("uses signed client scrolling and the 128 second wrap", () => {
        const effect = {scrollU: -32, scrollV: 16};
        expect(sampleMaterialEffect(effect, 1)).toMatchObject({u: -0.5, v: 0.25});
        expect(sampleMaterialEffect(effect, 2)).toMatchObject({u: -0, v: 0.5});
        expect(sampleMaterialEffect(effect, 129)).toMatchObject({u: -0.5, v: 0.25});
        expect(sampleMaterialEffect({}, 7)).toMatchObject({u: 0, v: 0});
    });
    it("decodes ordinary, underwater and lake masks at fixed times", () => {
        expect(sampleMaterialEffect({effectParameter: 3, effectParameter2: 1}, 1)).toMatchObject({noise: 0.2, secondary: 0.25, underwater: 0.25, distortion: 0.1, animateWater: true});
        expect(sampleMaterialEffect({effectParameter: 130, effectParameter2: 1}, 1).animateWater).toBe(false);
        expect(sampleMaterialEffect({effectParameter: 0}, 1)).toMatchObject({lakeShift: -0.5, lakeNoise: 0.5, lakeScale: 1/2048, lakeXAxis: false, underwater: 0});
        expect(sampleMaterialEffect({effectParameter: 203}, 0.125)).toMatchObject({lakeShift: -0.25, lakeNoise: 0.125, lakeScale: 1/1024, lakeXAxis: true});
    });
    it("preserves HD water and wave packed parameters", () => {
        const packed = 1234 | (2 << 16) | (3 << 19) | (5 << 23) | (7 << 27);
        expect(sampleMaterialEffect({effectParameter: 19, effectParameter2: packed}, 2.5)).toMatchObject({hdPhase: 0.5, hdScale: 0.125, breakDepth: 1234, breakOffset: 0.25, waveIntensity: [5/16, 3/16], waveExponent: 7});
    });
    it("labels client effects and unknown fallbacks", () => {
        for (const id of [2,4,5]) expect(describeMaterialEffect({effectId: id})).toContain("client");
        for (const id of [8,9]) expect(describeMaterialEffect({effectId: id})).toContain("Base material without a region cube");
        expect(describeMaterialEffect({effectId: 1})).toContain("client specular highlight");
        expect(describeMaterialEffect({effectId: 7})).toBe("Reflection #7 uses the region environment map. No reflection without one.");
        expect(describeMaterialEffect({effectId:6})).toContain("unlit");
        expect(describeMaterialEffect({effectId:3})).toContain("unresolved");
        expect(describeMaterialEffect({effectId:42})).toContain("unsupported");
        expect(describeMaterialEffect({effectId:0})).toBeNull();
    });
    it.each([
        [2, "waterUv += turbulence", "waterColor.rgb + noise.r"],
        [4, " * 0.25 + cacheWaterScroll", "waterColor.a + rampAlpha"],
        [5, "mix(world.z, world.x, cacheWaterFlags.z)", "waterColor *= vec4(vec3(noise.r), noise.g)"],
    ] as const)("preserves the effect %i coordinate and combine stages", (id, coordinate, combine) => {
        const { vertex, fragment } = clientFixedWater(id);
        expect(vertex).toContain(coordinate);
        expect(fragment).toContain(combine);
        expect(fragment).toContain("texture(cacheWaterNoise, cacheWaterCoordinates).rg");
        expect(fragment + vertex).not.toMatch(/cacheSky|sin\(|cos\(/);
    });
    it.each([8, 9] as const)("preserves the effect %i shader stages", id => {
        const { vertex, fragment } = clientHdWater(id, true);
        expect(vertex).toContain("cacheWaterDepth = waterDepth");
        expect(vertex).toContain("vec3(waterEye.x, -waterEye.y, waterEye.z)");
        expect(fragment).toContain(".rbga");
        expect(fragment).toContain("textureCube(cacheEnvironment, waterReflection)");
        expect(fragment).toContain("fresnel * shoreFactor");
        expect(fragment).toContain("surfaceColor += waterSpecular * shoreFactor");
        expect(fragment).toContain("mix(surfaceColor.rgb, fogColor, cacheWaterFog)");
        expect(fragment).toContain(id === 8 ? "96.0 + verticalSun" : "64.0 + verticalSun");
        if (id === 9) expect(fragment).toContain("mix(surfaceColor, cacheWave.x * waterNormal.aaaa");
        else expect(fragment).not.toContain("waveFactor");
        expect(fragment).not.toMatch(/cacheSky|sin\(|cos\(/);
    });
});
