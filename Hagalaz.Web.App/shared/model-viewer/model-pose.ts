export interface AnimationFrameData {
    frame: { skeletonId: number; transforms: { slot: number; x: number; y: number; z: number; pivotSlot: number; tweenFlags: number }[] };
    skeleton: { types: number[]; groups: number[][] };
}

export interface BillboardPose {
    x: number;
    y: number;
    angle: number;
    scaleX: number;
    scaleY: number;
}

export function poseBillboards<T extends { group: number }>(billboards: readonly T[], data: AnimationFrameData): (T & { pose: BillboardPose })[] {
    return billboards.map(billboard => {
        const pose = { x: 0, y: 0, angle: 0, scaleX: 128, scaleY: 128 };
        for (const transform of data.frame.transforms) {
            if (billboard.group < 0 || !data.skeleton.groups[transform.slot]?.includes(billboard.group)) continue;
            const { x, y } = transform;
            switch (data.skeleton.types[transform.slot]) {
                case 8: pose.x += x; pose.y += y; break;
                case 9: pose.angle = (pose.angle + x) & 0x3fff; break;
                case 10: pose.scaleX = (pose.scaleX * x) >> 7; pose.scaleY = (pose.scaleY * y) >> 7; break;
            }
        }
        return { ...billboard, pose };
    });
}

type FrameTransform = AnimationFrameData["frame"]["transforms"][number];
type Vector = [number, number, number];

const rotationUnit = 3.834951969714103e-4;

/** Rotates around Z, then X, then Y with the client's 14-bit fixed-point sine table, skipping axes without rotation. */
function rotateOffset([x, y, z]: Vector, rotation: Vector, sine: readonly number[], cosine: readonly number[]): Vector {
    if (rotation[2]) [x, y] = [(y * sine[2] + x * cosine[2] + 16383) >> 14, (y * cosine[2] - x * sine[2] + 16383) >> 14];
    if (rotation[0]) [y, z] = [(y * cosine[0] - z * sine[0] + 16383) >> 14, (y * sine[0] + z * cosine[0] + 16383) >> 14];
    if (rotation[1]) [x, z] = [(z * sine[1] + x * cosine[1] + 16383) >> 14, (z * cosine[1] - x * sine[1] + 16383) >> 14];
    return [x, y, z];
}

/** Vertex positions being posed, in the client's normalized units, switching to 4-bit sub-unit precision on demand. */
class PoseBuffer {
    private highPrecision = false;
    private pivot: Vector = [0, 0, 0];
    readonly positions: number[];

    constructor(positions: readonly number[], private readonly coordinateScale: number, readonly verticesFor: (slot: number) => number[]) {
        this.positions = Array.from(positions, value => Math.trunc(value / coordinateScale));
    }

    ensurePrecision() {
        if (this.highPrecision) return;
        for (let index = 0; index < this.positions.length; index++) this.positions[index] *= 16;
        this.highPrecision = true;
    }

    /** Sets the pivot to the slot's vertex centroid plus an offset. */
    setPivot(slot: number, offsets: Vector) {
        this.ensurePrecision();
        const vertices = this.verticesFor(slot);
        const centroid = (axis: number) => vertices.length
            ? Math.trunc(vertices.reduce((sum, vertex) => sum + this.positions[vertex + axis], 0) / vertices.length)
            : 0;
        this.pivot = offsets.map((offset, axis) => offset + centroid(axis)) as Vector;
    }

    translate(slot: number, values: Vector) {
        this.ensurePrecision();
        for (const vertex of this.verticesFor(slot)) {
            for (let axis = 0; axis < 3; axis++) this.positions[vertex + axis] += values[axis] * 16;
        }
    }

    /** Replaces each vertex of the slot with a transform of its offset from the pivot. */
    transformAroundPivot(slot: number, transform: (offset: Vector) => Vector) {
        const pivot = this.pivot;
        for (const vertex of this.verticesFor(slot)) {
            const offset = transform([this.positions[vertex] - pivot[0], this.positions[vertex + 1] - pivot[1], this.positions[vertex + 2] - pivot[2]]);
            for (let axis = 0; axis < 3; axis++) this.positions[vertex + axis] = offset[axis] + pivot[axis];
        }
    }

