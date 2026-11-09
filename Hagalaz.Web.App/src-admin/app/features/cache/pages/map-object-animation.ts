import { poseFaceAlpha, poseModel } from "../../../../../shared/model-viewer/model-pose";
import type { GetModelDto } from "../services/dtos";
import type { GetAnimationFrameDto } from "../services/dtos/get-animation-frame-dto";
import type { MapObjectClip } from "../services/map-render.models";

/** Cache delays are in 20 ms ticks. A loop offset counts back from the end. */
export function mapAnimationTimeline({ sequence }: MapObjectClip) {
    const first = sequence.frameIds[0];
    if (sequence.frameIds.every(id => id === first)) return () => ({ id: first, delay: Infinity });
    let duration = 0;
    const ends = sequence.frameIds.map((_, index) => duration += Math.max(1, sequence.frameDurations[index] ?? 1) * 20);
    const loopStart = sequence.loop > 0 ? ends[ends.length - sequence.loop - 1] ?? 0 : 0;
    return (elapsed: number) => {
        const time = elapsed < duration ? elapsed : loopStart + (elapsed - duration) % (duration - loopStart);
        const index = ends.findIndex(end => time < end);
        return { id: sequence.frameIds[index], delay: ends[index] - time };
    };
}

export function mapAnimatedAlphaFaces(model: GetModelDto, clip: MapObjectClip): boolean[] {
    const groups = new Set<number>();
    for (const { frame, skeleton } of clip.frames.values()) {
        for (const transform of frame.transforms) {
            if (skeleton.types[transform.slot] === 5) {
                for (const group of skeleton.groups[transform.slot]) groups.add(group);
            }
        }
    }
    return model.faceSkins?.map(skin => groups.has(skin)) ?? [];
}

/** The client poses the assembled model using the part's quarter-turn axes. */
export function poseMapObject(model: GetModelDto, frame: GetAnimationFrameDto): GetModelDto {
    const pose = model.objectAnimation;
    if (!pose) return model;
    const faceAlpha = poseFaceAlpha(model.faceAlpha, model.faceSkins, frame);
    if (!model.vertexSkins) return { ...model, faceAlpha };
    const source = pose.positionsBeforeExtraRotation ?? model.positions;
    const positions = source.map((value, index) => value - pose.offset[index % 3]);
    rotate(positions, (4 - pose.rotation) & 3);
    const posed = poseModel(positions, model.vertexSkins, frame, model.coordinateScale);
    rotate(posed, pose.rotation);
    for (let index = 0; index < posed.length; index++) posed[index] += pose.offset[index % 3];
    if (pose.positionsBeforeExtraRotation) {
        for (let index = 0; index < posed.length; index += 3) {
            const x = posed[index], z = posed[index + 2];
            posed[index] = ((x + z) * 11585) >> 14;
            posed[index + 2] = ((z - x) * 11585) >> 14;
        }
    }
    return { ...model, positions: posed, faceAlpha };
}

function rotate(positions: number[], turns: number): void {
    if (turns === 0) return;
    for (let index = 0; index < positions.length; index += 3) {
        const x = positions[index], z = positions[index + 2];
        if (turns === 1) { positions[index] = z; positions[index + 2] = -x; }
        else if (turns === 2) { positions[index] = -x; positions[index + 2] = -z; }
        else { positions[index] = -z; positions[index + 2] = x; }
    }
}
