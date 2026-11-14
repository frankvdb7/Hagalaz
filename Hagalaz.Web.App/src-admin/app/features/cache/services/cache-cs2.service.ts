import type { GetCs2DefinitionDto, GetCs2ScriptDto } from "./dtos";
import { Service, type Signal } from "@angular/core";
import { environment } from "../../../../environments/environment";
import { cacheDefinitionResource, cacheQueryResource } from "./cache-resource";

@Service()
export class CacheCs2Service {
    private readonly baseUrl = `${environment.cacheApiUrl}types/cs2`;

    definitionResource(id: Signal<number | null>) {
        return cacheDefinitionResource<GetCs2ScriptDto>(this.baseUrl, id);
    }

    rangeResource(range: Signal<{ startId: number; endIdExclusive: number } | null>) {
        return cacheQueryResource<GetCs2DefinitionDto[]>(`${this.baseUrl}/range`, range);
    }
}
