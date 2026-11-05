import { HttpClient } from "@angular/common/http";
import { inject, Service, type Signal } from "@angular/core";
import { map } from "rxjs";
import { environment } from "../../../../environments/environment";
import { cacheDefinitionResource, cacheQueryResource } from "./cache-resource";
import type { GetMapDefinitionDto, GetMapRegionsDto, GetTextureMaterialDto } from "./dtos";
import type { MapPlacement, MapRenderRegion, ObjectAppearanceResult } from "./map-render.models";

@Service()
export class CacheMapsService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = `${environment.cacheApiUrl}types/maps`;

    render(id: number, xteaKeys?: number[]) {
        return this.http.post<MapRenderRegion>(`${this.baseUrl}/${id}/render`, { xteaKeys });
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
