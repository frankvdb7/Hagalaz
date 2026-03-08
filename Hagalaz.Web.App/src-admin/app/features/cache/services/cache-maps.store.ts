import { HttpClient } from "@angular/common/http";
import { inject } from "@angular/core";
import { pipe, switchMap, tap, catchError, of, forkJoin, map, filter } from "rxjs";
import { signalStore, withState, withMethods, patchState } from "@ngrx/signals";
import { rxMethod } from "@ngrx/signals/rxjs-interop";
import { environment } from "../../../../environments/environment";
import { MapDecodeRequest, MapTypeDto, SearchResultDto } from "./cache.models";

const mockMap1: MapTypeDto = { 
    id: 12850, terrainPlanes: 4, terrainWidth: 64, terrainHeight: 64, objectCount: 5, 
    objects: [
        { id: 100, shapeType: 10, rotation: 0, x: 32, y: 32, z: 0 }, // Lumbridge Castle center
        { id: 101, shapeType: 10, rotation: 1, x: 10, y: 10, z: 0 }, // General Store
        { id: 102, shapeType: 10, rotation: 2, x: 50, y: 50, z: 0 }, // Church
        { id: 103, shapeType: 10, rotation: 0, x: 32, y: 10, z: 0 }, // Bridge North
        { id: 104, shapeType: 10, rotation: 0, x: 32, y: 54, z: 0 }  // Bridge South
    ] 
};

const mockMap2: MapTypeDto = { 
    id: 12345, terrainPlanes: 4, terrainWidth: 64, terrainHeight: 64, objectCount: 2, 
    objects: [
        { id: 200, shapeType: 10, rotation: 0, x: 32, y: 32, z: 0 }, // Varrock Center
        { id: 201, shapeType: 10, rotation: 1, x: 10, y: 50, z: 0 }  // Bank
    ] 
};

const mockMap3: MapTypeDto = { 
    id: 11111, terrainPlanes: 4, terrainWidth: 64, terrainHeight: 64, objectCount: 1, 
    objects: [
        { id: 300, shapeType: 10, rotation: 0, x: 32, y: 32, z: 0 }
    ] 
};

// Internal helper to generate realistic terrain for mocks
const generateRealisticMockTerrain = (id: number, planes: number, width: number, height: number) => {
    const totalTiles = planes * width * height;
    const flags = new Int8Array(totalTiles).fill(0);
    const heights = new Int16Array(totalTiles);

    // Simple fBm-like noise
    const noise = (x: number, y: number) => Math.sin(x * 0.15) * Math.cos(y * 0.15) + Math.sin(x * 0.07 + y * 0.03) * 0.5;
    
    for (let p = 0; p < planes; p++) {
        for (let x = 0; x < width; x++) {
            for (let y = 0; y < height; y++) {
                const idx = (p * width * height) + (x * height) + y;
                
                // Base terrain
                let h = noise(x + id, y + id) * 20;
                
                // Lumbridge simulation for ID 12850
                if (id === 12850) {
                    const riverX = 32 + Math.sin(y * 0.1) * 3;
                    const distToRiver = Math.abs(x - riverX);
                    if (distToRiver < 6) {
                        h = -30 + (distToRiver * 4);
                        if (distToRiver < 3) flags[idx] |= 0x1; // Water
                    }
                    
                    const distToRoad = Math.abs(y - 32);
                    if (distToRoad < 2 && x > riverX + 3) {
                        h = 2; 
                        flags[idx] |= 0x4; // Path
                    }
                }

                // Varrock simulation for ID 12345 (flat city)
                if (id === 12345) {
                    h = 0;
                    if (Math.abs(x - 32) < 10 && Math.abs(y - 32) < 10) {
                        flags[idx] |= 0x4; // Path/Square
                    }
                }

                heights[idx] = Math.round(h * 8); // Scale to RS2 units
            }
        }
    }
    return { flags, heights };
};

export interface CacheMapsState {
    mapInfo: MapTypeDto | null;
    terrainFlags: Int8Array | null;
    terrainHeights: Int16Array | null;
    mapList: MapTypeDto[];
    totalMatches: number;
    offset: number;
    limit: number;
    error: string | null;
    loading: boolean;
}

const initialState: CacheMapsState = {
    mapInfo: null,
    terrainFlags: null,
    terrainHeights: null,
    mapList: [mockMap1, mockMap2, mockMap3],
    totalMatches: 3,
    offset: 0,
    limit: 50,
    error: null,
    loading: false,
};

