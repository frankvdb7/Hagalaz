import { CharacterStatType } from "@app/services/character-statistics/character-statistics.models";
import { patchState, signalStore, withHooks, withMethods, withState } from "@ngrx/signals";
import { setAllEntities, withEntities } from "@ngrx/signals/entities";
import { CharacterStatisticEntity } from "./highscores.models";
import { rxMethod } from "@ngrx/signals/rxjs-interop";
import { debounceTime, pipe, switchMap, tap, timeout } from "rxjs";
import { inject } from "@angular/core";
import { CharacterStatisticsService } from "@app/services/character-statistics/character-statistics.service";
import { tapResponse } from "@ngrx/operators";
import { mapStatisticsResult, mapStatisticString } from "./highscores.functions";
import { ActivatedRoute } from "@angular/router";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";

export interface HighscoresState {
    paging: {
        type: CharacterStatType;
        page: number;
        limit: number;
        total: number;
    };
    loading: boolean;
    error: unknown;
}

const initialState: HighscoresState = {
    paging: {
        type: CharacterStatType.Overall,
        page: 1,
        limit: 25,
        total: 0,
    },
    loading: false,
    error: null,
};

const selectId = (model: CharacterStatisticEntity) => model.name;

export const HighscoresStore = signalStore(
    withState(() => initialState),
    withEntities<CharacterStatisticEntity>(),
    withMethods((store, statisticsService = inject(CharacterStatisticsService)) => ({
        loadHighscores: rxMethod<void>(
            pipe(
                tap(() => patchState(store, { error: null, loading: true })),
                debounceTime(100),
                switchMap(() =>
                    statisticsService
                        .getAllStatistics({
                            sort: { experience: "asc" },
                            filter: {
                                page: store.paging().page,
                                limit: store.paging().limit,
                                type: store.paging().type,
                            },
                        })
                        .pipe(
                            timeout(5000),
                            tapResponse({
                                next: (result) => {
                                    const [stats, total] = mapStatisticsResult(result);
                                    patchState(store, { paging: { ...store.paging(), total: total } }, setAllEntities(stats, { selectId: selectId }));
                                },
                                error: (error) => patchState(store, { error: error }),
                                finalize: () => patchState(store, { loading: false }),
                            })
                        )
                )
            )
        ),
        setPaging: (paging: Partial<HighscoresState["paging"]>) => patchState(store, { paging: { ...store.paging(), ...paging } }),
    })),
    withHooks((store, route = inject(ActivatedRoute)) => ({
        onInit(): void {
            route.queryParams.pipe(takeUntilDestroyed()).subscribe((params) => {
                store.setPaging({
                    type: mapStatisticString(params.skill) || initialState.paging.type,
                    page: +params.page || initialState.paging.page,
                    limit: +params.limit || initialState.paging.limit,
                });
                store.loadHighscores();
            });
        },
    }))
);
