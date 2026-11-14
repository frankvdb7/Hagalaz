import { inject } from "@angular/core";
import { tapResponse } from "@ngrx/operators";
import { patchState, signalStore, withMethods, withState } from "@ngrx/signals";
import { rxMethod } from "@ngrx/signals/rxjs-interop";
import { EMPTY, exhaustMap, filter, pipe, switchMap } from "rxjs";
import type { ReplaceSpriteImageResultDto } from "../services/cache.models";
import { CacheSpritesService } from "../services/cache-sprites.service";
import { getCacheErrorMessage } from "./cache-error";

export const CacheSpritesStore = signalStore(
    withState({
        mutationResult: null as ReplaceSpriteImageResultDto | null,
        mutationId: null as number | null,
        imageBlob: null as Blob | null,
        imageId: null as number | null,
        imageFrame: null as number | null,
        imageDescription: "",
        previewLoading: false,
        saving: false,
        previewError: null as string | null,
        mutationError: null as string | null,
    }),
    withMethods((store, service = inject(CacheSpritesService)) => ({
        clearError(): void {
            patchState(store, { previewError: null, mutationError: null });
        },
        previewSpriteImage: rxMethod<{ id: number; frame: number } | null>(
            pipe(
                switchMap((selection) => {
                    if (!selection) {
                        patchState(store, { imageBlob: null, imageId: null, imageFrame: null, imageDescription: "", previewError: null });
                        return EMPTY;
                    }
                    const { id, frame } = selection;
                    patchState(store, { previewLoading: true, previewError: null });
                    return service.getSpriteImage(id, frame).pipe(
                        tapResponse({
                            next: (imageBlob) =>
                                patchState(store, { imageBlob, imageId: id, imageFrame: frame, imageDescription: `Sprite #${id}, frame ${frame}` }),
                            error: (error: unknown) => patchState(store, { previewError: getCacheErrorMessage(error) }),
                            finalize: () => patchState(store, { previewLoading: false }),
                        })
                    );
                })
            )
        ),
        uploadSpriteImage: rxMethod<{ id: number; file: File | null }>(
            pipe(
                filter(() => !store.saving()),
                filter((value): value is { id: number; file: File } => {
                    if (value.file) return true;
                    patchState(store, { mutationId: value.id, mutationError: "Select a PNG file before uploading." });
                    return false;
                }),
                exhaustMap(({ id, file }) => {
                    patchState(store, { mutationId: id, saving: true, mutationError: null, mutationResult: null });
                    return service.replaceSpriteImage(id, file).pipe(
                        tapResponse({
                            next: (mutationResult) => patchState(store, { mutationResult }),
                            error: (error: unknown) => patchState(store, { mutationError: getCacheErrorMessage(error) }),
                            finalize: () => patchState(store, { saving: false }),
                        })
                    );
                })
            )
        ),
    }))
);
