import type { MapObjectDto } from "./map-object-dto";

export interface GetMapDefinitionDto {
    id: number;
    terrainPlanes: number;
    terrainWidth: number;
    terrainHeight: number;
    objectCount: number;
    objects: MapObjectDto[];
}
