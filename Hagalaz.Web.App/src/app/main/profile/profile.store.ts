
import { computed, inject } from "@angular/core";
import { patchState, signalStore, withComputed, withMethods, withState } from "@ngrx/signals";
import { rxMethod } from "@ngrx/signals/rxjs-interop";
import { pipe, switchMap } from "rxjs";
import { tapResponse } from "@ngrx/operators";
import { CharacterStatisticsService } from "@app/services/character-statistics/character-statistics.service";
import { CharacterStatisticsDto } from "@app/services/character-statistics/character-statistics.models";
import { UserStore } from "@app/core/user/user.store";

export interface ProfileState {
    loading: boolean;
    error: unknown;
    statistics: CharacterStatisticsDto | null;
}

const initialState: ProfileState = {
    loading: false,
    error: null,
    statistics: null,
};

export const ProfileStore = signalStore(
    { providedIn: "root" },
    withState(initialState),
    withComputed(({ statistics }) => ({
        displayName: computed(() => statistics()?.displayName || "Unknown"),
        stats: computed(() => statistics()?.statistics || []),
    })),
    withMethods((store, characterStatisticsService = inject(CharacterStatisticsService), userStore = inject(UserStore)) => ({
        loadStatistics: rxMethod<void>(
            pipe(
                switchMap(() =>
                    characterStatisticsService.getStatisticsByDisplayName({ displayName: userStore.username() }).pipe(
                        tapResponse({
                            next: (result) => patchState(store, { statistics: result }),
                            error: (error) => patchState(store, { error }),
                            finalize: () => patchState(store, { loading: false }),
                        })
                    )
                )
            )
        ),
    }))
);