    result(): number[] {
        return this.positions.map(value => (this.highPrecision ? (value + 7) >> 4 : value) * this.coordinateScale);
    }
}

const poseTransforms: Record<number, (pose: PoseBuffer, transform: FrameTransform, values: Vector) => void> = {
    0: (pose, transform, values) => pose.setPivot(transform.slot, values.map(value => value * 16) as Vector),
    1: (pose, transform, values) => pose.translate(transform.slot, values),
    2: (pose, transform, values) => {
        const sine = values.map(value => Math.trunc(16384 * Math.sin(value * rotationUnit)));
        const cosine = values.map(value => Math.trunc(16384 * Math.cos(value * rotationUnit)));
        pose.transformAroundPivot(transform.slot, offset => rotateOffset(offset, values, sine, cosine));
    },
    3: (pose, transform, values) => pose.transformAroundPivot(transform.slot, offset =>
        offset.map((value, axis) => Math.trunc((value * values[axis]) / 128)) as Vector),
};

/** Applies a frame in cache coordinates, always starting from the base pose. */
export function poseModel(positions: readonly number[], skins: readonly number[], data: AnimationFrameData, coordinateScale = 1): number[] {
    if (skins.length !== positions.length / 3) throw new Error("The model has no compatible vertex skin groups.");
    // Work in the client's normalized units; legacy raw models use coordinateScale 1/4.
    const pose = new PoseBuffer(positions, coordinateScale, verticesBySlot(skins, data.skeleton.groups));
    for (const transform of data.frame.transforms) {
        // Model.method4453 applies the pivot before dispatching on the transform type.
        if (transform.pivotSlot >= 0) pose.setPivot(transform.pivotSlot, [0, 0, 0]);
        const apply = poseTransforms[data.skeleton.types[transform.slot]];
        apply?.(pose, transform, [transform.x, transform.y, transform.z]);
    }
    return pose.result();
}

function verticesBySlot(skins: readonly number[], slots: readonly number[][]): (slot: number) => number[] {
    const bySkin = new Map<number, number[]>();
    for (const [vertex, skin] of skins.entries()) {
        const group = bySkin.get(skin);
        if (group) group.push(vertex * 3);
        else bySkin.set(skin, [vertex * 3]);
    }
    return slot => {
        const vertices: number[] = [];
        for (const skin of new Set(slots[slot])) {
            const group = bySkin.get(skin);
            if (group) vertices.push(...group);
        }
        return vertices;
    };
}

export function poseFaceAlpha(alpha: readonly number[], faceSkins: readonly number[] | null | undefined, data: AnimationFrameData): number[] {
    return poseFaces(alpha, faceSkins, data, 5, (value, transform) => Math.min(255, Math.max(0, value + transform.x * 8)));
}

export function poseFaceColors(colors: readonly number[], faceSkins: readonly number[] | null | undefined, data: AnimationFrameData): number[] {
    return poseFaces(colors, faceSkins, data, 7, (value, { x, y, z }) => {
        const hue = ((value >> 10) + x) & 0x3f;
        // The OpenGL models divide this delta by 4. Only the software Model_Sub3 adds it directly.
        const saturation = Math.min(7, Math.max(0, ((value >> 7) & 0x7) + Math.trunc(y / 4)));
        const lightness = Math.min(127, Math.max(0, (value & 0x7f) + z));
        return (hue << 10) | (saturation << 7) | lightness;
    });
}

export function unsupportedTransformTypes(data: AnimationFrameData): number[] {
    const types = data.frame.transforms.map(transform => data.skeleton.types[transform.slot]);
    return [...new Set(types.filter(type => ![0, 1, 2, 3, 5, 7, 8, 9, 10].includes(type)))];
}

function poseFaces(
    values: readonly number[], faceSkins: readonly number[] | null | undefined, data: AnimationFrameData,
    type: number, apply: (value: number, transform: AnimationFrameData["frame"]["transforms"][number]) => number,
): number[] {
    const result = Array.from(values);
    if (!faceSkins) return result;
    for (const transform of data.frame.transforms) {
        if (data.skeleton.types[transform.slot] !== type) continue;
        const groups = new Set(data.skeleton.groups[transform.slot] ?? []);
        for (let face = 0; face < result.length; face++)
            if (groups.has(faceSkins[face])) result[face] = apply(result[face], transform);
    }
    return result;
}
