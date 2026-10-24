import { inject } from "@angular/core";
import { signalStore, withMethods } from "@ngrx/signals";
import { CacheConfigDefinitionsService } from "../services/cache-config-definitions.service";
import { withCacheDefinition, withCacheRange } from "./cache-definition.features";

export const CacheConfigDefinitionsStore = signalStore(
    withCacheDefinition((id) => inject(CacheConfigDefinitionsService).definitionResource(id)),
    withCacheRange((range) => inject(CacheConfigDefinitionsService).rangeResource(range)),
    withMethods((store, service = inject(CacheConfigDefinitionsService)) => ({
        setConfigDefinition(v: { id: number; defaultValue: number; valueType: string }): void {
            store._saveDefinition({
                id: v.id,
                changes: { defaultValue: v.defaultValue, valueType: v.valueType },
                request: () => service.updateConfigDefinition(v.id, v.defaultValue, v.valueType),
            });
        },
    }))
);
