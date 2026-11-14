import { computed, inject } from "@angular/core";
import { patchState, signalStore, withComputed, withMethods, withProps, withState } from "@ngrx/signals";
import { CacheMapsService } from "../services/cache-maps.service";
import type { GetMapRegionsDto } from "../services/dtos";
import { getCacheErrorMessage } from "./cache-error";

function validRegionPage(offset: number, limit: number): boolean {
    return [offset, limit].every(Number.isSafeInteger) && offset >= 0 && offset <= 65536 && limit >= 1 && limit <= 200;
}

export const CacheMapRegionsStore = signalStore(
    withState({ _page: null as { offset: number; limit: number } | null, _previousPage: null as GetMapRegionsDto | null }),
    withProps((store) => {
        const service = inject(CacheMapsService);
        return { _regions: service.regionsResource(store._page) };
    }),
    withComputed((store) => ({
        regionPage: computed(() => store._regions.hasValue() ? store._regions.value() : store._regions.isLoading() ? store._previousPage() : null),
        rangeLoading: computed(() => store._regions.isLoading()),
        rangeError: computed(() => store._regions.error() ? getCacheErrorMessage(store._regions.error()) : null),
    })),
    withComputed((store) => ({
        rangeResult: computed(() => store.regionPage()?.items ?? null),
        rangePagination: computed(() => {
            const page = store.regionPage();
            return page ? { pageIndex: Math.floor(page.offset / page.limit), pageSize: page.limit } : null;
        }),
    })),
    withMethods((store) => ({
        loadPage(offset: number, limit: number): void {
            if (!validRegionPage(offset, limit)) return;
            const current = store._page();
            if (current?.offset === offset && current.limit === limit) {
                if (!store.rangeLoading()) store._regions.reload();
                return;
            }
            if (store._regions.hasValue()) patchState(store, { _previousPage: store._regions.value() });
            patchState(store, { _page: { offset, limit } });
        },
    })),
);
