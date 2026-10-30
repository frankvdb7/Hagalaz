export interface AnimationFrameData {
    frame: { skeletonId: number; transforms: { slot: number; x: number; y: number; z: number; pivotSlot: number; tweenFlags: number }[] };
    skeleton: { types: number[]; groups: number[][] };
}

/** Applies a discrete frame in cache coordinates, always starting from the base pose. */
export function poseModel(positions: readonly number[], skins: readonly number[], data: AnimationFrameData, coordinateScale = 1): number[] {
    if (skins.length !== positions.length / 3) throw new Error("The model has no compatible vertex skin groups.");
    // Work in the client's normalized units; legacy raw models use coordinateScale 1/4.
    const result = Array.from(positions, value => Math.trunc(value / coordinateScale));
    let highPrecision = false;
    const ensurePrecision = () => {
        if (highPrecision) return;
        for (let index = 0; index < result.length; index++) result[index] *= 16;
        highPrecision = true;
    };
    let pivot = [0, 0, 0];
    const verticesFor = (slot: number) => {
        const groups = new Set(data.skeleton.groups[slot] ?? []);
        return skins.flatMap((skin, vertex) => groups.has(skin) ? [vertex * 3] : []);
    };
    const setPivot = (slot: number, offsets: number[]) => {
        const vertices = verticesFor(slot);
        pivot = offsets.map((offset, axis) => offset + (vertices.length ? Math.trunc(vertices.reduce((sum, vertex) => sum + result[vertex + axis], 0) / vertices.length) : 0));
    };
    for (const transform of data.frame.transforms) {
        const type = data.skeleton.types[transform.slot];
        if (type === 5) continue; // Face alpha is handled by poseFaceAlpha.
        if (type < 0 || type > 3) throw new Error(`Animation transform type ${type} is not supported yet.`);
        if (transform.pivotSlot >= 0) { ensurePrecision(); setPivot(transform.pivotSlot, [0, 0, 0]); }
        const values = [transform.x, transform.y, transform.z];
        if (type === 0) { ensurePrecision(); setPivot(transform.slot, values.map(value => value * 16)); continue; }
        if (type === 1) ensurePrecision();
        const sine = type === 2 ? values.map(value => Math.trunc(16384 * Math.sin(value * 3.834951969714103e-4))) : [];
        const cosine = type === 2 ? values.map(value => Math.trunc(16384 * Math.cos(value * 3.834951969714103e-4))) : [];
        for (const vertex of verticesFor(transform.slot)) {
            if (type === 1) {
                for (let axis = 0; axis < 3; axis++) result[vertex + axis] += values[axis] * 16;
                continue;
            }
            let x = result[vertex] - pivot[0];
            let y = result[vertex + 1] - pivot[1];
            let z = result[vertex + 2] - pivot[2];
            if (type === 3) {
                x = Math.trunc(x * transform.x / 128);
                y = Math.trunc(y * transform.y / 128);
                z = Math.trunc(z * transform.z / 128);
            } else {
                if (transform.z) [x, y] = [(y * sine[2] + x * cosine[2] + 16383) >> 14, (y * cosine[2] - x * sine[2] + 16383) >> 14];
                if (transform.x) [y, z] = [(y * cosine[0] - z * sine[0] + 16383) >> 14, (y * sine[0] + z * cosine[0] + 16383) >> 14];
                if (transform.y) [x, z] = [(z * sine[1] + x * cosine[1] + 16383) >> 14, (z * cosine[1] - x * sine[1] + 16383) >> 14];
            }
            result[vertex] = x + pivot[0]; result[vertex + 1] = y + pivot[1]; result[vertex + 2] = z + pivot[2];
        }
    }
    return result.map(value => (highPrecision ? (value + 7) >> 4 : value) * coordinateScale);
}

export function poseFaceAlpha(alpha: readonly number[], faceSkins: readonly number[] | null | undefined, data: AnimationFrameData): number[] {
    const result = Array.from(alpha);
    if (!faceSkins) return result;
    for (const transform of data.frame.transforms) {
        if (data.skeleton.types[transform.slot] !== 5) continue;
        const groups = new Set(data.skeleton.groups[transform.slot] ?? []);
        for (let face = 0; face < result.length; face++)
            if (groups.has(faceSkins[face])) result[face] = Math.min(255, Math.max(0, result[face] + transform.x * 8));
    }
    return result;
}
