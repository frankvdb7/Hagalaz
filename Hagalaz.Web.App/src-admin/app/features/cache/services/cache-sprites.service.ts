import { HttpClient } from "@angular/common/http";
import { inject, Service, type Signal } from "@angular/core";
import type { Observable } from "rxjs";
import { environment } from "../../../../environments/environment";
import type { DefinitionSummaryDto, SearchResultDto, SpriteMutationResultDto } from "./cache.models";
import { cacheDefinitionResource, cacheQueryResource } from "./cache-resource";

@Service()
export class CacheSpritesService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = `${environment.cacheApiUrl}types/sprites`;

    definitionResource(id: Signal<number | null>) {
        return cacheDefinitionResource<unknown>(this.baseUrl, id);
    }

    rangeResource(range: Signal<{ startId: number; endIdExclusive: number } | null>) {
        return cacheQueryResource<unknown[]>(`${this.baseUrl}/range`, range);
    }

    searchResource(search: Signal<{ query: string; offset: number; limit: number } | null>) {
        return cacheQueryResource<SearchResultDto<DefinitionSummaryDto>>(`${this.baseUrl}/search`, search);
    }

    getSprite(id: number): Observable<unknown> {
        return this.http.get<unknown>(`${this.baseUrl}/${id}`);
    }

    searchSprites(query: string, offset: number, limit: number): Observable<SearchResultDto<unknown>> {
        return this.http.get<SearchResultDto<unknown>>(`${this.baseUrl}/search`, {
            params: {
                query,
                offset,
                limit,
            },
        });
    }

    getSpriteImage(id: number, frame: number): Observable<Blob> {
        return this.http.get(`${this.baseUrl}/${id}/image`, {
            params: { frame },
            responseType: "blob",
        });
    }

    replaceSpriteImage(id: number, file: File): Observable<SpriteMutationResultDto> {
        const formData = new FormData();
        formData.append("file", file);
        return this.http.post<SpriteMutationResultDto>(`${this.baseUrl}/${id}/image`, formData);
    }
}
