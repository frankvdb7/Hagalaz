import { inject } from "@angular/core";
import { signalStore } from "@ngrx/signals";
import { CacheItemsService } from "../services/cache-items.service";
import { withCacheDefinition, withCacheRange, withCacheSearch } from "./cache-definition.features";

export const CacheItemsStore = signalStore(
    withCacheDefinition((id) => inject(CacheItemsService).definitionResource(id)),
    withCacheRange((range) => inject(CacheItemsService).rangeResource(range)),
    withCacheSearch((search) => inject(CacheItemsService).searchResource(search))
);
