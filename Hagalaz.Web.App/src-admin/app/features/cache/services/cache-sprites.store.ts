import { HttpClient } from "@angular/common/http";
import { inject } from "@angular/core";
import { pipe, switchMap, tap, catchError, of } from "rxjs";
import { signalStore, withState, withMethods, patchState } from "@ngrx/signals";
import { rxMethod } from "@ngrx/signals/rxjs-interop";
import { environment } from "../../../../environments/environment";
import { SearchResultDto, SpriteMutationResultDto } from "./cache.models";

export interface CacheSpritesState {
    spriteInfo: any | null;
    searchResult: SearchResultDto<any> | null;
    mutationResult: SpriteMutationResultDto | null;
    imageUrl: string | null;
    error: string | null;
    loading: boolean;
}

const initialState: CacheSpritesState = {
    spriteInfo: null,
    searchResult: null,
    mutationResult: null,
    imageUrl: null,
    error: null,
    loading: false,
};

export const CacheSpritesStore = signalStore(
    { providedIn: "root" },
    withState(initialState),
    withMethods((store, http = inject(HttpClient)) => {
        const handleError = (error: unknown) => {
            const response = error as { error?: { detail?: string; title?: string }; message?: string };
            patchState(store, { error: response.error?.detail ?? response.error?.title ?? response.message ?? "Request failed.", loading: false });
            return of(null);
        };

        return {
            loadSprite: rxMethod<number>(
                pipe(
                    tap(() => patchState(store, { loading: true, error: null })),
                    switchMap(id => 
                        http.get<any>(`${environment.cacheApiUrl}types/sprites/${id}`).pipe(
                            tap(spriteInfo => patchState(store, { spriteInfo, loading: false })),
                            catchError(handleError)
                        )
                    )
                )
            ),

            searchSprites: rxMethod<{ query: string; offset: number; limit: number }>(
                pipe(
                    tap(() => patchState(store, { loading: true, error: null })),
                    switchMap(({ query, offset, limit }) => 
                        http.get<SearchResultDto<any>>(`${environment.cacheApiUrl}types/sprites/search`, {
                            params: { query, offset, limit }
                        }).pipe(
                            tap(searchResult => patchState(store, { searchResult, loading: false })),
                            catchError(handleError)
                        )
                    )
                )
            ),

            previewSpriteImage: rxMethod<{ id: number; frame: number }>(
                pipe(
                    tap(() => patchState(store, { loading: true, error: null })),
                    switchMap(({ id, frame }) => 
                        http.get(`${environment.cacheApiUrl}types/sprites/${id}/image`, {
                            params: { frame },
                            responseType: "blob",
                        }).pipe(
                            tap(blob => {
                                const currentUrl = store.imageUrl();
                                if (currentUrl) {
                                    URL.revokeObjectURL(currentUrl);
                                }
                                patchState(store, { imageUrl: URL.createObjectURL(blob), loading: false });
                            }),
                            catchError(handleError)
                        )
                    )
                )
            ),

            uploadSpriteImage: rxMethod<{ id: number; file: File }>(
                pipe(
                    tap(() => patchState(store, { loading: true, error: null })),
                    switchMap(({ id, file }) => {
                        const formData = new FormData();
                        formData.append("file", file);
                        return http.post<SpriteMutationResultDto>(`${environment.cacheApiUrl}types/sprites/${id}/image`, formData).pipe(
                            tap(mutationResult => patchState(store, { mutationResult, loading: false })),
                            catchError(handleError)
                        );
                    })
                )
            ),

            clearImageUrl(): void {
                const current = store.imageUrl();
                if (current) {
                    URL.revokeObjectURL(current);
                    patchState(store, { imageUrl: null });
                }
            },

            setError(error: string | null): void {
                patchState(store, { error });
            }
        };
    })
);
