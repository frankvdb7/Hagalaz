import {describe, expect, it} from "vitest";
import {describeMaterialEffect, sampleMaterialEffect} from "./material-effects";

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
    it("labels supported approximations and unknown fallbacks", () => {
        for (const id of [1,2,4,5,7,8,9]) expect(describeMaterialEffect({effectId: id})).toContain("approximation");
        expect(describeMaterialEffect({effectId:6})).toContain("unlit");
        expect(describeMaterialEffect({effectId:3})).toContain("unresolved");
        expect(describeMaterialEffect({effectId:42})).toContain("unsupported");
        expect(describeMaterialEffect({effectId:0})).toBeNull();
    });
});
