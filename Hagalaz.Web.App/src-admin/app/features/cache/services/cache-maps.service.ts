import { HttpClient } from "@angular/common/http";
import { inject, Service, type Signal } from "@angular/core";
import { catchError, defer, forkJoin, from, map, mergeMap, type Observable, of, shareReplay, switchMap, toArray } from "rxjs";
import { environment } from "../../../../environments/environment";
import { cacheDefinitionResource, cacheQueryResource } from "./cache-resource";
import type { GetMapDefinitionDto, GetMapRegionsDto, GetTextureMaterialDto } from "./dtos";
import type { GetAnimationFrameDto } from "./dtos/get-animation-frame-dto";
import type { GetAnimationSequenceDto } from "./dtos/get-animation-sequence-dto";
import type { MapFloor, MapPlacement, MapRenderRegion, MapTerrainTileChange, MapWorldMapRefreshResult, ObjectAppearanceResult, ObjectCollision, ObjectCollisionResult, UpdateMapLandscapeResultDto, UpdateMapTerrainResultDto } from "./map-render.models";

@Service()
export class CacheMapsService {
    private readonly http = inject(HttpClient);
    private readonly collisionById = new Map<number, Observable<ObjectCollision | null>>();
    private readonly baseUrl = `${environment.cacheApiUrl}types/maps`;

    render(id: number, xteaKeys?: number[]) {
        return this.http.post<MapRenderRegion>(`${this.baseUrl}/${id}/render`, { xteaKeys });
    }

    saveLandscape(id: number, objects: readonly MapPlacement[]) {
        return this.http.post<UpdateMapLandscapeResultDto>(`${this.baseUrl}/${id}/landscape`, { objects });
    }

    saveTerrain(id: number, tiles: readonly MapTerrainTileChange[]) {
        return this.http.post<UpdateMapTerrainResultDto>(`${this.baseUrl}/${id}/terrain`, { tiles });
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

    objectAnimation(id: number) {
        return this.http.get<GetAnimationSequenceDto>(`${environment.cacheApiUrl}types/animations/${id}/sequence`).pipe(
            switchMap(sequence => {
                if (!sequence.frameIds.length) throw new Error("Sequence has no frames.");
                return from(new Set(sequence.frameIds)).pipe(
                    mergeMap(frameId => this.http.get<GetAnimationFrameDto>(`${environment.cacheApiUrl}animation-frames/${frameId}`).pipe(
                        map(frame => {
                            if (frame.frame.transforms.some(transform => ![0, 1, 2, 3, 5].includes(frame.skeleton.types[transform.slot])))
                                throw new Error("Sequence uses unsupported transforms.");
                            return [frameId, frame] as const;
                        }),
                    ), 4),
                    toArray(),
                    map(entries => ({ sequence, frames: new Map(entries) })),
                );
            }),
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
