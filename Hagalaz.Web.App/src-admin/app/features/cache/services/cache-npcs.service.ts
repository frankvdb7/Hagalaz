import { HttpClient } from "@angular/common/http";
import { inject, Service, type Signal } from "@angular/core";
import type { Observable } from "rxjs";
import { environment } from "../../../../environments/environment";
import type { DefinitionSummaryDto, SearchResultDto, TypeMutationResultDto } from "./cache.models";
import { cacheDefinitionResource, cacheQueryResource } from "./cache-resource";

@Service()
export class CacheNpcsService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = `${environment.cacheApiUrl}types/npcs`;

    definitionResource(id: Signal<number | null>) {
        return cacheDefinitionResource<unknown>(this.baseUrl, id);
    }

    rangeResource(range: Signal<{ startId: number; endIdExclusive: number } | null>) {
        return cacheQueryResource<unknown[]>(`${this.baseUrl}/range`, range);
    }

    searchResource(search: Signal<{ query: string; offset: number; limit: number } | null>) {
        return cacheQueryResource<SearchResultDto<DefinitionSummaryDto>>(`${this.baseUrl}/search`, search);
    }

    updateNpcCombatLevel(id: number, combatLevel: number): Observable<TypeMutationResultDto> {
        return this.http.post<TypeMutationResultDto>(`${this.baseUrl}/${id}/combat-level`, { combatLevel });
    }
}
