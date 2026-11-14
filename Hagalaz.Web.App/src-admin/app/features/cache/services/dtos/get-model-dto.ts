import type { ModelParticleDto } from "./model-particle-dto";

export interface ModelEffectorDto {
    effectorId: number;
    vertex: number;
    definition: {
        x: number; y: number; z: number;
        coneAngle: number; falloff: number; attenuation: number; scope: number;
        directDisplacement: boolean; radial: boolean; reverse: boolean;
    } | null;
    warning: string | null;
}

export interface ModelBillboardDto {
    definitionId: number;
    face: number;
    group: number;
    depthOffset: number;
    definition: {
        textureId: number;
        halfWidth: number;
        halfHeight: number;
        blendMode: number;
        hideFace: boolean;
        hideInSpecialPass: boolean;
    } | null;
    warning: string | null;
}

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
    effectors?: ModelEffectorDto[] | null;
    billboards?: ModelBillboardDto[] | null;
}
