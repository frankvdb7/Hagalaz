import type { GetSpriteDefinitionDto } from "./dtos";
import { HttpClient } from "@angular/common/http";
import { inject, Service } from "@angular/core";
import { catchError, type Observable, shareReplay, tap, throwError } from "rxjs";
import type { ReplaceSpriteImageResultDto } from "./cache.models";
import { CacheSearchableTypeService } from "./cache-resource";

@Service()
export class CacheSpritesService extends CacheSearchableTypeService<GetSpriteDefinitionDto> {
    private readonly http = inject(HttpClient);
    private readonly frameRequests = new Map<string, Observable<Blob>>();

    constructor() {
        super("sprites");
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

    replaceSpriteImage(id: number, file: File): Observable<ReplaceSpriteImageResultDto> {
        const formData = new FormData();
        formData.append("file", file);
        return this.http.post<ReplaceSpriteImageResultDto>(`${this.baseUrl}/${id}/image`, formData).pipe(tap(() => this.frameRequests.clear()));
    }
}
