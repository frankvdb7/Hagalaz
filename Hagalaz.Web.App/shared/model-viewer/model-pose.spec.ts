import { expect, it } from "vitest";
import { poseModel, poseBillboards, poseFaceAlpha, poseFaceColors, unsupportedTransformTypes, type AnimationFrameData } from "./model-pose";
import { readFileSync } from "node:fs";
import { prepareCacheModel } from "./cache-model-geometry";

const demon = JSON.parse(readFileSync(new URL("../../../TestSupport/Fixtures/black-demon-animation.json", import.meta.url), "utf8")) as {
    positions: number[]; skins: number[]; skeleton: AnimationFrameData["skeleton"];
    frames: { index: number; frame: AnimationFrameData["frame"]; expectedPositions: number[] }[];
};

it.each(demon.frames)("matches all GameClient vertices for Black demon frame $index", sample => {
    const data = { frame: sample.frame, skeleton: demon.skeleton };
    expect(poseModel(demon.positions, demon.skins, data)).toEqual(sample.expectedPositions);
    // Raw pre-version-13 models keep quarter-sized coordinates, including frame movement.
    expect(poseModel(demon.positions.map(value => value / 4), demon.skins, data, 0.25))
        .toEqual(sample.expectedPositions.map(value => value / 4));
});

it("applies NPC scale after animation and uses client rounding for negative coordinates", () => {
    const data: AnimationFrameData = { skeleton: { types: [1], groups: [[0]] }, frame: { skeletonId: 1, transforms: [{ slot: 0, x: 1, y: -1, z: 0, pivotSlot: -1, tweenFlags: 0 }] } };
    const positions = poseModel([-3, -5, 0, 10, 0, 0, 0, 20, 0], [0, 0, 0], data);
    const rendered = prepareCacheModel({ positions, indices: [0, 1, 2], faceColors: [127], scale: [1.5, 0.5, 2] });
    expect(rendered.positions).toEqual([-3, 3, 0, 1, -9, 0, 16, 1, 0]);
});

it("translates only vertices in the selected skin group without changing the base pose", () => {
    const positions = [0, 0, 0, 10, 20, 30];
    const frame: AnimationFrameData = { skeleton: { types: [1], groups: [[2]] }, frame: { skeletonId: 1, transforms: [{ slot: 0, x: 3, y: 4, z: 5, pivotSlot: -1, tweenFlags: 0 }] } };
    expect(poseModel(positions, [1, 2], frame)).toEqual([0, 0, 0, 13, 24, 35]);
    expect(positions).toEqual([0, 0, 0, 10, 20, 30]);
});

it("animates face-group transparency without moving vertices and clamps the result", () => {
    const frame: AnimationFrameData = { skeleton: { types: [5], groups: [[0]] }, frame: { skeletonId: 1, transforms: [{ slot: 0, x: 3, y: 0, z: 0, pivotSlot: -1, tweenFlags: 0 }] } };
    expect(poseModel([0, 0, 0], [0], frame)).toEqual([0, 0, 0]);
    expect(poseFaceAlpha([10, 250, 80], [0, 0, 1], frame)).toEqual([34, 255, 80]);
});

it("matches the compiled client's packed HSL results and relights the posed faces", () => {
    const data: AnimationFrameData = {
        skeleton: { types: [7, 7], groups: [[0], [1]] },
        frame: { skeletonId: 1, transforms: [
            { slot: 0, x: 2, y: 4, z: 5, pivotSlot: -1, tweenFlags: 0 },
            { slot: 1, x: -3, y: -4, z: -5, pivotSlot: -1, tweenFlags: 0 },
        ] },
    };
    const base = [65406, 1154, 128];
    const posed = poseFaceColors(base, [0, 1, 2], data);
    // Compiled client values. These faces clamp saturation the same way in every renderer.
    expect(posed).toEqual([2047, 63488, 128]);
    const model = { positions: [0, 0, 0, 128, 0, 0, 0, 0, 128], indices: [0, 1, 2], ambient: 64, contrast: 768 };
    const lit = prepareCacheModel({ ...model, faceColors: [posed[0]] });
    expect(lit.preparedLighting).toBe(true);
    expect(lit.colors).not.toEqual(prepareCacheModel({ ...model, faceColors: [base[0]] }).colors);
    expect(lit.colors).not.toEqual(prepareCacheModel({ positions: model.positions, indices: model.indices, faceColors: [posed[0]] }).colors);
});

it("divides the saturation delta like the client's OpenGL models, truncating toward zero", () => {
    const data: AnimationFrameData = {
        skeleton: { types: [7, 7, 7], groups: [[0], [1], [2]] },
        frame: { skeletonId: 1, transforms: [
            { slot: 0, x: 0, y: 4, z: 0, pivotSlot: -1, tweenFlags: 0 },
            { slot: 1, x: 0, y: -5, z: 0, pivotSlot: -1, tweenFlags: 0 },
            { slot: 2, x: 0, y: 3, z: 0, pivotSlot: -1, tweenFlags: 0 },
        ] },
    };
    expect(poseFaceColors([0, 3 << 7, 0], [0, 1, 2], data)).toEqual([1 << 7, 2 << 7, 0]);
});

it("applies a pivot from a transform that does not move vertices", () => {
    const data: AnimationFrameData = {
        skeleton: { types: [0, 5, 2], groups: [[0], [2], [1]] },
        frame: { skeletonId: 1, transforms: [
            { slot: 1, x: 0, y: 0, z: 0, pivotSlot: 0, tweenFlags: 0 },
            { slot: 2, x: 0, y: 8192, z: 0, pivotSlot: -1, tweenFlags: 0 },
        ] },
    };
    expect(poseModel([10, 0, 0, 20, 0, 0], [0, 1], data)).toEqual([10, 0, 0, 0, 0, 0]);
});

it("reports unsupported transforms while applying the supported transforms in the same frame", () => {
    const data: AnimationFrameData = {
        skeleton: { types: [11, 1], groups: [[0], [0]] },
        frame: { skeletonId: 1, transforms: [
            { slot: 0, x: 20, y: 0, z: 0, pivotSlot: -1, tweenFlags: 0 },
            { slot: 1, x: 4, y: 0, z: 0, pivotSlot: -1, tweenFlags: 0 },
        ] },
    };
    expect(unsupportedTransformTypes(data)).toEqual([11]);
    expect(poseModel([0, 0, 0], [0], data)).toEqual([4, 0, 0]);
});

it("matches compiled OpenGL billboard translation, angle wrap and successive integer scales", () => {
    const types = [8, 9, 9, 10, 10];
    const xy = [[-11, 19], [16380, 0], [8, 0], [65, 193], [65, 67]];
    const data: AnimationFrameData = {
        skeleton: { types, groups: types.map(() => [0]) },
        frame: { skeletonId: 1, transforms: xy.map(([x, y], slot) => ({ slot, x, y, z: 0, pivotSlot: -1, tweenFlags: 0 })) },
    };
    expect(poseBillboards([{ group: 0 }, { group: 1 }], data)).toEqual([
        { group: 0, pose: { x: -11, y: 19, angle: 4, scaleX: 33, scaleY: 101 } },
        { group: 1, pose: { x: 0, y: 0, angle: 0, scaleX: 128, scaleY: 128 } },
    ]);
});
