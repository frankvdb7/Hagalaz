import { inject } from "@angular/core";
import { patchState, signalStore, withMethods, withState } from "@ngrx/signals";
import { rxMethod } from "@ngrx/signals/rxjs-interop";
import { firstValueFrom, Observable, pipe, switchMap, tap } from "rxjs";
import type { ModelTexture } from "../../../../../shared/model-viewer/model-scene";
import { CacheMapsService } from "../services/cache-maps.service";
import type { MapFloor, MapLoadRequest, MapObjectClip, MapRegionSave, MapRenderRegion, MapVisibility, MapWorldMapRefreshResult, ObjectRenderAppearance } from "../services/map-render.models";
import { getCacheErrorMessage } from "./cache-error";
import { emptyMapProgress, MapSceneLoad, type MapSceneState } from "./map-scene-load";

export const CacheMapSceneStore = signalStore(
    withState({
        regions: [] as MapRenderRegion[],
        appearances: new Map<string, ObjectRenderAppearance>(),
        animations: new Map<number, MapObjectClip>() as ReadonlyMap<number, MapObjectClip>,
        textures: new Map<number, ModelTexture>(),
        environmentFaces: null as readonly ModelTexture[] | null,
        warnings: [] as string[],
        loading: false,
        progress: emptyMapProgress(),
        saving: false,
        saveError: null as string | null,
        worldMapRefreshing: false,
        worldMapError: null as string | null,
        floors: [] as MapFloor[],
        floorsLoading: false,
        floorsError: null as string | null,
    }),
    withMethods((store, service: CacheMapsService = inject(CacheMapsService)) => {
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
            /** Saves in order and stops at the first failure; every save holds absolute values, so saving again is safe. */
            async saveRegions(saves: readonly MapRegionSave[]): Promise<boolean> {
                if (store.saving()) return false;
                patchState(store, { saving: true, saveError: null });
                const saved: string[] = [];
                for (const save of saves) {
                    const label = `region ${save.regionId} ${save.kind === "terrain" ? "terrain" : "objects"}`;
                    try {
                        await firstValueFrom<unknown>(save.kind === "terrain" ? service.saveTerrain(save.regionId, save.tiles) : service.saveLandscape(save.regionId, save.objects));
                        saved.push(label);
                    } catch (error) {
                        const progress = saved.length ? ` Already saved: ${saved.join(", ")}.` : "";
                        patchState(store, { saving: false, saveError: `Could not save ${label}. ${getCacheErrorMessage(error)}${progress}` });
                        return false;
                    }
                }
                patchState(store, { saving: false });
                return true;
            },
            /** Kept apart from saving, because a failure here leaves the saved map edits intact. */
            async refreshWorldMap(regions: readonly number[]): Promise<MapWorldMapRefreshResult | null> {
                if (store.worldMapRefreshing()) return null;
                patchState(store, { worldMapRefreshing: true, worldMapError: null });
                try {
                    const result = await firstValueFrom(service.refreshWorldMap(regions));
                    patchState(store, { worldMapRefreshing: false });
                    return result;
                } catch (error) {
                    patchState(store, { worldMapRefreshing: false, worldMapError: `The world map was not updated. ${getCacheErrorMessage(error)}` });
                    return null;
                }
            },
            async loadFloors(): Promise<void> {
                if (store.floors().length || store.floorsLoading()) return;
                patchState(store, { floorsLoading: true, floorsError: null });
                try {
                    patchState(store, { floors: await firstValueFrom(service.floors()), floorsLoading: false });
                } catch (error) {
                    patchState(store, { floorsLoading: false, floorsError: getCacheErrorMessage(error) });
                }
            },
            setVisibility(value: MapVisibility): void {
                visibility = value;
                active?.setVisibility(value);
            },
        };
    })
);
