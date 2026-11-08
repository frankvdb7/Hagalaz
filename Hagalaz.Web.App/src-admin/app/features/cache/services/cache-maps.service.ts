import { HttpClient } from "@angular/common/http";
import { inject, Service, type Signal } from "@angular/core";
import { map } from "rxjs";
import { environment } from "../../../../environments/environment";
import { cacheDefinitionResource, cacheQueryResource } from "./cache-resource";
import type { GetMapDefinitionDto, GetMapRegionsDto, GetTextureMaterialDto } from "./dtos";
import type { MapFloor, MapPlacement, MapRenderRegion, MapTerrainTileChange, MapWorldMapRefreshResult, ObjectAppearanceResult, UpdateMapLandscapeResultDto, UpdateMapTerrainResultDto } from "./map-render.models";

@Service()
export class CacheMapsService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = `${environment.cacheApiUrl}types/maps`;

    render(id: number, xteaKeys?: number[]) {
        return this.http.post<MapRenderRegion>(`${this.baseUrl}/${id}/render`, { xteaKeys });
    }

    saveLandscape(id: number, objects: readonly MapPlacement[]) {
        return this.http.post<UpdateMapLandscapeResultDto>(`${this.baseUrl}/${id}/landscape`, { objects });
    }

    saveTerrain(id: number, tiles: readonly MapTerrainTileChange[]) {
        return this.http.post<UpdateMapTerrainResultDto>(`${this.baseUrl}/${id}/terrain`, { tiles });
    }

    refreshWorldMap(regions: readonly number[]) {
        return this.http.post<MapWorldMapRefreshResult>(`${this.baseUrl}/worldmap/refresh`, { regions });
    }

    floors() {
        return this.http.get<MapFloor[]>(`${this.baseUrl}/floors`);
    }

    appearances(placements: readonly MapPlacement[]) {
        return this.http.post<ObjectAppearanceResult[]>(
            `${environment.cacheApiUrl}types/objects/render-appearances`,
            placements.map(({ id, shape, rotation }) => ({ id, shape, rotation }))
        );
    }

    texture(id: number) {
        return this.http.get(`${environment.cacheApiUrl}textures/${id}/image`, { responseType: "blob", observe: "response" }).pipe(
            map((response) => {
                const metadata = response.headers.get("X-Texture-Material");
                if (!response.body || !metadata) throw new Error("Texture image or material metadata is missing.");
                return { blob: response.body, material: JSON.parse(metadata) as GetTextureMaterialDto };
            })
        );
    }

    regionsResource(page: Signal<{ offset: number; limit: number } | null>) {
        return cacheQueryResource<GetMapRegionsDto>(this.baseUrl, page);
    }

    definitionResource(id: Signal<number | null>) {
        return cacheDefinitionResource<GetMapDefinitionDto>(this.baseUrl, id);
    }
}
