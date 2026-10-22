import { inject } from "@angular/core";
import { signalStore } from "@ngrx/signals";
import { CacheSpritesService } from "../services/cache-sprites.service";
import { withCacheDefinition, withCacheRange, withCacheSearch } from "./cache-definition.features";

export const CacheSpriteDefinitionsStore = signalStore(
    withCacheDefinition((id) => inject(CacheSpritesService).definitionResource(id)),
    withCacheRange((range) => inject(CacheSpritesService).rangeResource(range)),
    withCacheSearch((search) => inject(CacheSpritesService).searchResource(search))
);
