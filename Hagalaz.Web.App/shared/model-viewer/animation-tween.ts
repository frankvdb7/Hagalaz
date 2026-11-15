import type { AnimationFrameData } from "./model-pose";

type Transform = AnimationFrameData["frame"]["transforms"][number];

export const animationTickMs = 20;

export function animationFrameDelay(delays: readonly number[], index: number): number {
    return Math.max(1, delays[index] ?? 1);
}

export function nextAnimationFrame(index: number, count: number, loop: number): number {
    if (index + 1 < count) return index + 1;
    return loop > 0 && loop <= count ? count - loop : 0;
}

/** Model.method4453 merges the sparse transforms before dispatching the pose. */
export function tweenAnimationFrame<T extends AnimationFrameData>(current: T, next: T | undefined, tick: number, delay: number): T {
    if (!next || !tick || current.frame.skeletonId !== next.frame.skeletonId || current === next) return current;
    const from = current.frame.transforms;
    const to = next.frame.transforms;
    const transforms: Transform[] = [];
    let i = 0, j = 0;
    for (const [slot, type] of current.skeleton.types.entries()) {
        const a = from[i]?.slot === slot ? from[i++] : undefined;
        const b = to[j]?.slot === slot ? to[j++] : undefined;
        if (a || b) transforms.push(tweenTransform(slot, type, a, b, tick, delay));
    }
    return { ...current, frame: { ...current.frame, transforms } };
}

function tweenTransform(slot: number, type: number, from: Transform | undefined, to: Transform | undefined, tick: number, delay: number): Transform {
    const neutral = type === 3 || type === 10 ? 128 : 0;
    const pivotSlot = from && from.pivotSlot !== -1 ? from.pivotSlot : to?.pivotSlot ?? -1;
    const a = from ?? { x: neutral, y: neutral, z: neutral, tweenFlags: 0 };
    const b = to ?? { x: neutral, y: neutral, z: neutral, tweenFlags: 0 };
    if ((a.tweenFlags & 2) || (b.tweenFlags & 1)) return { ...a, slot, pivotSlot };
    const linear = (start: number, end: number) => start + Math.trunc(Math.imul(end - start, tick) / delay);
    const angle = (start: number, end: number, period: number) => {
        const half = period >> 1;
        const delta = ((end - start + half) & (period - 1)) - half;
        return (start + Math.trunc(Math.imul(delta, tick) / delay)) & (period - 1);
    };
    const [x, y, z] = tweenAxes(type, a, b, linear, angle);
    return { slot, x, y, z, pivotSlot, tweenFlags: 0 };
}

function tweenAxes(
    type: number, a: Pick<Transform, "x" | "y" | "z">, b: Pick<Transform, "x" | "y" | "z">,
    linear: (start: number, end: number) => number, angle: (start: number, end: number, period: number) => number,
): [number, number, number] {
    switch (type) {
        case 2: return [angle(a.x, b.x, 16384), angle(a.y, b.y, 16384), angle(a.z, b.z, 16384)];
        case 9: return [angle(a.x, b.x, 16384), 0, 0];
        case 7: return [angle(a.x, b.x, 64), linear(a.y, b.y), linear(a.z, b.z)];
        default: return [linear(a.x, b.x), linear(a.y, b.y), linear(a.z, b.z)];
    }
}
