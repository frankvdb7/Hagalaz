import { inject } from "@angular/core";
import { signalStore } from "@ngrx/signals";
import { CacheGraphicsService } from "../services/cache-graphics.service";
import { withCacheDefinition, withCacheRange } from "./cache-definition.features";

export const CacheGraphicsStore = signalStore(
    withCacheDefinition((id) => inject(CacheGraphicsService).definitionResource(id)),
    withCacheRange((range) => inject(CacheGraphicsService).rangeResource(range))
);
