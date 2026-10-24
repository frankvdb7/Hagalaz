import { inject } from "@angular/core";
import { signalStore, withMethods } from "@ngrx/signals";
import { CacheNpcsService } from "../services/cache-npcs.service";
import { withCacheDefinition, withCacheRange, withCacheSearch } from "./cache-definition.features";

export const CacheNpcsStore = signalStore(
    withCacheDefinition((id) => inject(CacheNpcsService).definitionResource(id)),
    withCacheRange((range) => inject(CacheNpcsService).rangeResource(range)),
    withCacheSearch((search) => inject(CacheNpcsService).searchResource(search)),
    withMethods((store, service = inject(CacheNpcsService)) => ({
        setCombatLevel(v: { id: number; combatLevel: number }): void {
            store._saveDefinition({ id: v.id, changes: { combatLevel: v.combatLevel }, request: () => service.updateNpcCombatLevel(v.id, v.combatLevel) });
        },
    }))
);
