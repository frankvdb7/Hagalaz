import type { HttpResourceRef } from "@angular/common/http";
import { computed, type Signal } from "@angular/core";
import { tapResponse } from "@ngrx/operators";
import { patchState, signalStoreFeature, withComputed, withMethods, withProps, withState } from "@ngrx/signals";
import { rxMethod } from "@ngrx/signals/rxjs-interop";
import { exhaustMap, filter, type Observable, pipe } from "rxjs";
import type { DefinitionSummaryDto, SearchResultDto, TypeMutationResultDto } from "../services/cache.models";
import { getCacheErrorMessage } from "./cache-error";

export function withCacheDefinition(resource: (id: Signal<number | null>) => HttpResourceRef<unknown>) {
    return signalStoreFeature(
        withState({ _id: null as number | null, _saving: false, _mutationError: null as string | null, mutationResult: null as TypeMutationResultDto | null }),
        withProps((store) => ({ _definition: resource(store._id) })),
        withComputed((store) => ({
            byIdResult: computed(() => (store._definition.hasValue() ? store._definition.value() : null)),
            definitionId: computed(() => (store._definition.hasValue() ? store._id() : null)),
            loading: computed(() => store._definition.isLoading() || store._saving()),
            error: computed(() => store._mutationError() ?? (store._definition.error() ? getCacheErrorMessage(store._definition.error()) : null)),
        })),
        withMethods((store) => ({
            _saveDefinition: rxMethod<{ id: number; changes: Record<string, unknown>; request: () => Observable<TypeMutationResultDto> }>(
                pipe(
                    filter(() => !store.loading()),
                    exhaustMap(({ id, changes, request }) => {
                        patchState(store, { _saving: true, _mutationError: null, mutationResult: null });
                        return request().pipe(
                            tapResponse({
                                next: (mutationResult) => {
                                    if (store._definition.hasValue() && store._id() === id) {
                                        const current = store._definition.value();
                                        if (current !== null && typeof current === "object") store._definition.set({ ...current, ...changes });
                                    }
                                    patchState(store, { mutationResult });
                                },
                                error: (error: unknown) => patchState(store, { _mutationError: getCacheErrorMessage(error) }),
                                finalize: () => patchState(store, { _saving: false }),
                            })
                        );
                    })
                )
            ),
            loadById(id: number): void {
                if (!Number.isSafeInteger(id) || id < 0) return;
                patchState(store, { mutationResult: null, _mutationError: null });
                if (id === store._id()) store._definition.reload();
                else patchState(store, { _id: id });
            },
        }))
    );
}

export function withCacheRange(resource: (range: Signal<{ startId: number; endIdExclusive: number } | null>) => HttpResourceRef<unknown[] | undefined>) {
    return signalStoreFeature(
        withState({ _range: null as { startId: number; endIdExclusive: number } | null }),
        withProps((store) => ({ _rangeResource: resource(store._range) })),
        withComputed((store) => ({
            rangeResult: computed(() => (store._rangeResource.hasValue() ? store._rangeResource.value() : null)),
            rangeLoading: computed(() => store._rangeResource.isLoading()),
            rangeError: computed(() => (store._rangeResource.error() ? getCacheErrorMessage(store._rangeResource.error()) : null)),
        })),
        withMethods((store) => ({
            loadRange(startId: number, endIdExclusive: number): void {
                patchState(store, { _range: { startId, endIdExclusive } });
            },
        }))
    );
}

export function withCacheSearch(
    resource: (search: Signal<{ query: string; offset: number; limit: number } | null>) => HttpResourceRef<SearchResultDto<DefinitionSummaryDto> | undefined>
) {
    return signalStoreFeature(
        withState({ _search: null as { query: string; offset: number; limit: number } | null }),
        withProps((store) => ({ _searchResource: resource(store._search) })),
        withComputed((store) => ({
            searchResult: computed(() => (store._searchResource.hasValue() ? store._searchResource.value() : null)),
            searchLoading: computed(() => store._searchResource.isLoading()),
            searchError: computed(() => (store._searchResource.error() ? getCacheErrorMessage(store._searchResource.error()) : null)),
        })),
        withMethods((store) => ({
            search(query: string, offset: number, limit: number): void {
                patchState(store, { _search: { query, offset, limit } });
            },
        }))
    );
}
