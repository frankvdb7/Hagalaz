import { computed, inject } from "@angular/core";
import { patchState, signalStore, withComputed, withMethods, withState } from "@ngrx/signals";
import { rxMethod } from "@ngrx/signals/rxjs-interop";
import { pipe, switchMap } from "rxjs";
import { tapResponse } from "@ngrx/operators";
import { PvpService } from "@app/services/pvp/pvp.service";
import { PvpStats, PvpMatch } from "@app/services/pvp/pvp.models";

export interface PvpState {
    loading: boolean;
    error: unknown;
    stats: PvpStats | null;
    matches: PvpMatch[];
}

const initialState: PvpState = {
    loading: false,
    error: null,
    stats: null,
    matches: [],
};

export const PvpStore = signalStore(
    { providedIn: "root" },
    withState(initialState),
    withComputed(({ stats, matches }) => ({
        kills: computed(() => stats()?.kills || 0),
        deaths: computed(() => stats()?.deaths || 0),
        killDeathRatio: computed(() => stats()?.killDeathRatio || 0),
        recentMatches: computed(() => matches()),
    })),
    withMethods((store, pvpService = inject(PvpService)) => ({
        loadPvpStats: rxMethod<void>(
            pipe(
                switchMap(() =>
                    pvpService.getPvpStats().pipe(
                        tapResponse({
                            next: (result) => patchState(store, { stats: result }),
                            error: (error) => patchState(store, { error }),
                            finalize: () => patchState(store, { loading: false }),
                        })
                    )
                )
            )
        ),
        loadMatchHistory: rxMethod<void>(
            pipe(
                switchMap(() =>
                    pvpService.getMatchHistory().pipe(
                        tapResponse({
                            next: (result) => patchState(store, { matches: result }),
                            error: (error) => patchState(store, { error }),
                            finalize: () => patchState(store, { loading: false }),
                        })
                    )
                )
            )
        ),
    }))
);
