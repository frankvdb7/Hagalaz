import { inject } from "@angular/core";
import { signalStore, withMethods } from "@ngrx/signals";
import { CacheObjectsService } from "../services/cache-objects.service";
import { withCacheDefinition, withCacheRange } from "./cache-definition.features";

export const CacheObjectsStore = signalStore(
    withCacheDefinition((id) => inject(CacheObjectsService).definitionResource(id)),
    withCacheRange((range) => inject(CacheObjectsService).rangeResource(range)),
    withMethods((store, service = inject(CacheObjectsService)) => ({
        setName(v: { id: number; name: string }): void {
            store._saveDefinition({ id: v.id, changes: { name: v.name }, request: () => service.updateObjectName(v.id, v.name) });
        },
    }))
);
