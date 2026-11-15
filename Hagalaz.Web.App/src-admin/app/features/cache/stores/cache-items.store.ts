import { toUpdateItemDefinitionDto } from "../services/item-definition.mapper";
import { inject } from "@angular/core";
import { signalStore, withMethods } from "@ngrx/signals";
import type { GetItemDefinitionDto } from "../services/cache.models";
import { CacheItemsService } from "../services/cache-items.service";
import { withCacheDefinition, withCacheRange, withCacheSearch } from "./cache-definition.features";

export const CacheItemsStore = signalStore(
    withCacheDefinition((id) => inject(CacheItemsService).definitionResource(id)),
    withCacheRange((range) => inject(CacheItemsService).rangeResource(range)),
    withCacheSearch((search) => inject(CacheItemsService).searchResource(search)),
    withMethods((store, service = inject(CacheItemsService)) => ({
        updateItem(item: GetItemDefinitionDto): void {
            const changes = {...item, stackable: (item.stackableType ?? 0) >= 1};
            store._saveDefinition({id: item.id, changes, request: () => service.updateItem(toUpdateItemDefinitionDto(item))});
        },
        duplicateItem(id: number): void {
            store._saveDefinition({id, changes: {}, request: () => service.duplicateItem(id)});
        },
    }))
);
