import { HttpClient } from "@angular/common/http";
import { inject, Service, type Signal } from "@angular/core";
import { catchError, type Observable, shareReplay, tap, throwError } from "rxjs";
import { environment } from "../../../../environments/environment";
import type { DefinitionSummaryDto, SearchResultDto, SpriteMutationResultDto } from "./cache.models";
import { cacheDefinitionResource, cacheQueryResource } from "./cache-resource";

@Service()
export class CacheSpritesService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = `${environment.cacheApiUrl}types/sprites`;
    private readonly frameRequests = new Map<string, Observable<Blob>>();

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
        const key = `${id}:${frame}`;
        let request = this.frameRequests.get(key);
        if (!request) {
            request = this.http.get(`${this.baseUrl}/${id}/image`, { params: { frame }, responseType: "blob" }).pipe(
                catchError((error: unknown) => {
                    this.frameRequests.delete(key);
                    return throwError(() => error);
                }),
                shareReplay({ bufferSize: 1, refCount: true })
            );
            this.frameRequests.set(key, request);
        }
        return request;
    }

    replaceSpriteImage(id: number, file: File): Observable<SpriteMutationResultDto> {
        const formData = new FormData();
        formData.append("file", file);
        return this.http.post<SpriteMutationResultDto>(`${this.baseUrl}/${id}/image`, formData).pipe(tap(() => this.frameRequests.clear()));
    }
}
