import { inject } from "@angular/core";
import { patchState, signalStore, withMethods, withState } from "@ngrx/signals";
import { rxMethod } from "@ngrx/signals/rxjs-interop";
import { firstValueFrom, Observable, pipe, switchMap, tap } from "rxjs";
import type { ModelTexture } from "../../../../../shared/model-viewer/model-scene";
import { CacheMapsService } from "../services/cache-maps.service";
import { withUnderwaterLayer, type MapFloor, type MapLoadRequest, type MapObjectClip, type MapRegionSave, type MapRenderRegion, type MapVisibility, type MapWorldMapRefreshResult, type ObjectRenderAppearance } from "../services/map-render.models";
import { getCacheErrorMessage } from "./cache-error";
import { emptyMapProgress, MapSceneLoad, type MapSceneState } from "./map-scene-load";

interface MapSaveResult {
    saved: MapRegionSave[];
    failed?: MapRegionSave;
    unattempted: readonly MapRegionSave[];
}

function saveLabel(save: MapRegionSave): string {
    return `region ${save.regionId} ${save.kind === "terrain" ? "terrain" : "objects"}`;
}

async function writeRegion(service: CacheMapsService, save: MapRegionSave, unreadable: boolean): Promise<void> {
    if (save.kind === "terrain") {
        await firstValueFrom(service.saveTerrain(save.regionId, save.tiles, save.expectedVersion));
    } else {
        if (unreadable) {
            throw new Error("Load this landscape with the correct region XTEA keys before publishing.");
        }
        await firstValueFrom(service.saveLandscape(save.regionId, save.objects, save.expectedVersion, save.xteaKeys));
    }
}

function saveFailure(save: MapRegionSave, error: unknown, saved: readonly MapRegionSave[], unattempted: readonly MapRegionSave[]): string {
    const progress = saved.length ? ` Already saved: ${saved.map(saveLabel).join(", ")}.` : "";
    const skipped = unattempted.length ? ` Not attempted: ${unattempted.map(saveLabel).join(", ")}.` : "";
    const status = (error as { status?: number }).status;
    const recovery = status === 409 ? " Review the latest cache state before applying this edit again."
        : status === 0 || (status !== undefined && status >= 500) ? " The write could not be confirmed. Check the cache before reapplying this edit."
        : status === 422 && save.kind === "landscape" ? " Check the XTEA keys for this region." : "";
    return `Could not save ${saveLabel(save)}. ${getCacheErrorMessage(error)}${progress}${skipped}${recovery}`;
}

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
            async saveRegions(saves: readonly MapRegionSave[], unverifiedKeys?: ReadonlySet<number>): Promise<MapSaveResult | null> {
                if (store.saving()) return null;
                patchState(store, { saving: true, saveError: null });
                const saved: MapRegionSave[] = [];
                for (const save of saves) {
                    try {
                        const unreadable = unverifiedKeys?.has(save.regionId) || store.regions().find(region => region.id === save.regionId)?.landscapeStatus === "failed";
                        await writeRegion(service, save, unreadable);
                        saved.push(save);
                    } catch (error) {
                        const unattempted = saves.slice(saved.length + 1);
                        patchState(store, { saving: false, saveError: saveFailure(save, error, saved, unattempted) });
                        return { saved, failed: save, unattempted };
                    }
                }
                patchState(store, { saving: false });
                return { saved, unattempted: [] };
            },
            async reloadRegion(id: number, kinds: readonly MapRegionSave["kind"][], keys?: number[], preserveLandscapeVersion?: number): Promise<string | null> {
                const selection = active;
                try {
                    const fresh = withUnderwaterLayer(await firstValueFrom(service.render(id, keys)));
                    const unavailable = kinds.find(kind => fresh[`${kind}Status`] !== "available");
                    if (unavailable === "landscape") return `Region ${id} landscape is unreadable. Check the four XTEA keys for this region.`;
                    if (unavailable) return `Region ${id} terrain is unreadable.`;
                    if (preserveLandscapeVersion !== undefined && fresh.landscapeVersion !== preserveLandscapeVersion) {
                        return `Region ${id} landscape has changed. Review the latest cache state before applying this draft again.`;
                    }
                    const current = store.regions().find(region => region.id === id);
                    const keepPlacements = current?.landscapeStatus === "available" && current.landscapeVersion === fresh.landscapeVersion;
                    return selection?.replaceKinds(fresh, kinds, keepPlacements) ? null : `Region ${id} is no longer loaded.`;
                } catch (error) {
                    return `Could not reload region ${id}. ${getCacheErrorMessage(error)}`;
                }
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
