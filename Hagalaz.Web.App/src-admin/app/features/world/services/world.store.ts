import { HttpClient } from "@angular/common/http";
import { inject } from "@angular/core";
import { pipe, switchMap, tap, catchError, of } from "rxjs";
import { signalStore, withState, withMethods, patchState } from "@ngrx/signals";
import { rxMethod } from "@ngrx/signals/rxjs-interop";
import { environment } from "../../../../environments/environment";

export interface WorldStatusDto {
    id: number;
    name: string;
    playerCount: number;
    maxPlayers: number;
    isOnline: boolean;
    uptime: string;
    lastBoot: string;
}

export interface WorldState {
    worlds: WorldStatusDto[];
    loading: boolean;
    error: string | null;
}

const initialState: WorldState = {
    worlds: [],
    loading: false,
    error: null,
};

export const WorldStore = signalStore(
    { providedIn: "root" },
    withState(initialState),
    withMethods((store, http = inject(HttpClient)) => ({
        loadStatus: rxMethod<void>(
            pipe(
                tap(() => patchState(store, { loading: true, error: null })),
                switchMap(() => {
                    const baseUrl = `${environment.cacheApiUrl.replace('cache/api/v1/cache/', 'api/v1/world')}`;
                    return http.get<WorldStatusDto[]>(`${baseUrl}/status`).pipe(
                        tap(worlds => patchState(store, { worlds, loading: false })),
                        catchError(() => {
                            // Mocking fallback
                            const mockWorlds: WorldStatusDto[] = [
                                { id: 1, name: "The Storm", playerCount: 142, maxPlayers: 2000, isOnline: true, uptime: "4d 12h 05m", lastBoot: "2026-03-03" },
                                { id: 2, name: "Wilderness War", playerCount: 12, maxPlayers: 500, isOnline: true, uptime: "0d 05h 22m", lastBoot: "2026-03-07" },
                                { id: 3, name: "Dev Sanctuary", playerCount: 1, maxPlayers: 100, isOnline: false, uptime: "0s", lastBoot: "N/A" },
                            ];
                            patchState(store, { worlds: mockWorlds, loading: false });
                            return of(null);
                        })
                    );
                })
            )
        )
    }))
);
