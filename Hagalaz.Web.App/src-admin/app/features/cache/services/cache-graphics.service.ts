import type { GetGraphicDefinitionDto } from "./dtos";
import { Service, type Signal } from "@angular/core";
import { environment } from "../../../../environments/environment";
import { cacheDefinitionResource, cacheQueryResource } from "./cache-resource";

@Service()
export class CacheGraphicsService {
    private readonly baseUrl = `${environment.cacheApiUrl}types/graphics`;

    definitionResource(id: Signal<number | null>) {
        return cacheDefinitionResource<GetGraphicDefinitionDto>(this.baseUrl, id);
    }

    rangeResource(range: Signal<{ startId: number; endIdExclusive: number } | null>) {
        return cacheQueryResource<GetGraphicDefinitionDto[]>(`${this.baseUrl}/range`, range);
    }
}
