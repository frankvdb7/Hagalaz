import type { ModelEffectorDto } from "./get-model-dto";

export interface ParticleEmitterDto {
    textureId: number;
    colorMin: number;
    colorMax: number;
    lifeMin: number;
    lifeMax: number;
    rateMin: number;
    rateMax: number;
    sizeMin: number;
    sizeMax: number;
    speedMin: number;
    speedMax: number;
    angles: number[];
    unsupportedOpcodes: number[];
    localEffectorIds?: number[] | null;
    constantEffectorIds?: number[] | null;
    constantEffectors?: NonNullable<ModelEffectorDto["definition"]>[] | null;
    sizeOverLife?: { target: number; lifetimePercent: number } | null;
    speedOverLife?: { rawTarget: number; lifetimePercent: number } | null;
    colorOverLife?: { targetArgb: number; rgbLifetimePercent: number; alphaLifetimePercent: number } | null;
    speedDrag?: { mode: number; strength: number } | null;
}
