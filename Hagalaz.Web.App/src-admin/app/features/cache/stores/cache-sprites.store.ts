import { inject } from "@angular/core";
import { patchState, signalStore, withMethods, withState } from "@ngrx/signals";
import { firstValueFrom } from "rxjs";
import type { SearchResultDto, SpriteMutationResultDto } from "../services/cache.models";
import { CacheSpritesService } from "../services/cache-sprites.service";
import { getCacheErrorMessage } from "./cache-error";

interface CacheSpritesState {
    spriteInfo: unknown;
    searchResult: SearchResultDto<unknown> | null;
    mutationResult: SpriteMutationResultDto | null;
    imageBlob: Blob | null;
    imageDescription: string;
    error: string | null;
    loading: boolean;
}

export const CacheSpritesStore = signalStore(
    withState<CacheSpritesState>({
        spriteInfo: null,
        searchResult: null,
        mutationResult: null,
        imageBlob: null,
        imageDescription: "",
        error: null,
        loading: false,
    }),
    withMethods((store, spritesService = inject(CacheSpritesService)) => {
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
            clearError(): void {
                patchState(store, { error: null });
            },
            async loadSprite(id: number): Promise<void> {
                await run(async () => patchState(store, { spriteInfo: await firstValueFrom(spritesService.getSprite(id)) }));
            },
            async searchSprites(query: string, offset: number, limit: number): Promise<void> {
                await run(async () => patchState(store, { searchResult: await firstValueFrom(spritesService.searchSprites(query, offset, limit)) }));
            },
            async previewSpriteImage(id: number, frame: number): Promise<void> {
                await run(async () => {
                    const imageBlob = await firstValueFrom(spritesService.getSpriteImage(id, frame));
                    patchState(store, { imageBlob, imageDescription: `Sprite #${id}, frame ${frame}` });
                });
            },
            async uploadSpriteImage(id: number, file: File | null): Promise<void> {
                if (!file) {
                    patchState(store, { error: "Select a PNG file before uploading." });
                    return;
                }
                await run(async () => patchState(store, { mutationResult: await firstValueFrom(spritesService.replaceSpriteImage(id, file)) }));
            },
        };
    })
);
