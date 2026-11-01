import {httpResource} from "@angular/common/http";
import {signalStore} from "@ngrx/signals";
import {environment} from "../../../../environments/environment";
import {withCacheRange} from "./cache-definition.features";
import {cacheQueryResource} from "../services/cache-resource";
import type {DefinitionSummaryDto} from "../services/cache.models";

export const CacheModelsStore = signalStore(
    withCacheRange(range => cacheQueryResource<DefinitionSummaryDto[]>(`${environment.cacheApiUrl}models/range`, range))
);

export function modelCapacityResource(enabled: () => boolean) {
    return httpResource<{capacity: number}>(() => enabled() ? `${environment.cacheApiUrl}models/capacity` : undefined);
}
