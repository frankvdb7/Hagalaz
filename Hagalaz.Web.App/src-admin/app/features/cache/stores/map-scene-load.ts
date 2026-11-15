import { catchError, EMPTY, finalize, from, map, mergeMap, Subscription, switchMap, tap } from "rxjs";
import type { ModelTexture } from "../../../../../shared/model-viewer/model-scene";
import type { CacheMapsService } from "../services/cache-maps.service";
import {
    mapAppearanceKey,
    selectedMapIds,
    withUnderwaterLayer,
    type MapLoadProgress,
    type MapLoadRequest,
    type MapObjectClip,
    type MapPlacement,
    type MapRenderRegion,
    type MapVisibility,
    type ObjectAppearanceResult,
    type ObjectRenderAppearance,
} from "../services/map-render.models";
import { getCacheErrorMessage } from "./cache-error";
import type { GetModelDto } from "../services/dtos";

function addModelTextures(part: GetModelDto, textures: Set<number>): void {
    for (const id of part.faceTextureIds ?? []) if (id >= 0) textures.add(id);
    for (const billboard of part.billboards ?? []) {
        const id = billboard.definition?.textureId ?? -1;
        if (id >= 0) textures.add(id);
    }
}

export interface MapSceneState {
    regions: MapRenderRegion[];
    appearances: Map<string, ObjectRenderAppearance>;
    animations: ReadonlyMap<number, MapObjectClip>;
    textures: Map<number, ModelTexture>;
    environmentFaces: readonly ModelTexture[] | null;
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
    private animations = new Map<number, MapObjectClip>();
    private readonly attemptedAnimations = new Set<number>();
    private animationRequests = 0;
    private readonly appearanceTextureIds = new WeakMap<ObjectRenderAppearance, readonly number[]>();
    private readonly textures = new Map<number, ModelTexture>();
    // Hidden planes keep their published textures; environment-only images stay out of surface rendering.
    private readonly surfaceTextureIds = new Set<number>();
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
    private publishedAppearances = new Map<string, ObjectRenderAppearance>();
    private publishedTextures = new Map<number, ModelTexture>();
    private publishedEnvironmentFaces: readonly ModelTexture[] | null = null;
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
                                map(withUnderwaterLayer),
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

    private interest(): { placements: Map<string, MapPlacement>; textures: Set<number>; environment: number[] } {
        const placements = new Map<string, MapPlacement>();
        const textures = new Set<number>(this.visibility.previewTextures);
        for (const region of this.regionList) this.floorTextures(region, textures);
        const environment = this.anchorEnvironmentFaces();
        if (!this.visibility.landscape) return { placements, textures, environment };
        const visible = this.regionList.flatMap(region => region.objects.filter(placement => this.visibility.planes[placement.plane]));
        for (const placement of [...visible, ...(this.visibility.previewPlacements ?? [])]) this.addInterest(placement, placements, textures);
        return { placements, textures, environment };
    }

    /** Records the first placement of each appearance together with the textures that appearance needs. */
    private addInterest(placement: MapPlacement, placements: Map<string, MapPlacement>, textures: Set<number>): void {
        const key = mapAppearanceKey(placement);
        if (placements.has(key)) return;
        placements.set(key, placement);
        this.appearanceTextures(key, textures);
    }

    // The viewer takes its environment from the anchor region.
    private anchorEnvironmentFaces(): number[] {
        return this.regions.get(this.request.id)?.atmosphere?.environmentFaces ?? [];
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
            for (const part of appearance.parts) addModelTextures(part, unique);
            ids = [...unique];
            this.appearanceTextureIds.set(appearance, ids);
        }
        for (const id of ids) textures.add(id);
    }

    private loadPendingTextures(textures: Iterable<number>): void {
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
            const { placements, textures, environment } = this.interest();
            this.loadPendingTextures(new Set([...textures, ...environment]));
            const pending = [...placements].filter(([key]) => !this.attemptedAppearances.has(key));
            while (this.appearanceRequests < 2 && pending.length) this.loadAppearances(pending.splice(0, 32));
            this.loadPendingAnimations(placements.keys());
        } while (this.pumpAgain && !this.cancelled);
        this.pumping = false;
        this.schedule();
    }

    private loadPendingAnimations(keys: Iterable<string>): void {
        for (const key of keys) {
            if (this.animationRequests >= 2) break;
            const id = this.appearances.get(key)?.parts.find(part => part.objectAnimation)?.animationIds?.[0];
            if (id !== undefined && !this.attemptedAnimations.has(id)) this.loadAnimation(id);
        }
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

    private loadAnimation(id: number): void {
        this.attemptedAnimations.add(id);
        this.animationRequests++;
        this.requests.add(this.service.objectAnimation(id).pipe(
            tap(clip => {
                this.animations = new Map(this.animations).set(id, clip);
            }),
            catchError(error => {
                this.warnings.add(`Animation ${id}: ${getCacheErrorMessage(error)}`);
                return EMPTY;
            }),
            finalize(() => {
                this.animationRequests--;
                this.pump();
            }),
        ).subscribe());
    }

    private loadTexture(id: number): void {
        this.attemptedTextures.add(id);
        this.textureRequests++;
        this.requests.add(
            this.service
                .texture(id, true)
                .pipe(
                    switchMap(({ blob, material }) =>
                        from(
                            // Premultiplying would destroy the colour under a low highlight-mask alpha.
                            createImageBitmap(blob, { imageOrientation: "flipY", premultiplyAlpha: "none" }).then((image) => {
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

    private publishTextures(surface: ReadonlySet<number>, environment: readonly number[]): void {
        for (const id of surface) this.surfaceTextureIds.add(id);
        const surfaces = [...this.textures].filter(([id]) => this.surfaceTextureIds.has(id));
        // Both sets only grow during a load, so an equal size means the published map is current.
        if (surfaces.length !== this.publishedTextures.size) this.publishedTextures = new Map(surfaces);
        if (this.publishedEnvironmentFaces) return;
        // Like the client, expose the cube only once every face image is available.
        const faces = environment.flatMap((id) => this.textures.get(id) ?? []);
        if (faces.length === 6) this.publishedEnvironmentFaces = faces;
    }

    private flush(): void {
        if (this.cancelled) return;
        clearTimeout(this.publication);
        this.publication = undefined;
        if (this.appearancesDirty) {
            this.publishedAppearances = new Map(this.appearances);
            this.appearancesDirty = false;
        }
        const interest = this.interest();
        const requestedTextures = new Set([...interest.textures, ...interest.environment]);
        this.publishTextures(interest.textures, interest.environment);
        this.publish({
            regions: this.regionList,
            appearances: this.publishedAppearances,
            animations: this.animations,
            textures: this.publishedTextures,
            environmentFaces: this.publishedEnvironmentFaces,
            warnings: [...this.warnings],
            loading: this.isLoading(),
            progress: {
                regionsSettled: this.ids.length - this.remainingRegions,
                regionsTotal: this.ids.length,
                appearancesSettled: [...interest.placements.keys()].filter((key) => this.settledAppearances.has(key)).length,
                appearancesTotal: interest.placements.size,
                texturesSettled: [...requestedTextures].filter((id) => this.settledTextures.has(id)).length,
                texturesTotal: requestedTextures.size,
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
