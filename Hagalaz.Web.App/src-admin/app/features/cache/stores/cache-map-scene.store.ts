import { inject } from "@angular/core";
import { patchState, signalStore, withMethods, withState } from "@ngrx/signals";
import { rxMethod } from "@ngrx/signals/rxjs-interop";
import { Observable, pipe, switchMap, tap } from "rxjs";
import type { ModelTexture } from "../../../../../shared/model-viewer/model-scene";
import { CacheMapsService } from "../services/cache-maps.service";
import type { MapLoadRequest, MapRenderRegion, MapVisibility, ObjectRenderAppearance } from "../services/map-render.models";
import { emptyMapProgress, MapSceneLoad, type MapSceneState } from "./map-scene-load";

export const CacheMapSceneStore = signalStore(
    withState({
        regions: [] as MapRenderRegion[],
        appearances: new Map<string, ObjectRenderAppearance>(),
        textures: new Map<number, ModelTexture>(),
        environmentFaces: null as readonly ModelTexture[] | null,
        warnings: [] as string[],
        loading: false,
        progress: emptyMapProgress(),
    }),
    withMethods((store, service = inject(CacheMapsService)) => {
        let active: MapSceneLoad | undefined;
        let visibility: MapVisibility = { planes: [true, false, false, false], landscape: true };
        return {
            load: rxMethod<MapLoadRequest>(
                pipe(
                    switchMap(
                        (request) =>
                            new Observable<MapSceneState>((subscriber) => {
                                const selection = new MapSceneLoad(service, request, visibility, (state) => subscriber.next(state));
                                active = selection;
                                selection.start();
                                return () => {
                                    selection.dispose();
                                    if (active === selection) active = undefined;
                                };
                            })
                    ),
                    tap((state) => patchState(store, state))
                )
            ),
            setVisibility(value: MapVisibility): void {
                visibility = value;
                active?.setVisibility(value);
            },
        };
    })
);