export const CacheMapsStore = signalStore(
    { providedIn: "root" },
    withState(initialState),
    withMethods((store, http = inject(HttpClient)) => {
        const handleError = (error: unknown) => {
            const response = error as { error?: { detail?: string; title?: string }; message?: string };
            patchState(store, { error: response.error?.detail ?? response.error?.title ?? response.message ?? "Request failed.", loading: false });
            return of(null);
        };

        const fetchTerrain = (id: number) => 
            http.get(`${environment.cacheApiUrl}types/maps/${id}/terrain`, { responseType: "arraybuffer" }).pipe(
                map(buffer => {
                    const view = new DataView(buffer);
                    let pos = 0;
                    
                    const planes = view.getInt32(pos, true); pos += 4;
                    const width = view.getInt32(pos, true); pos += 4;
                    const height = view.getInt32(pos, true); pos += 4;
                    
                    const totalTiles = planes * width * height;
                    const flags = new Int8Array(buffer, pos, totalTiles);
                    pos += totalTiles;
                    const heights = new Int16Array(buffer, pos, totalTiles);
                    
                    return { flags, heights };
                }),
                catchError(() => of(null))
            );

        const generateFlatTerrain = (planes: number, width: number, height: number) => {
            const totalTiles = planes * width * height;
            return {
                flags: new Int8Array(totalTiles).fill(0),
                heights: new Int16Array(totalTiles).fill(0)
            };
        };

        const loadMapMethod = rxMethod<number>(
            pipe(
                tap((id) => {
                    const existing = store.mapList().find(m => m.id === id);
                    patchState(store, { 
                        loading: true, 
                        error: null, 
                        terrainFlags: null,
                        terrainHeights: null,
                        mapInfo: existing ?? null 
                    });
                }),
                switchMap((id) => {
                    // 1. Handle Procedural Mock OR Static Mocks
                    const staticMock = initialState.mapList.find(m => m.id === id);
                    if (id === 99999 || staticMock) {
                        const baseMap = staticMock || store.mapList().find(m => m.id === id);
                        if (baseMap) {
                            const terrain = generateRealisticMockTerrain(id, baseMap.terrainPlanes, baseMap.terrainWidth, baseMap.terrainHeight);
                            patchState(store, { 
                                terrainFlags: terrain.flags,
                                terrainHeights: terrain.heights,
                                loading: false 
                            });
                            return of(null);
                        }
                    }
                    
                    // 2. Handle Live Data
                    return forkJoin({
                        mapInfo: http.get<MapTypeDto>(`${environment.cacheApiUrl}types/maps/${id}`),
                        terrain: fetchTerrain(id)
                    }).pipe(
                        tap(({ mapInfo, terrain }) => {
                            const finalTerrain = terrain ?? generateFlatTerrain(mapInfo.terrainPlanes, mapInfo.terrainWidth, mapInfo.terrainHeight);
                            patchState(store, { 
                                mapInfo, 
                                terrainFlags: finalTerrain.flags, 
                                terrainHeights: finalTerrain.heights,
                                loading: false 
                            });
                        }),
                        catchError((err) => {
                            const currentMap = store.mapInfo();
                            if (currentMap) {
                                const terrain = generateFlatTerrain(currentMap.terrainPlanes, currentMap.terrainWidth, currentMap.terrainHeight);
                                patchState(store, { 
                                    terrainFlags: terrain.flags,
                                    terrainHeights: terrain.heights,
                                    loading: false
                                });
                            }
                            return handleError(err);
                        })
                    );
                })
            )
        );

        return {
            loadMap: loadMapMethod,

            loadInitial: rxMethod<void>(
                pipe(
                    tap(() => patchState(store, { loading: true, error: null })),
                    switchMap(() => 
                        http.get<SearchResultDto<MapTypeDto>>(`${environment.cacheApiUrl}types/maps/search`, {
                            params: { query: "", offset: 0, limit: store.limit() }
                        }).pipe(
                            tap(result => patchState(store, { 
                                mapList: [...initialState.mapList, ...result.items], 
                                totalMatches: result.totalMatches + initialState.mapList.length,
                                offset: store.limit(),
                                loading: false 
                            })),
                            catchError((err) => {
                                patchState(store, { loading: false });
                                return handleError(err);
                            })
                        )
                    )
                )
            ),

            loadMore: rxMethod<void>(
                pipe(
                    filter(() => !store.loading() && store.mapList().length < store.totalMatches()),
                    tap(() => patchState(store, { loading: true })),
                    switchMap(() => 
                        http.get<SearchResultDto<MapTypeDto>>(`${environment.cacheApiUrl}types/maps/search`, {
                            params: { query: "", offset: store.offset(), limit: store.limit() }
                        }).pipe(
                            tap(result => patchState(store, { 
                                mapList: [...store.mapList(), ...result.items], 
                                offset: store.offset() + store.limit(),
                                loading: false 
                            })),
                            catchError(handleError)
                        )
                    )
                )
            ),

            decodeMap: rxMethod<{ id: number; request: MapDecodeRequest }>(
                pipe(
                    tap(() => patchState(store, { loading: true, error: null })),
                    switchMap(({ id, request }) => 
                        forkJoin({
                            mapInfo: http.post<MapTypeDto>(`${environment.cacheApiUrl}types/maps/${id}/decode`, request),
                            terrain: fetchTerrain(id)
                        }).pipe(
                            tap(({ mapInfo, terrain }) => {
                                const finalTerrain = terrain ?? generateFlatTerrain(mapInfo.terrainPlanes, mapInfo.terrainWidth, mapInfo.terrainHeight);
                                patchState(store, { mapInfo, terrainFlags: finalTerrain.flags, terrainHeights: finalTerrain.heights, loading: false });
                            }),
                            catchError(handleError)
                        )
                    )
                )
            ),

            clearMap: () => patchState(store, { mapInfo: null, terrainFlags: null, terrainHeights: null, error: null }),

            loadMockMap: () => {
                const existingMock = store.mapList().find(m => m.id === 99999);
                if (!existingMock) {
                    const mockMap: MapTypeDto = {
                        id: 99999,
                        terrainPlanes: 4,
                        terrainWidth: 64,
                        terrainHeight: 64,
                        objectCount: 0,
                        objects: []
                    };
                    patchState(store, { 
                        mapList: [mockMap, ...store.mapList()],
                        totalMatches: store.totalMatches() + 1
                    });
                }
                loadMapMethod(99999);
            }
        };
    })
);
