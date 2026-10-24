import { computed, inject } from "@angular/core";
import { patchState, signalStore, withComputed, withMethods, withProps, withState } from "@ngrx/signals";
import { CacheArchivesService } from "../services/cache-archives.service";
import { getCacheErrorMessage } from "./cache-error";

export const CacheArchivesStore = signalStore(
    withState({ _enabled: false }),
    withProps((store) => ({ _archives: inject(CacheArchivesService).archiveSizesResource(store._enabled) })),
    withComputed((store) => ({
        archiveSizes: computed(() => (store._archives.hasValue() ? store._archives.value() : null)),
        loading: computed(() => store._archives.isLoading()),
        error: computed(() => (store._archives.error() ? getCacheErrorMessage(store._archives.error()) : null)),
    })),
    withMethods((store) => ({
        loadArchiveSizes(): void {
            if (store._enabled()) store._archives.reload();
            else patchState(store, { _enabled: true });
        },
    }))
);
