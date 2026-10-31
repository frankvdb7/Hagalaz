import type { GetClientMapDefinitionDto } from "./dtos";
import { Service, type Signal } from "@angular/core";
import { environment } from "../../../../environments/environment";
import { cacheDefinitionResource, cacheQueryResource } from "./cache-resource";

@Service()
export class CacheClientMapDefinitionsService {
    private readonly baseUrl = `${environment.cacheApiUrl}types/client-map-definitions`;

    definitionResource(id: Signal<number | null>) {
        return cacheDefinitionResource<GetClientMapDefinitionDto>(this.baseUrl, id);
    }

    rangeResource(range: Signal<{ startId: number; endIdExclusive: number } | null>) {
        return cacheQueryResource<GetClientMapDefinitionDto[]>(`${this.baseUrl}/range`, range);
    }
}
