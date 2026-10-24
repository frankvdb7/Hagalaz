import { inject } from "@angular/core";
import { signalStore } from "@ngrx/signals";
import { CacheAnimationsService } from "../services/cache-animations.service";
import { withCacheDefinition, withCacheRange } from "./cache-definition.features";

export const CacheAnimationsStore = signalStore(
    withCacheDefinition((id) => inject(CacheAnimationsService).definitionResource(id)),
    withCacheRange((range) => inject(CacheAnimationsService).rangeResource(range))
);
