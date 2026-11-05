import {describe, expect, it} from "vitest";
import {clampLight, floorPaletteColor, lightModel, litFaceColor} from "./model-lighting";
import {prepareCacheModel} from "./cache-model-geometry";

const triangle = {positions: [0,0,0, 100,0,0, 0,100,0], indices: [0,1,2], faceColors: [0x1234], faceShadingTypes: [0]};

describe("client model lighting", () => {
    // Original Class491/Class230/Class356 with Class393's gamma fixed at 0.7.
    it.each([
        [0x1c1813, [48,42,42]], // Region 12850 underlay 164, texture 929 (riverbank).
        [0x29380f, [66,81,36]], // Underlay 160, texture 918 (grass).
        [0xff0000, [241,50,34]], [0x00ff00, [34,241,40]], [0x0000ff, [34,40,241]],
        [0, [0,0,0]], [0xffffff, [254,243,243]], [0x808080, [157,150,150]],
        [0x00aabb, [27,187,193]],
    ])("matches the client floor palette for RGB %i", (rgb, expected) => {
        expect(floorPaletteColor(rgb as number)).toEqual(expected);
    });
    // LightingReference.java runs the extracted client palette and packed-colour helpers with gamma fixed at 0.7.
    it("matches Java client lighting and palette fixtures on both sides of a face", () => {
        expect(lightModel(triangle)).toEqual([71,71,71]);
        const diagonal = {positions:[0,0,0, 1,0,-1, 1,-1,0],indices:[0,1,2]};
        expect(lightModel(diagonal)).toEqual([286,286,286]);
        expect(lightModel({...diagonal,indices:[0,2,1]})).toEqual([-93,-93,-93]);
        expect(lightModel({...triangle, indices: [0,2,1]})).toEqual([221,221,221]);
        expect(litFaceColor(0x1234,71,false)).toEqual([72,50,31]);
        expect(litFaceColor(0x1234,221,false)).toEqual([162,114,72]);
        expect(litFaceColor(0x1234,35,true)).toEqual([74,50,30]);
        expect(litFaceColor(0x1234,110,true)).toEqual([165,113,67]);
    });

    it("accumulates smooth normals with contribution counts and keeps flat normals separate", () => {
        const model = {positions: [0,0,0, 100,0,0, 0,100,0, 0,0,100], indices: [0,1,2, 0,3,1]};
        expect(lightModel(model)).toEqual([64,64,71,64,57,64]);
        expect(lightModel({...model, faceShadingTypes: [1,1]})).toEqual([71,71,71,57,57,57]);
    });

    it("applies ambient before packed lighting and contrast to directional response", () => {
        expect(litFaceColor(0x1234,71,false,128)).not.toEqual(litFaceColor(0x1234,71,false,64));
        expect(lightModel({...triangle,contrast:0})).toEqual([147,147,147]);
        expect(lightModel({...triangle,contrast:1700})).toEqual([109,109,109]);
        expect([clampLight(-100),clampLight(2),clampLight(126),clampLight(1000)]).toEqual([2,2,126,126]);
        expect(litFaceColor(0x1234,-100,true)).toEqual(litFaceColor(0x1234,0,true));
        expect(litFaceColor(0x1234,10000,true)).toEqual(litFaceColor(0x1234,100000,true));
    });

    it("relights posed geometry while preserving source faces, UVs and priorities", () => {
        const model = {...triangle, facePriorities:[11], faceTextureIds:[2], faceTextureUvs:[0,0,1,0,0,1], particles:[{emitterId:1,face:0,definition:null}]};
        const base = prepareCacheModel(model);
        const posed = prepareCacheModel({...model,positions:[0,0,0, 100,0,0, 0,-100,0]});
        expect(posed.colors).not.toEqual(base.colors);
        expect(posed.indices).toEqual(base.indices);
        expect(posed.uvs).toEqual(base.uvs);
        expect(posed.facePriorities).toEqual([11]);
        expect(posed.particles).toEqual(model.particles);
        expect(posed.preparedLighting).toBe(true);
    });

    it("preserves hidden and constant-shading face slots and leaves effect 6 unlit", () => {
        expect(prepareCacheModel({...triangle,faceShadingTypes:[2]}).opacities).toEqual([0,0,0]);
        expect(prepareCacheModel({...triangle,faceAlpha:[255]}).opacities).toEqual([0,0,0]);
        expect(prepareCacheModel({...triangle,faceAlpha:[254]}).colors).toEqual(Array(9).fill(0));
        const textured = {...triangle,faceTextureIds:[2],faceTextureUvs:[0,0,1,0,0,1]};
        const materials = new Map([[2,{effectId:6}]]);
        expect(prepareCacheModel(textured,materials).colors).toEqual(prepareCacheModel({...textured,positions:[0,0,0,100,0,0,0,-100,0]},materials).colors);
    });
});
