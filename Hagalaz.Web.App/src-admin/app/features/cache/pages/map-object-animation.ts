import { poseBillboards, poseFaceAlpha, poseFaceColors, poseModel } from "../../../../../shared/model-viewer/model-pose";
import { animationFrameDelay, animationTickMs, nextAnimationFrame, tweenAnimationFrame } from "../../../../../shared/model-viewer/animation-tween";
import type { GetModelDto } from "../services/dtos";
import type { GetAnimationFrameDto } from "../services/dtos/get-animation-frame-dto";
import type { MapObjectClip } from "../services/map-render.models";

interface MapAnimationSample {
    id: number;
    key: number | string;
    delay: number;
    tween?: { nextId: number; tick: number; duration: number };
}

export function mapAnimationTimeline({ sequence }: MapObjectClip): (elapsed: number) => MapAnimationSample {
    const first = sequence.frameIds[0];
    if (sequence.frameIds.every(id => id === first)) return () => ({ id: first, key: first, delay: Infinity });
    let duration = 0;
    const ends = sequence.frameIds.map((_, index) => duration += animationFrameDelay(sequence.frameDurations, index) * animationTickMs);
    const loopStart = ends[nextAnimationFrame(ends.length - 1, ends.length, sequence.loop) - 1] ?? 0;
    return (elapsed: number) => {
        // RenderAnimation advances only after the final tick of a tween.
        const clock = sequence.isTweened ? Math.max(0, elapsed - animationTickMs) : elapsed;
        const time = clock < duration ? clock : loopStart + (clock - duration) % (duration - loopStart);
        const index = ends.findIndex(end => time < end);
        const id = sequence.frameIds[index];
        if (!sequence.isTweened) return { id, key: id, delay: ends[index] - time };
        const delay = animationTickMs - elapsed % animationTickMs;
        if (elapsed < animationTickMs) return { id, key: id, delay };
        const tick = 1 + Math.floor((time - (ends[index - 1] ?? 0)) / animationTickMs);
        const nextId = sequence.frameIds[nextAnimationFrame(index, ends.length, sequence.loop)];
        const ticks = animationFrameDelay(sequence.frameDurations, index);
        return { id, key: `${id}/${nextId}/${tick}/${ticks}`, delay, tween: { nextId, tick, duration: ticks } };
    };
}

export function mapAnimationFrame(clip: MapObjectClip, { id, tween }: MapAnimationSample): GetAnimationFrameDto | undefined {
    const frame = clip.frames.get(id);
    return frame && tween ? tweenAnimationFrame(frame, clip.frames.get(tween.nextId), tween.tick, tween.duration) : frame;
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
    const faceColors = poseFaceColors(model.faceColors, model.faceSkins, frame);
    const billboards = model.billboards && poseBillboards(model.billboards, frame);
    if (!model.vertexSkins) return { ...model, faceAlpha, faceColors, billboards };
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
    return { ...model, positions: posed, faceAlpha, faceColors, billboards };
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
