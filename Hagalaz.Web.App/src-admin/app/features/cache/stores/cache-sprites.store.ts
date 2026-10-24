import { inject } from "@angular/core";
import { tapResponse } from "@ngrx/operators";
import { patchState, signalStore, withMethods, withState } from "@ngrx/signals";
import { rxMethod } from "@ngrx/signals/rxjs-interop";
import { exhaustMap, filter, type Observable, pipe } from "rxjs";
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
    withMethods((store, service = inject(CacheSpritesService)) => {
        function imageRequest<T>(source: Observable<T>, next: (value: T) => void) {
            patchState(store, { loading: true, error: null });
            return source.pipe(
                tapResponse({
                    next,
                    error: (error: unknown) => patchState(store, { error: getCacheErrorMessage(error) }),
                    finalize: () => patchState(store, { loading: false }),
                })
            );
        }
        return {
            clearError(): void {
                patchState(store, { error: null });
            },
            loadSprite: rxMethod<number>(
                pipe(
                    filter(() => !store.loading()),
                    exhaustMap((id) => imageRequest(service.getSprite(id), (spriteInfo) => patchState(store, { spriteInfo })))
                )
            ),
            searchSprites: rxMethod<{ query: string; offset: number; limit: number }>(
                pipe(
                    filter(() => !store.loading()),
                    exhaustMap(({ query, offset, limit }) =>
                        imageRequest(service.searchSprites(query, offset, limit), (searchResult) => patchState(store, { searchResult }))
                    )
                )
            ),
            previewSpriteImage: rxMethod<{ id: number; frame: number }>(
                pipe(
                    filter(() => !store.loading()),
                    exhaustMap(({ id, frame }) =>
                        imageRequest(service.getSpriteImage(id, frame), (imageBlob) =>
                            patchState(store, { imageBlob, imageDescription: `Sprite #${id}, frame ${frame}` })
                        )
                    )
                )
            ),
            uploadSpriteImage: rxMethod<{ id: number; file: File | null }>(
                pipe(
                    filter(() => !store.loading()),
                    filter((value): value is { id: number; file: File } => {
                        if (value.file) return true;
                        patchState(store, { error: "Select a PNG file before uploading." });
                        return false;
                    }),
                    exhaustMap(({ id, file }) => imageRequest(service.replaceSpriteImage(id, file), (mutationResult) => patchState(store, { mutationResult })))
                )
            ),
        };
    })
);
