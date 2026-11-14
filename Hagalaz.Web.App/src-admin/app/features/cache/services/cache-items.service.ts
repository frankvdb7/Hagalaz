import type { DuplicateItemDefinitionResultDto, GetItemTypeDto, UpdateItemDefinitionDto, UpdateItemDefinitionResultDto } from "./dtos";
import { HttpClient, HttpParams, httpResource } from "@angular/common/http";
import { inject, Service, type Signal } from "@angular/core";
import type { Observable } from "rxjs";
import type { GetItemDefinitionDto } from "./cache.models";
import { CacheSearchableTypeService } from "./cache-resource";

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
export class CacheItemsService extends CacheSearchableTypeService<GetItemDefinitionDto, GetItemTypeDto> {
    private readonly http = inject(HttpClient);

    constructor() {
        super("items");
    }

    getItemSpriteImage(id: number, options: ItemSpriteOptions = {}): Observable<Blob> {
        let params = new HttpParams();
        for (const [key, value] of Object.entries(options)) if (value !== undefined) params = params.set(key, value);
        return this.http.get(`${this.baseUrl}/${id}/image`, { params, responseType: "blob" });
    }

    /** Items expose the full editable definition under `/definition` rather than the shared `/{id}` route. */
    override definitionResource(id: Signal<number | null>) {
        return httpResource<GetItemDefinitionDto>(() => (id() === null ? undefined : `${this.baseUrl}/${id()}/definition`));
    }

    updateItem(item: UpdateItemDefinitionDto): Observable<UpdateItemDefinitionResultDto> {
        return this.http.put<UpdateItemDefinitionResultDto>(`${this.baseUrl}/${item.id}/definition`, item);
    }

    duplicateItem(id: number): Observable<DuplicateItemDefinitionResultDto> {
        return this.http.post<DuplicateItemDefinitionResultDto>(`${this.baseUrl}/${id}/duplicate`, {});
    }
}
