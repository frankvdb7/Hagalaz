import { expect, it } from "vitest";
import { tweenAnimationFrame } from "./animation-tween";
import type { AnimationFrameData } from "./model-pose";

function frame(types: number[], slots: [number, number, number, number, number?, number?][], skeletonId = 1): AnimationFrameData {
    return {
        skeleton: { types, groups: types.map(() => [0]) },
        frame: { skeletonId, transforms: slots.map(([slot, x, y, z, tweenFlags = 0, pivotSlot = -1]) => ({ slot, x, y, z, tweenFlags, pivotSlot })) },
    };
}

it("merges sparse slots using the client defaults, wrapped angles and signed integer division", () => {
    const types = [0, 1, 2, 3, 5, 7, 8, 9, 10];
    const a = frame(types, [[1, -5, 0, 0], [2, 16380, 4, 0], [3, 65, 128, 193], [4, 5, 0, 0], [5, 62, 0, 0], [7, 16380, 2, 3]]);
    const b = frame(types, [[1, -10, 0, 0, 0, 0], [2, 4, 16380, 8192], [5, 2, -5, 9], [6, 4, -5, 0], [7, 4, 8, 9], [8, 64, 192, 128]]);
    // Model.method4453 at tick 1 of 2. A half turn chooses the negative arc.
    expect(tweenAnimationFrame(a, b, 1, 2).frame.transforms.map(({ slot, x, y, z, pivotSlot }) => [slot, x, y, z, pivotSlot])).toEqual([
        [1, -7, 0, 0, 0], [2, 0, 0, 12288, -1], [3, 96, 128, 161, -1],
        [4, 3, 0, 0, -1], [5, 0, -2, 4, -1], [6, 2, -2, 0, -1],
        [7, 0, 0, 0, -1], [8, 96, 160, 128, -1],
    ]);
});

it("honors outgoing and incoming hold flags, including at the final tick", () => {
    const types = [1, 1, 1];
    const a = frame(types, [[0, 10, 0, 0, 2], [1, 20, 0, 0, 1]]);
    const b = frame(types, [[0, 40, 0, 0], [1, 50, 0, 0, 2], [2, 60, 0, 0, 1]]);
    expect(tweenAnimationFrame(a, b, 2, 2).frame.transforms.map(transform => transform.x)).toEqual([10, 50, 0]);
    expect(tweenAnimationFrame(a, b, 0, 2)).toBe(a);
    expect(tweenAnimationFrame(a, frame(types, [], 2), 1, 2)).toBe(a);
});
