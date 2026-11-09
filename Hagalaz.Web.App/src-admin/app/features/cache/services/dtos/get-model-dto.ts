import type { ModelParticleDto } from "./model-particle-dto";

export interface GetModelDto {
    id: number;
    positions: number[];
    indices: number[];
    faceColors: number[];
    faceAlpha: number[];
    faceShadingTypes?: number[] | null;
    ambient?: number;
    contrast?: number;
    facePriorities?: number[] | null;
    vertexSkins: number[] | null;
    warnings: string[];
    animationIds: number[] | null;
    objectAnimation?: { rotation: number; offset: number[]; positionsBeforeExtraRotation: number[] | null } | null;
    coordinateScale: number;
    faceSkins: number[] | null;
    faceTextureIds: number[] | null;
    faceTextureVertices: number[] | null;
    faceTextureUvs: number[] | null;
    firstTexelFaces?: number[] | null;
    scale: number[] | null;
    particles: ModelParticleDto[] | null;
}
