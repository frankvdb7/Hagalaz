import { inject } from "@angular/core";
import { signalStore } from "@ngrx/signals";
import { CacheClientMapDefinitionsService } from "../services/cache-client-map-definitions.service";
import { withCacheDefinition, withCacheRange } from "./cache-definition.features";

export const CacheClientMapDefinitionsStore = signalStore(
    withCacheDefinition((id) => inject(CacheClientMapDefinitionsService).definitionResource(id)),
    withCacheRange((range) => inject(CacheClientMapDefinitionsService).rangeResource(range))
);
