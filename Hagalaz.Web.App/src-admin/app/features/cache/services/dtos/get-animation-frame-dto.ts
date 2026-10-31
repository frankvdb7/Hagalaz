import type { AnimationFrameDto } from "./animation-frame-dto";
import type { AnimationSkeletonDto } from "./animation-skeleton-dto";

export interface GetAnimationFrameDto {
    frame: AnimationFrameDto;
    skeleton: AnimationSkeletonDto;
}
