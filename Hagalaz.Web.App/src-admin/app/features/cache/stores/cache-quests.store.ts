import { inject } from "@angular/core";
import { signalStore } from "@ngrx/signals";
import { CacheQuestsService } from "../services/cache-quests.service";
import { withCacheDefinition, withCacheRange } from "./cache-definition.features";

export const CacheQuestsStore = signalStore(
    withCacheDefinition((id) => inject(CacheQuestsService).definitionResource(id)),
    withCacheRange((range) => inject(CacheQuestsService).rangeResource(range))
);
