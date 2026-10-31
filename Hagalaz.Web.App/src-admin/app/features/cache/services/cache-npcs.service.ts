import type { UpdateNpcCombatLevelDto } from "./dtos";
import type { GetNpcDefinitionDto } from "./dtos";
import { HttpClient } from "@angular/common/http";
import { inject, Service, type Signal } from "@angular/core";
import type { Observable } from "rxjs";
import { environment } from "../../../../environments/environment";
import type { SearchDefinitionsDto, UpdateNpcCombatLevelResultDto } from "./cache.models";
import { cacheDefinitionResource, cacheQueryResource } from "./cache-resource";

@Service()
export class CacheNpcsService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = `${environment.cacheApiUrl}types/npcs`;

    definitionResource(id: Signal<number | null>) {
        return cacheDefinitionResource<GetNpcDefinitionDto>(this.baseUrl, id);
    }

    rangeResource(range: Signal<{ startId: number; endIdExclusive: number } | null>) {
        return cacheQueryResource<GetNpcDefinitionDto[]>(`${this.baseUrl}/range`, range);
    }

    searchResource(search: Signal<{ query: string; offset: number; limit: number } | null>) {
        return cacheQueryResource<SearchDefinitionsDto<GetNpcDefinitionDto>>(`${this.baseUrl}/search`, search);
    }

    updateNpcCombatLevel(id: number, combatLevel: number): Observable<UpdateNpcCombatLevelResultDto> {
        return this.http.post<UpdateNpcCombatLevelResultDto>(`${this.baseUrl}/${id}/combat-level`, { combatLevel } satisfies UpdateNpcCombatLevelDto);
    }
}
