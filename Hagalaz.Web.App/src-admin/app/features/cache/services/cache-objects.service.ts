import type { GetObjectDefinitionDto, UpdateObjectNameDto } from "./dtos";
import { HttpClient, HttpParams } from "@angular/common/http";
import { inject, Service, type Signal } from "@angular/core";
import type { Observable } from "rxjs";
import type { UpdateObjectNameResultDto } from "./cache.models";
import { type CacheSearchPage, CacheTypeService, cacheQueryResource } from "./cache-resource";
import type { SearchDefinitionsDto } from "./dtos/search-definitions-dto";

export interface ObjectCatalogEntry { id: number; name: string; shape: number | null }

@Service()
export class CacheObjectsService extends CacheTypeService<GetObjectDefinitionDto> {
    private readonly http = inject(HttpClient);

    constructor() {
        super("objects");
    }

    catalogResource(page: Signal<(CacheSearchPage & { shapes?: string }) | null>) {
        return cacheQueryResource<SearchDefinitionsDto<ObjectCatalogEntry>>(`${this.baseUrl}/catalog`, page);
    }

    getObjectImage(id: number, options: { width: number; height: number; shape: number }): Observable<Blob> {
        return this.http.get(`${this.baseUrl}/${id}/image`, { params: new HttpParams({ fromObject: options }), responseType: "blob" });
    }

    updateObjectName(id: number, name: string): Observable<UpdateObjectNameResultDto> {
        return this.http.post<UpdateObjectNameResultDto>(`${this.baseUrl}/${id}/name`, { name } satisfies UpdateObjectNameDto);
    }
}
