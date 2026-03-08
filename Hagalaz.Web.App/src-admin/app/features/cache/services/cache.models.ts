export type TypeKind =
    | "items"
    | "npcs"
    | "objects"
    | "sprites"
    | "quests"
    | "animations"
    | "graphics"
    | "maps"
    | "varp-bits"
    | "client-map-definitions"
    | "config-definitions"
    | "cs2"
    | "cs2-int";

export interface ArchiveSizesDto {
    items: number;
    npcs: number;
    objects: number;
    sprites: number;
    quests: number;
    animations: number;
    graphics: number;
    cs2: number;
    clientMapDefinitions: number;
    configDefinitions: number;
    varpBits: number;
    maps: number;
}

export interface SearchResultDto<T> {
    query: string;
    totalMatches: number;
    offset: number;
    limit: number;
    items: T[];
}

export interface TypeMutationResultDto {
    operation: string;
    type: string;
    id: number;
}

export interface SpriteMutationResultDto extends TypeMutationResultDto {
    width: number;
    height: number;
    frameCount: number;
}

export interface MapObjectDto {
    id: number;
    shapeType: number;
    rotation: number;
    x: number;
    y: number;
    z: number;
}

export interface MapTypeDto {
    id: number;
    terrainPlanes: number;
    terrainWidth: number;
    terrainHeight: number;
    objectCount: number;
    objects: MapObjectDto[];
}

export interface MapDecodeRequest {
    xteaKeys: number[];
}
