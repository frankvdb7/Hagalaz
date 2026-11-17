import { HttpClient } from "@angular/common/http";
import { inject, Service, type Signal } from "@angular/core";
import { catchError, defer, forkJoin, map, type Observable, of, shareReplay } from "rxjs";
import { environment } from "../../../../environments/environment";
import { cacheDefinitionResource, cacheQueryResource } from "./cache-resource";
import type { GetMapDefinitionDto, GetMapRegionsDto, GetTextureMaterialDto } from "./dtos";
import type { GetAnimationFrameDto } from "./dtos/get-animation-frame-dto";
import type { AnimationClipResult, MapFloor, MapObjectClip, TextureMaterialResult, MapPlacement, MapRenderRegion, MapTerrainTileChange, MapWorldMapRefreshResult, ObjectAppearanceResult, ObjectCollision, ObjectCollisionResult, UpdateMapLandscapeResultDto, UpdateMapTerrainResultDto } from "./map-render.models";

@Service()
export class CacheMapsService {
    private readonly http = inject(HttpClient);
    private readonly collisionById = new Map<number, Observable<ObjectCollision | null>>();
    private readonly baseUrl = `${environment.cacheApiUrl}types/maps`;

    render(id: number, xteaKeys?: number[]) {
        return this.http.post<MapRenderRegion>(`${this.baseUrl}/${id}/render`, { xteaKeys });
    }

    saveLandscape(id: number, objects: readonly MapPlacement[], expectedVersion: number, xteaKeys?: number[]) {
        return this.http.post<UpdateMapLandscapeResultDto>(`${this.baseUrl}/${id}/landscape`, { objects, expectedVersion, ...(xteaKeys && { xteaKeys }) });
    }

    saveTerrain(id: number, tiles: readonly MapTerrainTileChange[], expectedVersion: number) {
        return this.http.post<UpdateMapTerrainResultDto>(`${this.baseUrl}/${id}/terrain`, { tiles, expectedVersion });
    }

    refreshWorldMap(regions: readonly number[]) {
        return this.http.post<MapWorldMapRefreshResult>(`${this.baseUrl}/worldmap/refresh`, { regions });
    }

    floors() {
        return this.http.get<MapFloor[]>(`${this.baseUrl}/floors`);
    }

    appearances(placements: readonly MapPlacement[]) {
        return this.http.post<ObjectAppearanceResult[]>(
            `${environment.cacheApiUrl}types/objects/render-appearances`,
            placements.map(({ id, shape, rotation }) => ({ id, shape, rotation }))
        );
    }

    objectCollision(ids: readonly number[]): Observable<ReadonlyMap<number, ObjectCollision | null>> {
        return defer(() => {
            const requested = [...new Set(ids)];
            const missing = requested.filter(id => !this.collisionById.has(id));
            for (let start = 0; start < missing.length; start += 256) {
                const batch = missing.slice(start, start + 256);
                const definitions = this.http.post<ObjectCollisionResult[]>(
                    `${environment.cacheApiUrl}types/objects/collision`, { ids: batch }
                ).pipe(
                    map(results => new Map(results.map(result => [result.id, result.collision]))),
                    catchError(() => of(new Map<number, ObjectCollision | null>())),
                    shareReplay(1),
                );
                for (const id of batch) {
                    this.collisionById.set(id, definitions.pipe(map(results => results.get(id) ?? null)));
                }
            }
            if (!requested.length) return of(new Map());
            return forkJoin(requested.map(id => (this.collisionById.get(id) ?? of(null)).pipe(
                map(collision => [id, collision] as const),
            ))).pipe(map(entries => new Map(entries)));
        });
    }

    /** Reads up to 256 texture materials, so water can be recognised before any image is fetched. Failed rows map to null. */
    textureMaterials(ids: readonly number[]): Observable<ReadonlyMap<number, GetTextureMaterialDto | null>> {
        return this.http.post<TextureMaterialResult[]>(`${environment.cacheApiUrl}textures/materials`, ids).pipe(
            map(rows => new Map(rows.map(row => [row.id, row.status === 200 ? row.material : null]))),
        );
    }

    /**
     * Reads up to 32 object animations, each with its sequence and every frame, in one request. Each ID maps to its clip or
     * to the error that keeps it in its base pose.
     */
    objectClips(ids: readonly number[]): Observable<ReadonlyMap<number, MapObjectClip | Error>> {
        return this.http.post<AnimationClipResult[]>(`${environment.cacheApiUrl}types/animations/clips`, ids).pipe(
            map(rows => new Map(rows.map(row => [row.id, objectClip(row)]))),
        );
    }

    /** Scene rendering needs source alpha, which masks material highlights; previews show the image as opaque. */
    texture(id: number, sourceAlpha = false) {
        return this.http.get(`${environment.cacheApiUrl}textures/${id}/image${sourceAlpha ? "?sourceAlpha=true" : ""}`, { responseType: "blob", observe: "response" }).pipe(
            map((response) => {
                const metadata = response.headers.get("X-Texture-Material");
                if (!response.body || !metadata) throw new Error("Texture image or material metadata is missing.");
                return { blob: response.body, material: JSON.parse(metadata) as GetTextureMaterialDto };
            })
        );
    }

    regionsResource(page: Signal<{ offset: number; limit: number } | null>) {
        return cacheQueryResource<GetMapRegionsDto>(this.baseUrl, page);
    }

    definitionResource(id: Signal<number | null>) {
        return cacheDefinitionResource<GetMapDefinitionDto>(this.baseUrl, id);
    }
}

function objectClip(row: AnimationClipResult): MapObjectClip | Error {
    const { sequence, frames } = row;
    if (row.status !== 200 || !sequence || !frames) return new Error(problemMessage(row.problem));
    if (!sequence.frameIds.length) return new Error("Sequence has no frames.");
    if (!frames.every(({ frame }) => supportedFrame(frame))) return new Error("Sequence uses unsupported transforms.");
    return { sequence, frames: new Map(frames.map(({ frameId, frame }) => [frameId, frame])) };
}

function problemMessage(problem: AnimationClipResult["problem"]): string {
    return problem?.detail ?? problem?.title ?? "Animation unavailable.";
}

/** The map renderer poses translation, rotation, scale and alpha transforms (types 0-3 and 5). */
function supportedFrame({ frame, skeleton }: GetAnimationFrameDto): boolean {
    return frame.transforms.every(transform => [0, 1, 2, 3, 5].includes(skeleton.types[transform.slot]));
}
