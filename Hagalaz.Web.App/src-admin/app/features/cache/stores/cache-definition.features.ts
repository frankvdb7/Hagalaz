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
            saving: computed(() => store._saving()),
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
        withState({
            _range: null as { startId: number; endIdExclusive: number } | null,
            _rangePageSize: 10,
            _previousRangeResult: null as unknown[] | null,
            _previousRangeRequest: null as { startId: number; pageSize: number } | null,
        }),
        withProps((store) => ({ _rangeResource: resource(store._range) })),
        withComputed((store) => ({
            rangeResult: computed(() => {
                if (store._rangeResource.isLoading()) return store._previousRangeResult();
                return store._rangeResource.hasValue() ? (store._rangeResource.value() ?? null) : null;
            }),
            rangePagination: computed(() => {
                if (store._rangeResource.isLoading()) {
                    const previous = store._previousRangeRequest();
                    return previous ? { pageIndex: Math.floor(previous.startId / previous.pageSize), pageSize: previous.pageSize } : null;
                }
                const range = store._range();
                const pageSize = store._rangePageSize();
                return range ? { pageIndex: Math.floor(range.startId / pageSize), pageSize } : null;
            }),
            rangeLoading: computed(() => store._rangeResource.isLoading()),
            rangeError: computed(() => (store._rangeResource.error() ? getCacheErrorMessage(store._rangeResource.error()) : null)),
        })),
        withMethods((store) => ({
            loadRange(startId: number, endIdExclusive: number, pageSize = Math.max(1, endIdExclusive - startId)): void {
                if (store._rangeResource.hasValue()) {
                    const range = store._range();
                    if (range) {
                        patchState(store, {
                            _previousRangeResult: store._rangeResource.value() ?? null,
                            _previousRangeRequest: { startId: range.startId, pageSize: store._rangePageSize() },
                        });
                    }
                }
                patchState(store, { _range: { startId, endIdExclusive }, _rangePageSize: pageSize });
            },
        }))
    );
}

export function withCacheSearch(
    resource: (search: Signal<{ query: string; offset: number; limit: number } | null>) => HttpResourceRef<SearchResultDto<DefinitionSummaryDto> | undefined>
) {
    return signalStoreFeature(
        withState({
            _search: null as { query: string; offset: number; limit: number } | null,
            _previousSearchResult: null as { request: { query: string; offset: number; limit: number }; result: SearchResultDto<DefinitionSummaryDto> } | null,
        }),
        withProps((store) => ({ _searchResource: resource(store._search) })),
        withComputed((store) => ({
            searchResult: computed(() => {
                const request = store._search();
                const previous = store._previousSearchResult();
                if (store._searchResource.isLoading()) return previous && request && previous.request.query === request.query ? previous.result : null;
                return store._searchResource.hasValue() ? (store._searchResource.value() ?? null) : null;
            }),
            searchPagination: computed(() => {
                const request = store._search();
                if (!request) return null;
                const previous = store._previousSearchResult();
                const displayedRequest =
                    store._searchResource.isLoading() && previous?.request.query === request.query ? previous.request : request;
                return { pageIndex: Math.floor(displayedRequest.offset / displayedRequest.limit), pageSize: displayedRequest.limit };
            }),
            searchLoading: computed(() => store._searchResource.isLoading()),
            searchError: computed(() => (store._searchResource.error() ? getCacheErrorMessage(store._searchResource.error()) : null)),
        })),
        withMethods((store) => ({
            search(query: string, offset: number, limit: number): void {
                const request = store._search();
                if (store._searchResource.hasValue() && request) {
                    const result = store._searchResource.value();
                    if (result) patchState(store, { _previousSearchResult: { request, result } });
                }
                patchState(store, { _search: { query, offset, limit } });
            },
        }))
    );
}
