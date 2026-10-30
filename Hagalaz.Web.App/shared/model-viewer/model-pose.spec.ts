import { expect, it } from "vitest";
import { poseModel, poseFaceAlpha, type AnimationFrameData } from "./model-pose";
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
