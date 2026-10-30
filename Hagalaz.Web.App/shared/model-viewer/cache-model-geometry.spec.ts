import { describe, expect, it } from "vitest";
import { prepareCacheModel } from "./cache-model-geometry";

describe("prepareCacheModel", () => {
    it("preserves server-projected corner UVs while reversing face winding", () => {
        const model = { id: 1, positions: [0, 0, 0, 10, 0, 0, 0, 10, 0], indices: [0, 1, 2], faceColors: [127], faceTextureIds: [42], faceTextureUvs: [0.25, 0.5, 1.25, 0.5, 0.25, 1.5] };
        const result = prepareCacheModel(model);
        expect(result.uvs).toEqual([0.25, 0.5, 0.25, 1.5, 1.25, 0.5]);
        expect(result.textureIds).toEqual([42]);
        expect(result.colors).toEqual([1, 1, 1, 1, 1, 1, 1, 1, 1]);
    });
    const triangle = { positions: [0, 0, 0, 10, 0, 0, 0, 20, 0], indices: [0, 1, 2], faceColors: [0x1234] };

    it("tints projected textures with face color when the material preserves color", () => {
        const textured = { ...triangle, faceTextureIds: [42], faceTextureUvs: [0, 0, 1, 0, 0, 1] };
        const materials = new Map([[42, {colorBlend: 0, brightness: 0}]]);
        expect(prepareCacheModel(textured, materials).colors).toEqual(prepareCacheModel(triangle).colors);
    });

    it("applies the material grayscale mix before brightness", () => {
        const textured = { ...triangle, faceTextureIds: [42], faceTextureUvs: [0, 0, 1, 0, 0, 1] };
        const mixed = prepareCacheModel(textured, new Map([[42, {colorBlend: 255, brightness: 0}]])).colors!;
        expect(Math.max(...mixed) - Math.min(...mixed)).toBeLessThan(0.01);
        const bright = prepareCacheModel(textured, new Map([[42, {colorBlend: 255, brightness: 255}]])).colors!;
        expect(bright).toEqual(Array(9).fill(1));
    });

    it("converts the vertical axis and winding while retaining face color", () => {
        const result = prepareCacheModel(triangle);
        expect(result.positions).toEqual([0, -0, 0, 0, -20, 0, 10, -0, 0]);
        expect(result.indices).toEqual([0, 1, 2]);
        expect(result.colors).toHaveLength(9);
        expect(result.colors?.slice(0, 3)).toEqual(result.colors?.slice(3, 6));
    });

    it("converts ordinary inverse alpha to vertex opacity", () => {
        expect(prepareCacheModel({ ...triangle, faceAlpha: [128] }).opacities).toEqual([127 / 255, 127 / 255, 127 / 255]);
    });

    it("hides alpha-255 helper faces without removing attachment geometry and retains alpha-254 shading faces", () => {
        const result = prepareCacheModel({ ...triangle, faceColors: [20287], faceAlpha: [255], particles: [{emitterId:403,face:0,definition:null}] });
        expect(result.opacities).toEqual([0, 0, 0]);
        expect(result.indices).toEqual([0, 1, 2]);
        expect(result.positions).toHaveLength(9);
        expect(result.particles?.[0].face).toBe(0);
        expect(prepareCacheModel({...triangle,faceAlpha:[254]}).opacities).toEqual([1,1,1]);
    });
});
