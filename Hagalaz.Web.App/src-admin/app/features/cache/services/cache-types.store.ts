import { HttpClient } from "@angular/common/http";
import { inject } from "@angular/core";
import { pipe, switchMap, tap, catchError, of, Observable } from "rxjs";
import { signalStore, withState, withMethods, patchState } from "@ngrx/signals";
import { rxMethod } from "@ngrx/signals/rxjs-interop";
import { environment } from "../../../../environments/environment";
import { ArchiveSizesDto, SearchResultDto, TypeKind, TypeMutationResultDto } from "./cache.models";

export interface CacheTypesState {
    archiveSizes: ArchiveSizesDto | null;
    byIdResult: unknown | null;
    rangeResult: unknown[] | null;
    searchResult: SearchResultDto<unknown> | null;
    mutationResult: TypeMutationResultDto | null;
    error: string | null;
    loading: boolean;
}

const initialState: CacheTypesState = {
    archiveSizes: null,
    byIdResult: null,
    rangeResult: null,
    searchResult: null,
    mutationResult: null,
    error: null,
    loading: false,
};

export const CacheTypesStore = signalStore(
    { providedIn: "root" },
    withState(initialState),
    withMethods((store, http = inject(HttpClient)) => {
        const handleError = (error: unknown) => {
            const response = error as { error?: { detail?: string; title?: string }; message?: string };
            patchState(store, { error: response.error?.detail ?? response.error?.title ?? response.message ?? "Request failed.", loading: false });
            return of(null);
        };

        return {
            loadArchiveSizes: rxMethod<void>(
                pipe(
                    tap(() => patchState(store, { loading: true, error: null })),
                    switchMap(() => 
                        http.get<ArchiveSizesDto>(`${environment.cacheApiUrl}types/archive-sizes`).pipe(
                            tap(archiveSizes => patchState(store, { archiveSizes, loading: false })),
                            catchError(handleError)
                        )
                    )
                )
            ),

            loadById: rxMethod<{ kind: TypeKind; id: number }>(
                pipe(
                    tap(() => patchState(store, { loading: true, error: null })),
                    switchMap(({ kind, id }) => 
                        http.get<unknown>(`${environment.cacheApiUrl}types/${kind}/${id}`).pipe(
                            tap(byIdResult => patchState(store, { byIdResult, loading: false })),
                            catchError(handleError)
                        )
                    )
                )
            ),

            loadRange: rxMethod<{ kind: TypeKind; startId: number; endIdExclusive: number }>(
                pipe(
                    tap(() => patchState(store, { loading: true, error: null })),
                    switchMap(({ kind, startId, endIdExclusive }) => 
                        http.get<unknown[]>(`${environment.cacheApiUrl}types/${kind}/range`, {
                            params: { startId, endIdExclusive }
                        }).pipe(
                            tap(rangeResult => patchState(store, { rangeResult, loading: false })),
                            catchError(handleError)
                        )
                    )
                )
            ),

            search: rxMethod<{ kind: string; query: string; offset: number; limit: number }>(
                pipe(
                    tap(() => patchState(store, { loading: true, error: null })),
                    switchMap(({ kind, query, offset, limit }) => 
                        http.get<SearchResultDto<unknown>>(`${environment.cacheApiUrl}types/${kind}/search`, {
                            params: { query, offset, limit }
                        }).pipe(
                            tap(searchResult => patchState(store, { searchResult, loading: false })),
                            catchError(handleError)
                        )
                    )
                )
            ),

            updateNpcCombatLevel: rxMethod<{ id: number; combatLevel: number }>(
                pipe(
                    tap(() => patchState(store, { loading: true, error: null })),
                    switchMap(({ id, combatLevel }) => 
                        http.post<TypeMutationResultDto>(`${environment.cacheApiUrl}types/npcs/${id}/combat-level`, { combatLevel }).pipe(
                            tap(mutationResult => patchState(store, { mutationResult, loading: false })),
                            catchError(handleError)
                        )
                    )
                )
            ),

            updateObjectName: rxMethod<{ id: number; name: string }>(
                pipe(
                    tap(() => patchState(store, { loading: true, error: null })),
                    switchMap(({ id, name }) => 
                        http.post<TypeMutationResultDto>(`${environment.cacheApiUrl}types/objects/${id}/name`, { name }).pipe(
                            tap(mutationResult => patchState(store, { mutationResult, loading: false })),
                            catchError(handleError)
                        )
                    )
                )
            ),

            updateVarpBit: rxMethod<{ id: number; configId: number; bitLength: number; bitOffset: number }>(
                pipe(
                    tap(() => patchState(store, { loading: true, error: null })),
                    switchMap(({ id, configId, bitLength, bitOffset }) => 
                        http.post<TypeMutationResultDto>(`${environment.cacheApiUrl}types/varp-bits/${id}`, { configId, bitLength, bitOffset }).pipe(
                            tap(mutationResult => patchState(store, { mutationResult, loading: false })),
                            catchError(handleError)
                        )
                    )
                )
            ),

            updateConfigDefinition: rxMethod<{ id: number; defaultValue: number; valueType: string }>(
                pipe(
                    tap(() => patchState(store, { loading: true, error: null })),
                    switchMap(({ id, defaultValue, valueType }) => 
                        http.post<TypeMutationResultDto>(`${environment.cacheApiUrl}types/config-definitions/${id}`, { defaultValue, valueType }).pipe(
                            tap(mutationResult => patchState(store, { mutationResult, loading: false })),
                            catchError(handleError)
                        )
                    )
                )
            )
        };
    })
);
