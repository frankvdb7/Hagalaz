import { Service, type Signal } from "@angular/core";
import { environment } from "../../../../environments/environment";
import type { DefinitionSummaryDto, SearchResultDto } from "./cache.models";
import { cacheDefinitionResource, cacheQueryResource } from "./cache-resource";

@Service()
export class CacheItemsService {
    private readonly baseUrl = `${environment.cacheApiUrl}types/items`;

    definitionResource(id: Signal<number | null>) {
        return cacheDefinitionResource<unknown>(this.baseUrl, id);
    }

    rangeResource(range: Signal<{ startId: number; endIdExclusive: number } | null>) {
        return cacheQueryResource<unknown[]>(`${this.baseUrl}/range`, range);
    }

    searchResource(search: Signal<{ query: string; offset: number; limit: number } | null>) {
        return cacheQueryResource<SearchResultDto<DefinitionSummaryDto>>(`${this.baseUrl}/search`, search);
    }
}
