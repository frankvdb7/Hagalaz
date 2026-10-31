import type { ModelParticleDto } from "./model-particle-dto";

export interface GetModelDto {
    id: number;
    positions: number[];
    indices: number[];
    faceColors: number[];
    faceAlpha: number[];
    vertexSkins: number[] | null;
    warnings: string[];
    animationIds: number[] | null;
    coordinateScale: number;
    faceSkins: number[] | null;
    faceTextureIds: number[] | null;
    faceTextureVertices: number[] | null;
    faceTextureUvs: number[] | null;
    scale: number[] | null;
    particles: ModelParticleDto[] | null;
}
