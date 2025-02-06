import { Action, createAction, props } from "@ngrx/store";
import { CharacterStatisticEntity } from "@app/main/highscores/highscores.models";
import { CharacterStatType } from "@app/services/character-statistics/character-statistics.models";

export const initHighscoresFeature = createAction("[Highscores] Init");
export const destroyHighscoresFeature = createAction("[Highscores] Destroy");

export const loadHighscores = createAction("[Highscores] Load Highscores");
export const loadHighscoresSuccess = createAction(
    "[Highscores] Load Highscores Success",
    props<{ stats: CharacterStatisticEntity[]; total: number }>()
);
export const loadHighscoresFail = createAction(
    "[Highscores] Load Highscores Fail",
    props<{ error: unknown }>()
);

export const filterHighscoresByStatType = createAction(
    "[Highscores] Filter Highscores By Stat Type",
    props<{ statType: CharacterStatType }>()
);

export const filterHighscoresByPage = createAction(
    "[Highscores] Filter Highscores By Page",
    props<{ page: number }>()
);

export const filterHighscoresByLimit = createAction(
    "[Highscores] Filter Highscores By Limit",
    props<{ limit: number }>()
);
