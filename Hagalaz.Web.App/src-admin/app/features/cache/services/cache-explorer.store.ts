import { HttpClient } from "@angular/common/http";
import { inject } from "@angular/core";
import { pipe, switchMap, tap, catchError, of } from "rxjs";
import { signalStore, withState, withMethods, patchState } from "@ngrx/signals";
import { rxMethod } from "@ngrx/signals/rxjs-interop";
import { environment } from "../../../../environments/environment";

export interface ChecksumEntry {
    indexId: number;
    crc32: number;
    version: number;
    digestBase64: string;
}

export interface ReferenceTableEntry {
    fileId: number;
    id: number;
    crc32: number;
    version: number;
    capacity: number;
    whirlpoolDigestBase64: string;
    children: { childId: number; id: number; index: number }[];
}

export interface ArchiveEntry {
    subFileId: number;
    dataBase64: string;
    dataLength: number;
}

export interface CacheExplorerState {
    indices: ChecksumEntry[];
    selectedArchiveEntries: ReferenceTableEntry[];
    selectedMemberFiles: ArchiveEntry[];
    loading: boolean;
    error: string | null;
}

const initialState: CacheExplorerState = {
    indices: [],
    selectedArchiveEntries: [],
    selectedMemberFiles: [],
    loading: false,
    error: null,
};

export const CacheExplorerStore = signalStore(
    { providedIn: "root" },
    withState(initialState),
    withMethods((store, http = inject(HttpClient)) => ({
        loadIndices: rxMethod<void>(
            pipe(
                tap(() => patchState(store, { loading: true, error: null })),
                switchMap(() => 
                    http.get<{ entries: ChecksumEntry[] }>(`${environment.cacheApiUrl}checksum-table`).pipe(
                        tap(response => patchState(store, { indices: response.entries, loading: false })),
                        catchError(e => {
                            patchState(store, { error: e.message ?? "Failed to load indices", loading: false });
                            return of(null);
                        })
                    )
                )
            )
        ),

        loadArchives: rxMethod<number>(
            pipe(
                tap(() => patchState(store, { loading: true, error: null, selectedArchiveEntries: [], selectedMemberFiles: [] })),
                switchMap(indexId => 
                    http.get<{ entries: ReferenceTableEntry[] }>(`${environment.cacheApiUrl}indexes/${indexId}/reference-table`).pipe(
                        tap(response => patchState(store, { selectedArchiveEntries: response.entries, loading: false })),
                        catchError(e => {
                            patchState(store, { error: e.message ?? "Failed to load archives", loading: false });
                            return of(null);
                        })
                    )
                )
            )
        ),

        loadMemberFiles: rxMethod<{ indexId: number; fileId: number }>(
            pipe(
                tap(() => patchState(store, { loading: true, error: null, selectedMemberFiles: [] })),
                switchMap(({ indexId, fileId }) => 
                    http.get<{ entries: ArchiveEntry[] }>(`${environment.cacheApiUrl}indexes/${indexId}/files/${fileId}/archive`).pipe(
                        tap(response => patchState(store, { selectedMemberFiles: response.entries, loading: false })),
                        catchError(e => {
                            patchState(store, { error: e.message ?? "Failed to load member files", loading: false });
                            return of(null);
                        })
                    )
                )
            )
        ),

        clearSelection(): void {
            patchState(store, { selectedArchiveEntries: [], selectedMemberFiles: [] });
        }
    }))
);
