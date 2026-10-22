import { inject } from "@angular/core";
import { signalStore } from "@ngrx/signals";
import { CacheMapsService } from "../services/cache-maps.service";
import { withCacheDefinition } from "./cache-definition.features";

export const CacheMapsStore = signalStore(withCacheDefinition((id) => inject(CacheMapsService).definitionResource(id)));
