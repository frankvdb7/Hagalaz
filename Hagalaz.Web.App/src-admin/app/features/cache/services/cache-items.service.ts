import type { UpdateItemDefinitionDto, UpdateItemDefinitionResultDto, DuplicateItemDefinitionResultDto } from "./dtos";
import type { GetItemTypeDto } from "./dtos";
import { HttpClient, HttpParams, httpResource } from "@angular/common/http";
import { inject, Service, type Signal } from "@angular/core";
import type { Observable } from "rxjs";
import { environment } from "../../../../environments/environment";
import type { GetItemDefinitionDto, SearchDefinitionsDto } from "./cache.models";
import { cacheQueryResource } from "./cache-resource";

/** Rendered item icon options; omitted values keep the server's client defaults. */
export interface ItemSpriteOptions {
    width?: number;
    height?: number;
    outline?: 0 | 1 | 2;
    /** Six-digit RGB colour, or "none". */
    shadow?: string;
    quantity?: number;
}

@Service()
export class CacheItemsService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = `${environment.cacheApiUrl}types/items`;

    getItemSpriteImage(id: number, options: ItemSpriteOptions = {}): Observable<Blob> {
        let params = new HttpParams();
        for (const [key, value] of Object.entries(options)) if (value !== undefined) params = params.set(key, value);
        return this.http.get(`${this.baseUrl}/${id}/image`, { params, responseType: "blob" });
    }

    definitionResource(id: Signal<number | null>) {
        return httpResource<GetItemDefinitionDto>(() => id() === null ? undefined : `${this.baseUrl}/${id()}/definition`);
    }

    updateItem(item: UpdateItemDefinitionDto): Observable<UpdateItemDefinitionResultDto> {
        return this.http.put<UpdateItemDefinitionResultDto>(`${this.baseUrl}/${item.id}/definition`, item);
    }

    duplicateItem(id: number): Observable<DuplicateItemDefinitionResultDto> {
        return this.http.post<DuplicateItemDefinitionResultDto>(`${this.baseUrl}/${id}/duplicate`, {});
    }

    rangeResource(range: Signal<{ startId: number; endIdExclusive: number } | null>) {
        return cacheQueryResource<GetItemTypeDto[]>(`${this.baseUrl}/range`, range);
    }

    searchResource(search: Signal<{ query: string; offset: number; limit: number } | null>) {
        return cacheQueryResource<SearchDefinitionsDto<GetItemTypeDto>>(`${this.baseUrl}/search`, search);
    }
}
