import { inject } from "@angular/core";
import { signalStore } from "@ngrx/signals";
import { CacheCs2Service } from "../services/cache-cs2.service";
import { withCacheDefinition, withCacheRange } from "./cache-definition.features";

export const CacheCs2Store = signalStore(
    withCacheDefinition((id) => inject(CacheCs2Service).definitionResource(id)),
    withCacheRange((range) => inject(CacheCs2Service).rangeResource(range))
);
