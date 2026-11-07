import { catchError, EMPTY, finalize, from, mergeMap, Subscription, switchMap, tap } from "rxjs";
import type { ModelTexture } from "../../../../../shared/model-viewer/model-scene";
import type { CacheMapsService } from "../services/cache-maps.service";
import {
    mapAppearanceKey,
    selectedMapIds,
    type MapLoadProgress,
    type MapLoadRequest,
    type MapPlacement,
    type MapRenderRegion,
    type MapVisibility,
    type ObjectAppearanceResult,
    type ObjectRenderAppearance,
} from "../services/map-render.models";
import { getCacheErrorMessage } from "./cache-error";

export interface MapSceneState {
    regions: MapRenderRegion[];
    appearances: Map<string, ObjectRenderAppearance>;
    textures: Map<number, ModelTexture>;
    warnings: string[];
    loading: boolean;
    progress: MapLoadProgress;
}
export const emptyMapProgress = (): MapLoadProgress => ({
    regionsSettled: 0,
    regionsTotal: 0,
    appearancesSettled: 0,
    appearancesTotal: 0,
    texturesSettled: 0,
    texturesTotal: 0,
    failures: 0,
});

/** One selection owns requests, publication scheduling and bitmap cleanup. */
export class MapSceneLoad {
    private readonly requests = new Subscription();
    private readonly ids: number[];
    private readonly regions = new Map<number, MapRenderRegion>();
    private readonly appearances = new Map<string, ObjectRenderAppearance>();
    private readonly appearanceTextureIds = new WeakMap<ObjectRenderAppearance, readonly number[]>();
    private readonly textures = new Map<number, ModelTexture>();
    private readonly attemptedAppearances = new Set<string>();
    private readonly attemptedTextures = new Set<number>();
    private readonly settledAppearances = new Set<string>();
    private readonly settledTextures = new Set<number>();
    private readonly warnings = new Set<string>();
    private regionList: MapRenderRegion[] = [];
    private remainingRegions: number;
    private appearanceRequests = 0;
    private textureRequests = 0;
    private cancelled = false;
    private pumping = false;
    private pumpAgain = false;
    private publication?: ReturnType<typeof setTimeout>;
    private appearancesDirty = true;
    private texturesDirty = true;
    private publishedAppearances = new Map<string, ObjectRenderAppearance>();
    private publishedTextures = new Map<number, ModelTexture>();
    private failures = 0;

    constructor(
        private readonly service: CacheMapsService,
        private readonly request: MapLoadRequest,
        private visibility: MapVisibility,
        private readonly publish: (state: MapSceneState) => void
    ) {
        this.ids = selectedMapIds(request);
        this.remainingRegions = this.ids.length;
    }

    start(): void {
        if (!this.ids.length) this.warnings.add("Enter a valid region rectangle of one to three regions on each axis.");
        this.flush();
        this.requests.add(
            from(this.ids)
                .pipe(
                    mergeMap(
                        (id) =>
                            this.service.render(id, this.request.keys[id]).pipe(
                                tap((region) => {
                                    this.regions.set(id, region);
                                    this.regionList = this.ids.flatMap((id) => this.regions.get(id) ?? []);
                                    for (const warning of region.warnings) this.warnings.add(warning);
                                    for (const kind of ["terrain", "landscape"] as const)
                                        if (region[`${kind}Status`] !== "available") this.warnings.add(`Region ${id}: ${kind} ${region[`${kind}Status`]}.`);
                                    this.flush();
                                    this.pump();
                                }),
                                catchError((error) => {
                                    this.fail(`Region ${id}: ${getCacheErrorMessage(error)}`);
                                    return EMPTY;
                                }),
                                finalize(() => {
                                    this.remainingRegions--;
                                    this.pump();
                                })
                            ),
                        4
                    )
                )
                .subscribe()
        );
    }

    setVisibility(visibility: MapVisibility): void {
        this.visibility = visibility;
        this.pump();
        this.flush();
    }

    private interest(): { placements: Map<string, MapPlacement>; textures: Set<number> } {
        const placements = new Map<string, MapPlacement>();
        const textures = new Set<number>();
        for (const region of this.regionList) this.floorTextures(region, textures);
        if (!this.visibility.landscape) return { placements, textures };
        for (const region of this.regionList) {
            for (const placement of region.objects) {
                if (!this.visibility.planes[placement.plane]) continue;
                const key = mapAppearanceKey(placement);
                if (placements.has(key)) continue;
                placements.set(key, placement);
                this.appearanceTextures(key, textures);
            }
        }
        for (const placement of this.visibility.previewPlacements ?? []) {
            const key = mapAppearanceKey(placement);
            if (placements.has(key)) continue;
            placements.set(key, placement);
            this.appearanceTextures(key, textures);
        }
        return { placements, textures };
    }

    private floorTextures(region: MapRenderRegion, textures: Set<number>): void {
        const floors = new Set<string>();
        region.planes.forEach((plane, level) => {
            if (!this.visibility.planes[level]) return;
            for (const id of plane.underlays) floors.add(`false:${id}`);
            for (const id of plane.overlays) floors.add(`true:${id}`);
        });
        for (const floor of region.floors) if (floors.has(`${floor.overlay}:${floor.id}`) && floor.textureId >= 0) textures.add(floor.textureId);
    }

