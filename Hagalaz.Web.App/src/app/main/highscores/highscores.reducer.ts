import { CharacterStatisticEntity, HighscoresState } from "./highscores.models";
import { CharacterStatType } from "@app/services/character-statistics/character-statistics.models";
import { createFeature, createReducer, createSelector, on } from "@ngrx/store";
import {
    filterHighscoresByLimit,
    filterHighscoresByPage,
    filterHighscoresByStatType,
    loadHighscores,
    loadHighscoresFail,
    loadHighscoresSuccess,
} from "@app/main/highscores/highscores.actions";
import { createEntityAdapter, EntityAdapter } from "@ngrx/entity";

const adapter: EntityAdapter<CharacterStatisticEntity> = createEntityAdapter({
    selectId: (model) => model.name,
});

const initialState: HighscoresState = adapter.getInitialState({
    paging: {
        type: CharacterStatType.Overall,
        page: 1,
        limit: 25,
        total: 0,
    },
    loading: false,
    error: null,
});

export const highscoresFeature = createFeature({
    name: "highscores",
    reducer: createReducer(
        initialState,
        on(filterHighscoresByStatType, (state, { statType }) => ({
            ...state,
            paging: { ...state.paging, type: statType },
        })),
        on(filterHighscoresByPage, (state, { page }) => ({
            ...state,
            paging: { ...state.paging, page: page },
        })),
        on(filterHighscoresByLimit, (state, { limit }) => ({
            ...state,
            paging: { ...state.paging, limit: limit },
        })),
        on(loadHighscores, (state) => ({ ...state, loading: true })),
        on(loadHighscoresSuccess, (state, { stats, total }) =>
            adapter.setAll(stats, {
                ...state,
                loading: false,
                paging: { ...state.paging, total: total },
            })
        ),
        on(loadHighscoresFail, (state, { error }) => ({
            ...state,
            error,
            loading: false,
        }))
    ),
    extraSelectors: ({ selectHighscoresState, selectPaging }) => ({
        selectHighscoresStatType: createSelector(selectPaging, (paging) => paging.type),
        ...adapter.getSelectors(selectHighscoresState),
    }),
});
