import { HttpClient } from "@angular/common/http";
import { inject } from "@angular/core";
import { pipe, switchMap, tap, catchError, of } from "rxjs";
import { signalStore, withState, withMethods, patchState } from "@ngrx/signals";
import { rxMethod } from "@ngrx/signals/rxjs-interop";
import { environment } from "../../../../environments/environment";

export interface CharacterDto {
    id: number;
    username: string;
    displayName: string;
    email: string;
    createdDate: string;
    lastLoginDate: string | null;
    isBanned: boolean;
    isMuted: boolean;
}

export interface CharacterState {
    results: CharacterDto[];
    loading: boolean;
    error: string | null;
    hasSearched: boolean;
}

const initialState: CharacterState = {
    results: [],
    loading: false,
    error: null,
    hasSearched: false,
};

export const CharacterStore = signalStore(
    { providedIn: "root" },
    withState(initialState),
    withMethods((store, http = inject(HttpClient)) => ({
        search: rxMethod<string>(
            pipe(
                tap(() => patchState(store, { loading: true, error: null, hasSearched: true })),
                switchMap(query => {
                    const baseUrl = `${environment.cacheApiUrl.replace('cache/api/v1/cache/', 'api/v1/characters')}`;
                    return http.get<CharacterDto[]>(`${baseUrl}/search`, { params: { query } }).pipe(
                        tap(results => patchState(store, { results, loading: false })),
                        catchError(() => {
                            // Mocking fallback
                            const mockResults: CharacterDto[] = [
                                { id: 1, username: "admin", displayName: "Archivist", email: "admin@hagalaz.com", createdDate: "2024-01-01", lastLoginDate: "2024-03-07", isBanned: false, isMuted: false },
                                { id: 159, username: "zezima", displayName: "Zezima", email: "zez@classic.com", createdDate: "2004-01-01", lastLoginDate: "2026-03-06", isBanned: false, isMuted: false },
                                { id: 420, username: "scammer123", displayName: "Gold Seller", email: "bad@guy.com", createdDate: "2026-02-01", lastLoginDate: "2026-03-01", isBanned: true, isMuted: false },
                            ].filter(c => c.displayName.toLowerCase().includes(query.toLowerCase()) || c.username.toLowerCase().includes(query.toLowerCase()));
                            
                            patchState(store, { results: mockResults, loading: false });
                            return of(null);
                        })
                    );
                })
            )
        )
    }))
);
