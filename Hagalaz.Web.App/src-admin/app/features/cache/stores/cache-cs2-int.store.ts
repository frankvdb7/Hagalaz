import { inject } from "@angular/core";
import { signalStore } from "@ngrx/signals";
import { CacheCs2IntService } from "../services/cache-cs2-int.service";
import { withCacheDefinition } from "./cache-definition.features";

export const CacheCs2IntStore = signalStore(withCacheDefinition((id) => inject(CacheCs2IntService).definitionResource(id)));
