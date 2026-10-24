import { inject } from "@angular/core";
import { signalStore, withMethods } from "@ngrx/signals";
import { CacheVarpBitsService } from "../services/cache-varp-bits.service";
import { withCacheDefinition, withCacheRange } from "./cache-definition.features";

export const CacheVarpBitsStore = signalStore(
    withCacheDefinition((id) => inject(CacheVarpBitsService).definitionResource(id)),
    withCacheRange((range) => inject(CacheVarpBitsService).rangeResource(range)),
    withMethods((store, service = inject(CacheVarpBitsService)) => ({
        setVarpBit(v: { id: number; configId: number; bitLength: number; bitOffset: number }): void {
            store._saveDefinition({
                id: v.id,
                changes: { configId: v.configId, bitLength: v.bitLength, bitOffset: v.bitOffset },
                request: () => service.updateVarpBit(v.id, v.configId, v.bitLength, v.bitOffset),
            });
        },
    }))
);
