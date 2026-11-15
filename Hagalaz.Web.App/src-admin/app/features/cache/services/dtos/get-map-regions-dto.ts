export interface MapRegionSummaryDto {
    id: number;
    regionX: number;
    regionY: number;
    terrainFileId: number | null;
    landscapeFileId: number | null;
}

export interface GetMapRegionsDto {
    items: MapRegionSummaryDto[];
    total: number;
    offset: number;
    limit: number;
}
