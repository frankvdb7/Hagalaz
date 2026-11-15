import type { AnimationTransformDto } from "./animation-transform-dto";

export interface AnimationFrameDto {
    skeletonId: number;
    transforms: AnimationTransformDto[];
}