    private appearanceTextures(key: string, textures: Set<number>): void {
        const appearance = this.appearances.get(key);
        if (!appearance) return;
        let ids = this.appearanceTextureIds.get(appearance);
        if (!ids) {
            const unique = new Set<number>();
            for (const part of appearance.parts) for (const id of part.faceTextureIds ?? []) if (id >= 0) unique.add(id);
            ids = [...unique];
            this.appearanceTextureIds.set(appearance, ids);
        }
        for (const id of ids) textures.add(id);
    }

    private loadPendingTextures(textures: Set<number>): void {
        for (const id of textures) {
            if (this.textureRequests >= 4) break;
            if (!this.attemptedTextures.has(id)) this.loadTexture(id);
        }
    }

    private pump(): void {
        if (this.cancelled) return;
        if (this.pumping) {
            this.pumpAgain = true;
            return;
        }
        this.pumping = true;
        do {
            this.pumpAgain = false;
            const { placements, textures } = this.interest();
            this.loadPendingTextures(textures);
            const pending = [...placements].filter(([key]) => !this.attemptedAppearances.has(key));
            while (this.appearanceRequests < 2 && pending.length) this.loadAppearances(pending.splice(0, 32));
        } while (this.pumpAgain && !this.cancelled);
        this.pumping = false;
        this.schedule();
    }

    private loadAppearances(entries: [string, MapPlacement][]): void {
        for (const [key] of entries) this.attemptedAppearances.add(key);
        this.appearanceRequests++;
        this.requests.add(
            this.service
                .appearances(entries.map(([, placement]) => placement))
                .pipe(
                    tap((rows) => {
                        for (const [key, placement] of entries)
                            this.acceptAppearance(
                                key,
                                placement,
                                rows.find((row) => mapAppearanceKey(row) === key)
                            );
                    }),
                    catchError((error) => {
                        for (const [, placement] of entries) this.fail(`Object ${placement.id}, shape ${placement.shape}: ${getCacheErrorMessage(error)}`);
                        return EMPTY;
                    }),
                    finalize(() => {
                        for (const [key] of entries) this.settledAppearances.add(key);
                        this.appearanceRequests--;
                        this.pump();
                    })
                )
                .subscribe()
        );
    }

    private acceptAppearance(key: string, placement: MapPlacement, row: ObjectAppearanceResult | undefined): void {
        if (row?.status !== 200 || !row.appearance) {
            this.fail(`Object ${placement.id}, shape ${placement.shape}: ${row?.problem?.detail ?? row?.problem?.title ?? "Appearance unavailable."}`);
            return;
        }
        this.appearances.set(key, row.appearance);
        this.appearancesDirty = true;
        for (const warning of row.appearance.warnings) this.warnings.add(`Object ${placement.id}: ${warning}`);
    }

    private loadTexture(id: number): void {
        this.attemptedTextures.add(id);
        this.textureRequests++;
        this.requests.add(
            this.service
                .texture(id)
                .pipe(
                    switchMap(({ blob, material }) =>
                        from(
                            createImageBitmap(blob, { imageOrientation: "flipY" }).then((image) => {
                                if (this.cancelled) {
                                    image.close();
                                    return;
                                }
                                const mode = material.alphaMode;
                                if (mode !== 0 && mode !== 1 && mode !== 2) {
                                    image.close();
                                    throw new Error("Unsupported texture alpha mode.");
                                }
                                this.textures.set(id, { image, ...material, alphaMode: mode });
                                this.texturesDirty = true;
                            })
                        )
                    ),
                    catchError((error) => {
                        this.fail(`Texture ${id}: ${getCacheErrorMessage(error)}`);
                        return EMPTY;
                    }),
                    finalize(() => {
                        this.settledTextures.add(id);
                        this.textureRequests--;
                        this.pump();
                    })
                )
                .subscribe()
        );
    }

    private fail(message: string): void {
        this.failures++;
        this.warnings.add(message);
    }

    private schedule(): void {
        if (!this.isLoading()) {
            this.flush();
            return;
        }
        this.publication ??= setTimeout(() => {
            this.publication = undefined;
            this.flush();
        }, 250);
    }

    private isLoading(): boolean {
        return this.remainingRegions > 0 || this.appearanceRequests > 0 || this.textureRequests > 0;
    }

    private flush(): void {
        if (this.cancelled) return;
        clearTimeout(this.publication);
        this.publication = undefined;
        if (this.appearancesDirty) {
            this.publishedAppearances = new Map(this.appearances);
            this.appearancesDirty = false;
        }
        if (this.texturesDirty) {
            this.publishedTextures = new Map(this.textures);
            this.texturesDirty = false;
        }
        const interest = this.interest();
        this.publish({
            regions: this.regionList,
            appearances: this.publishedAppearances,
            textures: this.publishedTextures,
            warnings: [...this.warnings],
            loading: this.isLoading(),
            progress: {
                regionsSettled: this.ids.length - this.remainingRegions,
                regionsTotal: this.ids.length,
                appearancesSettled: [...interest.placements.keys()].filter((key) => this.settledAppearances.has(key)).length,
                appearancesTotal: interest.placements.size,
                texturesSettled: [...interest.textures].filter((id) => this.settledTextures.has(id)).length,
                texturesTotal: interest.textures.size,
                failures: this.failures,
            },
        });
    }

    dispose(): void {
        this.cancelled = true;
        clearTimeout(this.publication);
        this.requests.unsubscribe();
        for (const texture of this.textures.values()) texture.image.close();
    }
}
