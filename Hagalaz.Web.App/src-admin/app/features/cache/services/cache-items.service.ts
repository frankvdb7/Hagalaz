import { HttpClient } from "@angular/common/http";
import { inject, Service, type Signal } from "@angular/core";
import type { Observable } from "rxjs";
import { environment } from "../../../../environments/environment";
import type { ItemDefinitionDto, SearchResultDto } from "./cache.models";
import { cacheDefinitionResource, cacheQueryResource } from "./cache-resource";

@Service()
export class CacheItemsService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = `${environment.cacheApiUrl}types/items`;

    getItemSpriteImage(id: number): Observable<Blob> {
        return this.http.get(`${this.baseUrl}/${id}/image`, { responseType: "blob" });
    }

    definitionResource(id: Signal<number | null>) {
        return cacheDefinitionResource<ItemDefinitionDto>(this.baseUrl, id);
    }

    rangeResource(range: Signal<{ startId: number; endIdExclusive: number } | null>) {
        return cacheQueryResource<ItemDefinitionDto[]>(`${this.baseUrl}/range`, range);
    }

    searchResource(search: Signal<{ query: string; offset: number; limit: number } | null>) {
        return cacheQueryResource<SearchResultDto<ItemDefinitionDto>>(`${this.baseUrl}/search`, search);
    }
}
