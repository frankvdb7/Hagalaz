import { HttpClient } from "@angular/common/http";
import { Injectable, inject } from "@angular/core";
import { Observable } from "rxjs";
import { environment } from "../../../../environments/environment";
import { SearchResultDto, SpriteMutationResultDto } from "./cache.models";

@Injectable({ providedIn: "root" })
export class CacheSpritesService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = `${environment.cacheApiUrl}types/sprites`;

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
