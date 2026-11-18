import { catchError, EMPTY, finalize, from, map, mergeMap, Subscription, switchMap, tap } from "rxjs";
import type { ModelTexture } from "../../../../../shared/model-viewer/model-scene";
import { waterVolume, type WaterVolume } from "../../../../../shared/model-viewer/water-noise";
import type { CacheMapsService } from "../services/cache-maps.service";
import {
    mapAppearanceKey,
    selectedMapIds,
    withUnderwaterLayer,
    underwaterPlaneIndex,
    type MapLoadProgress,
    type MapLoadRequest,
    type MapObjectClip,
    type MapPlacement,
    type MapRegionSave,
    type MapRenderRegion,
    type MapVisibility,
    type ObjectAppearanceResult,
    type ObjectRenderAppearance,
} from "../services/map-render.models";
import { getCacheErrorMessage } from "./cache-error";
import type { GetModelDto, GetTextureMaterialDto } from "../services/dtos";

function addModelTextures(part: GetModelDto, textures: Set<number>): void {
    for (const id of part.faceTextureIds ?? []) if (id >= 0) textures.add(id);
    for (const billboard of part.billboards ?? []) {
        const id = billboard.definition?.textureId ?? -1;
        if (id >= 0) textures.add(id);
    }
}

/** Milliseconds before the first progressive publication for each selected region; later ones wait twice as long each time. */
const publicationInterval = 250;
/** The longest wait between progressive publications for each selected region. */
const maxPublicationInterval = 2000;
/** Concurrent requests for the many small texture reads. */
const textureConcurrency = 6;
/** Texture materials are read 256 at a time, so water can load as its own phase. */
const materialBatchSize = 256;
/** The client's water material effects and the noise volume each one samples. */
const waterVolumes: Readonly<Record<number, WaterVolume>> = { 2: "ridged", 4: "ridged", 5: "lake", 8: "normal", 9: "normal" };
/** Animation clips are read 32 at a time, two requests in flight. */
const animationBatchSize = 32;
const animationConcurrency = 2;

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
    animationsSettled: 0,
    animationsTotal: 0,
    waterSettled: 0,
    waterTotal: 0,
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
    private readonly settledAnimations = new Set<number>();
    private readonly materials = new Map<number, GetTextureMaterialDto | null>();
    private readonly requestedMaterials = new Set<number>();
    /** Water noise volumes this scene needs, and whether each is ready. */
    private readonly volumes = new Map<WaterVolume, boolean>();
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
    private readonly regionWarnings = new Map<number, string[]>();
    private regionList: MapRenderRegion[] = [];
    private remainingRegions: number;
    private appearanceRequests = 0;
    private textureRequests = 0;
    private cancelled = false;
    private pumping = false;
    private pumpAgain = false;
    private publication?: ReturnType<typeof setTimeout>;
    private publications = 0;
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
                                    this.setRegion(region, region.warnings);
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

    replaceKinds(fresh: MapRenderRegion, kinds: readonly MapRegionSave["kind"][], keepPlacements = false): boolean {
        const current = this.regions.get(fresh.id);
        if (!current || this.cancelled) return false;
        const terrain = kinds.includes("terrain");
        const landscape = kinds.includes("landscape");
        const region: MapRenderRegion = {
            ...current,
            ...(terrain && {
                terrainStatus: fresh.terrainStatus, terrainVersion: fresh.terrainVersion, planes: fresh.planes,
                floors: fresh.floors, atmosphere: fresh.atmosphere, pointLights: fresh.pointLights, underwaterPlane: fresh.underwaterPlane,
            }),
            ...(landscape && {
                landscapeStatus: fresh.landscapeStatus, landscapeVersion: fresh.landscapeVersion,
                objects: keepPlacements ? current.objects : [...fresh.objects.filter(object => object.plane !== underwaterPlaneIndex), ...current.objects.filter(object => object.plane === underwaterPlaneIndex)],
            }),
        };
        if (terrain) this.updateTerrainBorders(region);
        this.setRegion(region, fresh.warnings);
        return true;
    }

    private updateTerrainBorders(region: MapRenderRegion): void {
        for (const neighbor of this.regionList) {
            const dx = region.regionX - neighbor.regionX;
            const dy = region.regionY - neighbor.regionY;
            if (dx < 0 || dx > 1 || dy < 0 || dy > 1 || (!dx && !dy)) continue;
            const planes = neighbor.planes.map((plane, index) => {
                // A corner owned by a region without underwater terrain has zero underwater depth.
                const source = region.planes[index] ?? (index === underwaterPlaneIndex ? region.planes[0] : undefined);
                if (source?.cornerHeights.length !== 65 * 65 || plane.cornerHeights.length !== 65 * 65) return plane;
                const cornerHeights = [...plane.cornerHeights];
                for (let x = dx * 64; x < 64 + dx; x++) {
                    for (let y = dy * 64; y < 64 + dy; y++) {
                        cornerHeights[x * 65 + y] = source.cornerHeights[(x - dx * 64) * 65 + y - dy * 64];
                    }
                }
                return { ...plane, cornerHeights };
            });
            this.regions.set(neighbor.id, { ...neighbor, planes });
        }
    }

    private setRegion(region: MapRenderRegion, warnings: readonly string[]): void {
        this.regions.set(region.id, region);
        this.regionList = this.ids.flatMap(id => this.regions.get(id) ?? []);
        const issues = [...warnings];
        for (const kind of ["terrain", "landscape"] as const) {
            const status = region[`${kind}Status`];
            if (status !== "available") issues.push(`Region ${region.id}: ${kind} ${status}.`);
        }
        this.regionWarnings.set(region.id, issues);
        this.flush();
        this.pump();
    }

    setVisibility(visibility: MapVisibility): void {
        this.visibility = visibility;
        this.pump();
        this.flush();
    }

    private interest(): { placements: Map<string, MapPlacement>; textures: Set<number>; environment: number[] } {
        const placements = new Map<string, MapPlacement>();
        const textures = new Set<number>(this.visibility.previewTextures);
        // Requests follow insertion order: the focused plane first, and within a plane what lies nearest the camera's focus.
        const rank = this.planeRank();
        const distance = this.focusDistance();
        const levels = [...this.visibility.planes.keys()].filter(level => this.visibility.planes[level]).sort((left, right) => rank(left) - rank(right));
        const regions = [...this.regionList].sort((left, right) => distance(left.regionX * 64 + 32, left.regionY * 64 + 32) - distance(right.regionX * 64 + 32, right.regionY * 64 + 32));
        for (const level of levels) for (const region of regions) this.floorTextures(region, level, textures);
        const environment = this.anchorEnvironmentFaces();
        if (!this.visibility.landscape) return { placements, textures, environment };
        const visible = this.regionList.flatMap(region => region.objects
            .filter(placement => this.visibility.planes[placement.plane])
            .map(placement => ({ placement, rank: rank(placement.plane), distance: distance(region.regionX * 64 + placement.x, region.regionY * 64 + placement.y) })))
            .sort((left, right) => left.rank - right.rank || left.distance - right.distance)
            .map(({ placement }) => placement);
        for (const placement of [...visible, ...(this.visibility.previewPlacements ?? [])]) this.addInterest(placement, placements, textures);
        return { placements, textures, environment };
    }

    /** Tiles from the camera's focus, or zero for everything before the viewer reports one. */
    private focusDistance(): (x: number, y: number) => number {
        const focus = this.visibility.focusTile;
        return focus ? (x, y) => Math.max(Math.abs(x - focus.x), Math.abs(y - focus.y)) : () => 0;
    }

    /** Distance from the focused plane; underwater terrain sits just below plane 0. */
    private planeRank(): (plane: number) => number {
        const focus = this.visibility.focusPlane ?? 0;
        return plane => Math.abs((plane === underwaterPlaneIndex ? -1 : plane) - focus);
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

    private floorTextures(region: MapRenderRegion, level: number, textures: Set<number>): void {
        const plane = region.planes[level];
        if (!plane) return;
        const floors = new Set<string>();
        for (const id of plane.underlays) floors.add(`false:${id}`);
        for (const id of plane.overlays) floors.add(`true:${id}`);
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
        // Water is its own phase: once materials name it, its images load ahead of the others.
        const ordered = [...textures].sort((left, right) => Number(this.isWater(right)) - Number(this.isWater(left)));
        for (const id of ordered) {
            if (this.textureRequests >= textureConcurrency) break;
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
            const wanted = new Set([...textures, ...environment]);
            this.loadPendingMaterials(wanted);
            this.prepareWaterVolumes(wanted);
            this.loadPendingTextures(wanted);
            const pending = [...placements].filter(([key]) => !this.attemptedAppearances.has(key));
            while (this.appearanceRequests < 2 && pending.length) this.loadAppearances(pending.splice(0, 32));
            this.loadPendingAnimations(placements.keys());
        } while (this.pumpAgain && !this.cancelled);
        this.pumping = false;
        this.schedule();
    }

    private loadPendingMaterials(textures: Iterable<number>): void {
        const ids = [...textures].filter(id => !this.requestedMaterials.has(id));
        for (let start = 0; start < ids.length; start += materialBatchSize) this.loadMaterials(ids.slice(start, start + materialBatchSize));
    }

    private loadMaterials(ids: readonly number[]): void {
        for (const id of ids) this.requestedMaterials.add(id);
        this.requests.add(this.service.textureMaterials(ids).pipe(
            tap(materials => {
                for (const id of ids) this.materials.set(id, materials.get(id) ?? null);
            }),
            // Without materials the textures still load, in their usual order.
            catchError(() => EMPTY),
            finalize(() => this.pump()),
        ).subscribe());
    }

    /** The effect of a texture, from its batched material or, once loaded, its image. */
    private textureEffect(id: number): number | undefined {
        return this.materials.get(id)?.effectId ?? this.textures.get(id)?.effectId;
    }

    private isWater(id: number): boolean {
        return waterVolumes[this.textureEffect(id) ?? 0] !== undefined;
    }

    /** Starts generating each water noise volume the scene's water needs, alongside the rest of the load. */
    private prepareWaterVolumes(textures: Iterable<number>): void {
        for (const id of textures) {
            const kind = waterVolumes[this.textureEffect(id) ?? 0];
            if (!kind || this.volumes.has(kind)) continue;
            this.volumes.set(kind, false);
            void waterVolume(kind).then(() => {
                if (this.cancelled) return;
                this.volumes.set(kind, true);
                this.pump();
            });
        }
    }

    private loadPendingAnimations(keys: Iterable<string>): void {
        const ids = [...this.animationIds(keys)].filter(id => !this.attemptedAnimations.has(id));
        while (this.animationRequests < animationConcurrency && ids.length) this.loadAnimations(ids.splice(0, animationBatchSize));
    }

    /** The object animations the loaded appearances of these keys play. */
    private animationIds(keys: Iterable<string>): Set<number> {
        const ids = new Set<number>();
        for (const key of keys) {
            const id = this.appearances.get(key)?.parts.find(part => part.objectAnimation)?.animationIds?.[0];
            if (id !== undefined) ids.add(id);
        }
        return ids;
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

    private loadAnimations(ids: readonly number[]): void {
        for (const id of ids) this.attemptedAnimations.add(id);
        this.animationRequests++;
        this.requests.add(this.service.objectClips(ids).pipe(
            tap(clips => {
                const animations = new Map(this.animations);
                for (const id of ids) {
                    const clip = clips.get(id) ?? new Error("Animation unavailable.");
                    if (clip instanceof Error) this.warnings.add(`Animation ${id}: ${clip.message}`);
                    else animations.set(id, clip);
                }
                this.animations = animations;
            }),
            catchError(error => {
                for (const id of ids) this.warnings.add(`Animation ${id}: ${getCacheErrorMessage(error)}`);
                return EMPTY;
            }),
            finalize(() => {
                for (const id of ids) this.settledAnimations.add(id);
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
        // Each publication rebuilds the whole scene, which takes far longer for nine regions than for one. Publishing at a fixed
        // pace left the main thread rebuilding back to back, starving the requests themselves, so the pace backs off: the
        // first results show quickly and the rest arrive in a few larger steps. The finished load always publishes at once.
        const interval = Math.min(publicationInterval * 2 ** this.publications, maxPublicationInterval);
        this.publication ??= setTimeout(() => {
            this.publication = undefined;
            this.publications++;
            this.flush();
        }, interval * Math.max(1, this.ids.length));
    }

    /** Water textures count with the water noise volumes; other textures count on their own. */
    private textureProgress(requested: ReadonlySet<number>) {
        const water = [...requested].filter(id => this.isWater(id));
        const other = [...requested].filter(id => !this.isWater(id));
        const settled = (ids: number[]) => ids.filter(id => this.settledTextures.has(id)).length;
        const volumes = [...this.volumes.values()];
        return {
            texturesSettled: settled(other), texturesTotal: other.length,
            waterSettled: settled(water) + volumes.filter(Boolean).length, waterTotal: water.length + volumes.length,
        };
    }

    private animationProgress(keys: Iterable<string>): { animationsSettled: number; animationsTotal: number } {
        const ids = this.animationIds(keys);
        return { animationsSettled: [...ids].filter(id => this.settledAnimations.has(id)).length, animationsTotal: ids.size };
    }

    private isLoading(): boolean {
        return this.remainingRegions > 0 || this.appearanceRequests > 0 || this.textureRequests > 0 || this.animationRequests > 0
            // Materials only order the texture work, so they do not hold the scene in its loading state.
            || [...this.volumes.values()].some(ready => !ready);
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
            warnings: [...new Set([...this.warnings, ...[...this.regionWarnings.values()].flat()])],
            loading: this.isLoading(),
            progress: {
                regionsSettled: this.ids.length - this.remainingRegions,
                regionsTotal: this.ids.length,
                appearancesSettled: [...interest.placements.keys()].filter((key) => this.settledAppearances.has(key)).length,
                appearancesTotal: interest.placements.size,
                ...this.textureProgress(requestedTextures),
                ...this.animationProgress(interest.placements.keys()),
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
