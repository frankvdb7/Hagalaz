import { HttpClient } from "@angular/common/http";
import { inject } from "@angular/core";
import { pipe, switchMap, tap, catchError, of } from "rxjs";
import { signalStore, withState, withMethods, patchState } from "@ngrx/signals";
import { rxMethod } from "@ngrx/signals/rxjs-interop";
import { environment } from "../../../../environments/environment";
import { MapDecodeRequest, MapTypeDto } from "./cache.models";

export interface CacheMapsState {
    mapInfo: MapTypeDto | null;
    error: string | null;
    loading: boolean;
}

const initialState: CacheMapsState = {
    mapInfo: null,
    error: null,
    loading: false,
};

export const CacheMapsStore = signalStore(
    { providedIn: "root" },
    withState(initialState),
    withMethods((store, http = inject(HttpClient)) => {
        const handleError = (error: unknown) => {
            const response = error as { error?: { detail?: string; title?: string }; message?: string };
            patchState(store, { error: response.error?.detail ?? response.error?.title ?? response.message ?? "Request failed.", loading: false });
            return of(null);
        };

        return {
            loadMap: rxMethod<number>(
                pipe(
                    tap(() => patchState(store, { loading: true, error: null })),
                    switchMap((id) => 
                        http.get<MapTypeDto>(`${environment.cacheApiUrl}types/maps/${id}`).pipe(
                            tap(mapInfo => patchState(store, { mapInfo, loading: false })),
                            catchError(handleError)
                        )
                    )
                )
            ),

            decodeMap: rxMethod<{ id: number; request: MapDecodeRequest }>(
                pipe(
                    tap(() => patchState(store, { loading: true, error: null })),
                    switchMap(({ id, request }) => 
                        http.post<MapTypeDto>(`${environment.cacheApiUrl}types/maps/${id}/decode`, request).pipe(
                            tap(mapInfo => patchState(store, { mapInfo, loading: false })),
                            catchError(handleError)
                        )
                    )
                )
            ),

            clearMap: () => patchState(store, { mapInfo: null, error: null })
        };
    })
);
