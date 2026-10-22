import { inject } from "@angular/core";
import { patchState, signalStore, withMethods, withState } from "@ngrx/signals";
import { firstValueFrom } from "rxjs";
import type { ArchiveSizesDto, SearchResultDto, TypeKind, TypeMutationResultDto } from "../services/cache.models";
import { CacheTypesService } from "../services/cache-types.service";
import { getCacheErrorMessage } from "./cache-error";

interface CacheTypesState {
    archiveSizes: ArchiveSizesDto | null;
    byIdResult: unknown;
    rangeResult: unknown[] | null;
    searchResult: SearchResultDto<unknown> | null;
    mutationResult: TypeMutationResultDto | null;
    error: string | null;
    loading: boolean;
}

export const CacheTypesStore = signalStore(
    withState<CacheTypesState>({
        archiveSizes: null,
        byIdResult: null,
        rangeResult: null,
        searchResult: null,
        mutationResult: null,
        error: null,
        loading: false,
    }),
    withMethods((store, typesService = inject(CacheTypesService)) => {
        async function run(action: () => Promise<void>): Promise<void> {
            if (store.loading()) return;
            patchState(store, { error: null, loading: true });
            try {
                await action();
            } catch (error: unknown) {
                patchState(store, { error: getCacheErrorMessage(error) });
            } finally {
                patchState(store, { loading: false });
            }
        }

        return {
            async loadArchiveSizes(): Promise<void> {
                await run(async () => patchState(store, { archiveSizes: await firstValueFrom(typesService.getArchiveSizes()) }));
            },
            async loadById(kind: TypeKind, id: number): Promise<void> {
                await run(async () => patchState(store, { byIdResult: await firstValueFrom(typesService.getById(kind, id)) }));
            },
            async loadRange(kind: TypeKind, startId: number, endIdExclusive: number): Promise<void> {
                await run(async () => patchState(store, { rangeResult: await firstValueFrom(typesService.getRange(kind, startId, endIdExclusive)) }));
            },
            async search(kind: "items" | "npcs" | "sprites", query: string, offset: number, limit: number): Promise<void> {
                await run(async () => patchState(store, { searchResult: await firstValueFrom(typesService.search(kind, query, offset, limit)) }));
            },
            async setNpcCombatLevel(id: number, combatLevel: number): Promise<void> {
                await run(async () => patchState(store, { mutationResult: await firstValueFrom(typesService.updateNpcCombatLevel(id, combatLevel)) }));
            },
            async setObjectName(id: number, name: string): Promise<void> {
                await run(async () => patchState(store, { mutationResult: await firstValueFrom(typesService.updateObjectName(id, name)) }));
            },
            async setVarpBit(id: number, configId: number, bitLength: number, bitOffset: number): Promise<void> {
                await run(async () =>
                    patchState(store, { mutationResult: await firstValueFrom(typesService.updateVarpBit(id, configId, bitLength, bitOffset)) })
                );
            },
            async setConfigDefinition(id: number, defaultValue: number, valueType: string): Promise<void> {
                await run(async () =>
                    patchState(store, { mutationResult: await firstValueFrom(typesService.updateConfigDefinition(id, defaultValue, valueType)) })
                );
            },
        };
    })
);
