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
    speedDrag?: { mode: number; strength: number } | null;
}
