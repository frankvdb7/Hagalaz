export interface GetAnimationSequenceDto {
    id: number;
    frameIds: number[];
    frameDurations: number[];
    loop: number;
    isTweened: boolean;
}
